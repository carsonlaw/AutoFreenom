# AutoFreenom

## 工具介绍

此小工具可以自动检测并获取主机外网IP，IP变更时可以自动登录[Freenom](https://www.freenom.com/)并更新域名A记录为更改后IP，并发送邮件通知结果。

## Freenom介绍

[Freenom](https://www.freenom.com/)是个可以免费注册顶级域名的服务商，可以免费申请免费域名。

## 开发工具

- IDE: Visual Studio 2019
- Language: C# 9.0
- SDK: .NET 5

## 使用方法

运行 Docker
```bash
docker run -d --restart unless-stopped \
    -e NomDomainName="yourdomain.ml" \
    -e NomEmail="name@mail.com" \
    -e NomPass="password" \
    -e smtpHost="smtp.mail.com" \
    -e smtpAccount="sent@mail.com" \
    -e smtpPassword="password" \
    -e smtpToAddress="revive@mail.com" \
    --name autofreenom registry.cn-chengdu.aliyuncs.com/esechi/autofreenom:latest
```

确认容器正常运行即可，如果IP发生变动，则自动更新域名A记录。
查看日志或者更换ip后，查看是否生效

```bash
docker logs autofreenom
```

### 参数说明

- DelayMinute ： IP检测时间间隔分钟数，默认10
- NomDomainName : 需要修改A记录的域名
- NomEmail : freenom登录账户
- NomPass : freenom登录密码
- smtpHost : 发送邮件smtp主机地址
- smtpPort : smtp主机端口，默认25
- smtpAccount : 发送邮件email账户
- smtpPassword : 发送邮件email密码
- smtpToAddress : 接受通知邮件email地址