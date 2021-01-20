using NLog;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace CheckWanIP
{
    class Program
    {
        //freenom args
        private static string NomDomainName, NomHostName, NomEmail, NomPass;
        private static string smtpHost, smtpPort, smtpAccount, smtpPassword, smtpToAddress;
        private static int DelayMinute;
        private static readonly string[] urls = new string[] {
            "http://api.ipify.org/",
            "http://ifconfig.me/ip",
            //"https://ipinfo.io/ip",
            "http://icanhazip.com/"
        };

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static HttpClient http;

        //private static IConfiguration conf;

        static async Task Main(string[] args)
        {
            await SetParams();
            while (true)
            {
                await DOCheckIp();
                await Task.Delay(1000 * 60 * DelayMinute);
            }
        }

        private static async Task DOCheckIp()
        {
            using (http = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }))
            {
                try
                {
                    //http.Timeout = TimeSpan.FromMinutes(1);
                    var ip = await GetWanIPAsync();
                    logger.Info($"Get IP:{ip}");
                    //IP changed
                    if (await IsIPChanged(ip))
                    {
                        var ret = await UpdateDNS(ip);
                        await SendMail(ip, ret);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e, "error");
                }
            }
        }

        /// <summary>
        /// Auto set freenom
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private static async Task<bool> UpdateDNS(string ip)
        {
            logger.Debug("http get clientarea.php?action=domains");
            var html = await http.GetAsync("https://my.freenom.com/clientarea.php?action=domains");
            var res = await html.Content.ReadAsStringAsync();
            logger.Debug($"response:{res}");
            var token = Regex.Match(res, "name=\"token\" value=\"(.*)\"", RegexOptions.Multiline).Groups[1].Value;

            var content = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("token", token),
                new KeyValuePair<string, string>("username", NomEmail),
                new KeyValuePair<string, string>("password", NomPass),
                //new KeyValuePair<string, string>("rememberme", "off")
            });

            logger.Debug($"http post dologin.php data:{token}|{NomEmail}|{NomPass}");

            http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
            http.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            http.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            http.DefaultRequestHeaders.Add("Origin", "https://my.freenom.com");
            http.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            http.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            http.DefaultRequestHeaders.Add("Referer", "https://my.freenom.com/clientarea.php");
            http.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

            html = await http.PostAsync("https://my.freenom.com/dologin.php", content);
            res = await html.Content.ReadAsStringAsync();
            logger.Debug($"response:{res}");

            var islogin = html.StatusCode == HttpStatusCode.OK && Regex.IsMatch(res, "Hello");
            if (islogin)
            {
                var id = Regex.Match(res, NomDomainName + @".*?clientarea\.php\?action=domaindetails&id=(\d*)", RegexOptions.Singleline).Groups[1].Value;
                content = new FormUrlEncodedContent(new[] {
                    new KeyValuePair<string, string>("token", token),
                    new KeyValuePair<string, string>("dnsaction", "modify"),
                    new KeyValuePair<string, string>("records[0][line]", ""),
                    new KeyValuePair<string, string>("records[0][type]", "A"),
                    new KeyValuePair<string, string>("records[0][name]", ""),
                    new KeyValuePair<string, string>("records[0][ttl]", "600"),
                    new KeyValuePair<string, string>("records[0][value]", ip)
                });
                html = await http.PostAsync($"https://my.freenom.com/clientarea.php?managedns={NomDomainName}&domainid={id}", content); 
                res = await html.Content.ReadAsStringAsync();
                logger.Debug($"response:{res}");
                if (html.StatusCode == HttpStatusCode.OK)
                {
                    logger.Info($"set DNS to IP:{ip}");
                    return true;
                }
            }

            logger.Info($"set DNS to IP failure:{ip}");
            return false;

        }

        /// <summary>
        /// not working
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private static async Task<bool> UpdateDNSAPI(string ip)
        {
            return false;
            var param = $"domainname={NomDomainName}&hostname={NomHostName}&ipaddress={ip}&email={NomEmail}&password={NomPass}";
            var content = new ByteArrayContent(new UTF8Encoding(true).GetBytes(param));
            var response = await http.PutAsync("https://api.freenom.com/v2/nameserver/register", content);
            if (!response.IsSuccessStatusCode)
            {
                logger.Error($"Set DNS error!,StatusCode:{response.StatusCode}");
                return false;
            }
            var xml = await response.Content.ReadAsStringAsync();
            if (Regex.IsMatch(xml, @"OK"))
            {
                return true;
            }
            else
            {
                logger.Error($"set DNS Fault!,Response:{xml}");
                return false;
            }
        }

        /// <summary>
        /// Check ip changed
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private static async Task<bool> IsIPChanged(string ip)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string path = @"IP.txt";
                    if (!File.Exists(path))
                    {
                        using (StreamWriter sw = File.CreateText(path))
                        {
                            sw.WriteLine(ip);
                        }
                        logger.Debug($"ip.txt not exist,create ip.txt,IP{ip}");
                        return false;
                    }
                    else
                    {
                        string oldip;
                        using (StreamReader sr = File.OpenText(path))
                        {
                            oldip = sr.ReadLine();
                        }
                        if (ip == oldip)
                        {
                            logger.Debug($"ip.txt exist,IP not change,IP{ip}");
                            return false;
                        }
                        else if(string.IsNullOrEmpty(ip))
                        {
                            logger.Error($"ip.txt exist,Get IP Fault{ip}");
                            return false;
                        }
                        else
                        {
                            using (StreamWriter sw = File.CreateText(path))
                            {
                                sw.WriteLine(ip);
                            }
                            logger.Debug($"ip.txt exist,IP changed, updated ip.txt,old IP{oldip},new IP{ip}");
                            return true;
                        }

                    }
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
                return false;
            });

        }

        /// <summary>
        /// Get WanLan IP
        /// </summary>
        /// <returns></returns>
        private static async Task<string> GetWanIPAsync()
        {
            var tasks = new List<Task<string>>();            
            var cts = new CancellationTokenSource();
            cts.CancelAfter(1*60*1000);
            foreach(var url in urls)
            {
                tasks.Add(http.GetStringAsync(url,cts.Token));
            }
            while(!cts.IsCancellationRequested)
            {
                if(tasks.Count<=0)
                {
                    logger.Error($"Can't get IP From {urls}");
                    return null;
                }
                var task = await Task.WhenAny(tasks);
                var fromurl = urls[tasks.IndexOf(task)];
                tasks.Remove(task);
                var ip = (await task).Trim();
                logger.Error($"get IP From {fromurl},IP{ip}");
                if (task.IsCompletedSuccessfully && !string.IsNullOrEmpty(ip))
                {
                    cts.Cancel();
                    return ip;
                }
            }
            return null;
        }

        /// <summary>
        /// set email
        /// </summary>
        private static async Task SendMail(string ip,bool isset)
        {
            SmtpClient smtp = new SmtpClient(smtpHost, int.Parse(smtpPort));
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential(smtpAccount, smtpPassword);

            MailAddress from = new MailAddress(smtpAccount);
            MailAddress to = new MailAddress(smtpToAddress);
            MailMessage message = new MailMessage(from, to);
            message.Body = $"IP has changed to : {ip} Auto set freenom {isset.ToString()}";
            //message.Body += Environment.NewLine;
            message.BodyEncoding = Encoding.UTF8;
            message.Subject = $"IP has changed to : {ip}";
            message.SubjectEncoding = Encoding.UTF8;

            smtp.SendCompleted += (object sender, System.ComponentModel.AsyncCompletedEventArgs e) =>
            {
                if (e.Cancelled)
                {
                    logger.Info("smtp Send canceled.");
                }
                if (e.Error != null)
                {
                    logger.Error("smtp e.Error.ToString()");
                }
                else
                {
                    logger.Info("smtp Message sent.");
                }
            };

            await smtp.SendMailAsync(message);
            return;
        }

        /// <summary>
        /// set args
        /// </summary>
        /// <returns></returns>
        private static async Task SetParams()
        {
            DelayMinute = 1;
            smtpPort = "25";

            int.TryParse(Environment.GetEnvironmentVariable(nameof(DelayMinute)) ?? DelayMinute.ToString(), out var DelayMinuteE);
            DelayMinute = DelayMinuteE > 0 ? DelayMinuteE : DelayMinute;
            NomDomainName = Environment.GetEnvironmentVariable(nameof(NomDomainName)) ?? NomDomainName;
            NomHostName = Environment.GetEnvironmentVariable(nameof(NomHostName)) ?? NomHostName;
            NomEmail = (Environment.GetEnvironmentVariable(nameof(NomEmail)) ?? NomEmail);
            NomPass = (Environment.GetEnvironmentVariable(nameof(NomPass)) ?? NomPass);

            smtpHost = Environment.GetEnvironmentVariable(nameof(smtpHost)) ?? smtpHost;
            smtpPort = Environment.GetEnvironmentVariable(nameof(smtpPort)) ?? smtpPort;
            smtpAccount = Environment.GetEnvironmentVariable(nameof(smtpAccount)) ?? smtpAccount;
            smtpPassword = Environment.GetEnvironmentVariable(nameof(smtpPassword)) ?? smtpPassword;
            smtpToAddress = Environment.GetEnvironmentVariable(nameof(smtpToAddress)) ?? smtpToAddress;

        }
    }


}
