# AutoFreenom

## Info

Automatically get Wan IP and update [Freenom](https://www.freenom.com/) DNS A record

## Freenom

[Freenom](https://www.freenom.com/) is a free DNS。

## Developer

- IDE: Visual Studio 2019
- Language: C# 9.0
- SDK: .NET 5

## usage

run Docker
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
ARM64
```bash
docker run -d --restart unless-stopped \
    -e NomDomainName="yourdomain.ml" \
    -e NomEmail="name@mail.com" \
    -e NomPass="password" \
    -e smtpHost="smtp.mail.com" \
    -e smtpAccount="sent@mail.com" \
    -e smtpPassword="password" \
    -e smtpToAddress="revive@mail.com" \
    --name autofreenom registry.cn-chengdu.aliyuncs.com/esechi/autofreenom:arm64
```

check logs

```bash
docker logs autofreenom
```

### Args

- DelayMinute ： IP check delay，default 10
- NomDomainName : domail
- NomEmail : freenom account
- NomPass : freenom password
- smtpHost : smtp host
- smtpPort : smt port，default 25
- smtpAccount : email account
- smtpPassword : email pass
- smtpToAddress : email sent to