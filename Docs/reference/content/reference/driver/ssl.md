+++
date = "2015-03-17T15:36:56Z"
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