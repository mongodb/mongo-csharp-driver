+++
date = "2018-04-23T07:36:42Z"
draft = false
title = "SSL"
[menu.main]
  parent = "Reference Connecting"
  identifier = "SSL"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## SSL

The driver supports SSL connections to MongoDB servers using the underlying support for SSL provided by the .NET Framework. The driver takes a [`Network Stream`]({{< msdnref "system.net.sockets.networkstream" >}}) and wraps it with an [`SslStream`]({{< msdnref "system.net.security.sslstream" >}}). You can configure the use of SSL with the [connection string]({{< relref "reference\driver\connecting.md#connection-string" >}}) or with [`MongoClientSettings`]({{< apiref "T_MongoDB_Driver_MongoClientSettings" >}}).

## Connection String

The connection string provides 2 options:

1. `?ssl=true|false`
	You can turn on SSL using this option, or explicitly turn it off. The default is `false`.
1. `?sslVerifyCertificate=true|false`
	You can turn off automatic certificate verification using this option. The default is `true`.
	{{% note class="warning" %}}This option should not be set to `false` in production. It is important that the server certificate is properly validated.{{% /note %}}

## MongoClientSettings

[`MongoClientSettings`]({{< apiref "T_MongoDB_Driver_MongoClientSettings" >}}) provides a much fuller and robust solution for configuring SSL. It contains the [`SslSettings`]({{< apiref "P_MongoDB_Driver_MongoClientSettings_SslSettings" >}}) property which allows the setting of various values. Each of these values will map very strongly to their counterpart in the [`SslStream constructor`]({{< msdnref "dd990420" >}}) and the [`AuthenticateAsClient`]({{< msdnref "ms145061" >}}) method. For example, to authenticate with a client certificate called "client.pfx":

```csharp
var cert = new X509Certificate2("client.pfx", "mySuperSecretPassword");

var settings = new MongoClientSettings
{
    SslSettings = new SslSettings
    {
        ClientCertificates = new[] { cert },
    },
    UseSsl = true
};
```

{{% note class="important" %}}It is imperative that when loading a certificate with a password, the [PrivateKey]({{< msdnref "system.security.cryptography.x509certificates.x509certificate2.privatekey" >}}) property not be null. If the property is null, it means that your certificate does not contain the private key and will not be passed to the server.{{% /note %}}

## TLS support
### Overview

| OS      | .NET Version          | TLS1.1 | TLS1.2 | SNI | CRLs without OCSP |
|---------|-----------------------|--------|--------|-----|-------------------|
| Windows |                       |        |        |     |                   |
|         | .NET Framework 4.5    | Yes    | Yes    | Yes | Yes               |
|         | .NET Framework 4.6    | Yes    | Yes    | Yes | Yes               |
|         | .NET Framework 4.7    | Yes    | Yes    | Yes | Yes               |
|         | .NET Core 1.0         | Yes    | Yes    | Yes | Yes               |
|         | .NET Core 1.1         | Yes    | Yes    | Yes | Yes               |
|         | .NET Core 2.0         | Yes    | Yes    | Yes | Yes               |
|         | .NET Core 2.1-preview | Yes    | Yes    | Yes | Yes               |
| Linux   |                       |        |        |     |                   |
|         | .NET Core 1.0         | Yes    | Yes    | No  | Yes               |
|         | .NET Core 1.1         | Yes    | Yes    | No  | Yes               |
|         | .NET Core 2.0         | Yes    | Yes    | No  | Yes               |
|         | .NET Core 2.1-preview | Yes    | Yes    | Yes | Yes               |
| OSX     |                       |        |        |     |                   |
|         | .NET Core 1.0         | Yes    | Yes    | No  | Yes               |
|         | .NET Core 1.1         | Yes    | Yes    | No  | Yes               |
|         | .NET Core 2.0         | Yes    | Yes    | Yes | No                |
|         | .NET Core 2.1-preview | Yes    | Yes    | Yes | No                |


#### Notes:
 - SNI (Server Name Indication) required for Atlas free tier.
 - If a server's certificate includes Certificate Revocation List (CRL) Distribution Points but does not include an Online Certificate Status Protocol (OCSP) extension, .NET Core on OSX will fail to connect due to a limitation of the Apple Security Framework (see https://github.com/dotnet/corefx/issues/29064). Prior to version 2.0, .NET Core on OSX used OpenSSL, which does support CRLs without OCSP.


### Support for TLS v1.1 and newer

Industry best practices recommend, and some regulations require, the use of TLS 1.1 or newer. No application changes are required
for the driver to make use of the newest TLS protocols.
