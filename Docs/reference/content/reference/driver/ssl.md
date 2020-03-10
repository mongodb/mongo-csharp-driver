+++
date = "2018-07-02T12:50:42Z"
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

### Certificate Revocation Checking

#### Default behavior
The .NET Driver now **enables** certificate revocation checking by
default, setting [`CheckCertificateRevocation`]({{< apiref
"P_MongoDB_Driver_SslSettings_CheckCertificateRevocation">}}) in
[`SslSettings`]({{< apiref "T_MongoDB_Driver_SslSettings" >}}) to
`true` by default. This is in contrast to .NET's defaults for
`SslStream` (see .NET Framework documentation
[here](https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream.authenticateasclient?view=netframework-4.7.2#System_Net_Security_SslStream_AuthenticateAsClient_System_String_)
and .NET Standard documentation
[here](https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream.authenticateasclient?view=netstandard-2.0#System_Net_Security_SslStream_AuthenticateAsClient_System_String_)).
Any applications relying on the older default of `false` now must
explicitly set [`CheckCertificateRevocation`]({{< apiref
"P_MongoDB_Driver_SslSettings_CheckCertificateRevocation">}}) to
`false` in [`SslSettings`]({{< apiref "T_MongoDB_Driver_SslSettings"
>}}) to disable certificate revocation checking. Alternatively,
applications may also set `tlsDisableCertificateRevocationCheck=true`
in their connection string.  See
[tlsDisableCertificateRevocationCheck](#tlsDisableCertificateRevocationCheck)
for more information.

Prior to v2.7.0, the driver also enabled certificate revocation checking by
default.

#### tlsDisableCertificateRevocationCheck
The URI option, `tlsDisableCertificateRevocationCheck` controls
whether or not to disable certificate revocation checking during a TLS
handshake. Setting `tlsDisableCertificateRevocationCheck=true` is
equivalent to setting [`CheckCertificateRevocation`]({{< apiref
"P_MongoDB_Driver_SslSettings_CheckCertificateRevocation">}}) in
[`SslSettings`]({{< apiref "T_MongoDB_Driver_SslSettings" >}}) to
`false`.

### OCSP

#### Stapling
Due to limitations in .NET, the driver currently only supports OCSP
(Online Certificate Status Protocol) stapling on .NET Core ≥2.x on
macOS.

On Windows, when a server has a Must-Staple certificate and does not
staple, by default, the driver will continue to connect as long as the
OCSP responder is still available and reports that the server's
certificate is valid. This behavior differs from the mongo shell and
from the MongoDB Python and Go drivers, which will fail to connect in
when a server has a Must-Staple certificate and does not staple.

#### Hard-fail vs. soft-fail
On Windows, due .NET's implementation of TLS, the driver utilizes
"hard-fail" behavior in contrast to the "soft-fail" behavior exhibited
by the Linux/macOS mongo shell and MongoDB drivers such as Python and
Go. This means that in the case that an OCSP responder is unavailable,
the driver will fail to connect (i.e. hard-fail) instead of allowing
the connection to continue (i.e. soft-fail).

## TLS support
### Overview

| OS | .NET Version | TLS1.1 | TLS1.2 | SNI | CRLs without OCSP |
|---------|-----------------------|--------|--------|-----|-------------------|
| Windows |                       |        |        |     |                   |
|         | .NET Framework 4.5    | Yes    | Yes    | Yes | Yes               |
|         | .NET Framework 4.6    | Yes    | Yes    | Yes | Yes               |
|         | .NET Framework 4.7    | Yes    | Yes    | Yes | Yes               |
|         | .NET Core 1.0         | Yes    | Yes    | Yes | Yes               |
|         | .NET Core 1.1         | Yes    | Yes    | Yes | Yes               |
|         | .NET Core 2.0         | Yes    | Yes    | Yes | Yes               |
|         | .NET Core 2.1         | Yes    | Yes    | Yes | Yes               |
| Linux   |                       |        |        |     |                   |
|         | .NET Core 1.0         | Yes    | Yes    | No  | Yes               |
|         | .NET Core 1.1         | Yes    | Yes    | No  | Yes               |
|         | .NET Core 2.0         | Yes    | Yes    | No  | Yes               |
|         | .NET Core 2.1         | Yes    | Yes    | Yes | Yes               |
| macOS   |                       |        |        |     |                   |
|         | .NET Core 1.0         | Yes    | Yes    | No  | Yes               |
|         | .NET Core 1.1         | Yes    | Yes    | No  | Yes               |
|         | .NET Core 2.0         | Yes    | Yes    | Yes | No                |
|         | .NET Core 2.1         | Yes    | Yes    | Yes | No                |


#### Notes
 - SNI (Server Name Indication) is required for Atlas free tier.
 - .NET Core on macOS will fail to connect if **both** of the following conditions are met: (1) [certificate revocation checking]({{<relref "reference\driver\ssl.md#certificate-revocation-checking" >}}) is enabled, and (2) a server's certificate includes Certificate Revocation List (CRL) Distribution Points but does not include an Online Certificate Status Protocol (OCSP) extension.

  - This is due to a limitation of the Apple Security Framework (see https://github.com/dotnet/corefx/issues/29064). Prior to version 2.0, .NET Core on macOS used OpenSSL, which does support CRLs without OCSP.
  - Connecting to Atlas on macOS with certificate revocation checking enabled will succeed since Atlas certificates include CRL Distribution Points as well as an OCSP extension.


### Support for TLS v1.1 and newer

Industry best practices recommend, and some regulations require, the use of TLS 1.1 or newer. No application changes are required
for the driver to make use of the newest TLS protocols.
