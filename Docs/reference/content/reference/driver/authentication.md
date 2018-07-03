+++
date = "2018-07-02T16:49:42Z"
draft = false
title = "Authentication"
[menu.main]
  parent = "Reference Connecting"
  identifier = "Authentication"
  weight = 30
  pre = "<i class='fa'></i>"
+++

## Authentication

The .NET driver supports all [MongoDB authentication mechanisms]({{< docsref "core/authentication/" >}}) including those in the [Enterprise Edition]({{< docsref "administration/install-enterprise/" >}}).

Authentication credentials are created by the application as instances of [`MongoCredential`]({{< apiref "T_MongoDB_Driver_MongoCredential" >}}) which includes static factory methods for each of the supported authentication mechanisms. A list of these instances must be passed to the driver using the [`MongoClient constructor`]({{< apiref "M_MongoDB_Driver_MongoClient__ctor_1" >}}) that takes a [`MongoClientSettings`]({{< apiref "T_MongoDB_Driver_MongoClientSettings" >}}). When only one credential is necessary, it is possible to specify via the [connection string]({{< relref "connecting.md#connection-string" >}}).


### Default

MongoDB 4.0 now uses [SCRAM](https://docs.mongodb.com/manual/core/security-scram/#authentication-scram) (Salted Challenge Response Authentication Mechanism) as the default mechanism and no longer supports [MONGODB-CR](http://docs.mongodb.org/manual/core/authentication/#mongodb-cr-authentication). To create a credential that will authenticate properly regardless of server version, create a credential using the following static factory method.

```csharp
var credential = MongoCredential.CreateCredential(databaseName, username, password);
```

Or via the connection string:

```
mongodb://username:password@myserver/databaseName
```

These are the recommended approaches as it will make upgrading from MongoDB 2.6 to MongoDB 3.0 seamless, before and after [upgrading the authentication schema](http://docs.mongodb.org/manual/release-notes/3.0-scram/#upgrade-mongodb-cr-to-scram). For MongoDB 4.0, the above approaches will automatically determine which version of SCRAM should be used (SCRAM-SHA-1 or SCRAM-SHA-256).

{{% note %}}The databaseName part of the connection string indicates which database the credentials are located in. See the [connection string section]({{< relref "connecting.md#connection-string" >}}) for more information on connection strings.{{% /note %}}

### SCRAM

When connecting to a MongoDB 4.0 server without specifying an authentication mechanism, the driver will negotiate with the server to determine whether SCRAM-SHA-1 or SCRAM-SHA-256 is the appropriate mechanism.

#### SCRAM-SHA-256

SCRAM-SHA-256 is the default authentication mechanism chosen as long as the user's authentication mechanism supports it. (See the [mechanism parameter of createUser()](https://docs.mongodb.com/manual/reference/method/db.createUser/#db.createUser) and the ["Supported Authentication Methods: Defaults" section of the Driver Authentication Specification](https://github.com/mongodb/specifications/blob/master/source/auth/auth.rst#defaults) for additional information.

##### SecureStrings

SecureStrings are slightly less secure when used in conjunction with SCRAM-SHA-256, due to the need to temporarily store the cleartext password in a managed memory string in order to SASLPrep it. This behavior is no different from other drivers in languages with managed memory. (SCRAM-SHA-1 is *not* affected.) 

##### .NET Standard support

In .NET Standard, authenticating via SCRAM-SHA-256 may not work with non-ASCII passwords because SASLPrep is not fully implemented due to the lack of a string normalization function in .NET Standard 1.5. Normalizing the password into Unicode Normalization Form KC beforehand MAY help. SCRAM-SHA-1 is the recommended alternative for now. See [RFC5802] (https://tools.ietf.org/html/rfc5802) and the [SCRAM-SHA-256 section of the Driver Authentication specification](https://github.com/mongodb/specifications/blob/master/source/auth/auth.rst#scram-sha-256) for additional information. 

### x.509 Authentication

The [x.509](http://docs.mongodb.org/manual/core/authentication/#x-509-certificate-authentication) mechanism authenticates a user whose name is derived from the distinguished subject name of the x.509 certificate presented by the driver during SSL negotiation. This authentication method requires the use of [SSL connections]({{< relref "reference\driver\ssl.md" >}}) with certificate validation and is available in MongoDB 2.6 and newer. To create a credential of this type, use the following static factory method:

```csharp
var credential = MongoCredential.CreateX509Credential(username);
```

Or via the connection string:

```
mongodb://username@myserver/?authMechanism=MONGODB-X509
```

Even when using the connection string to provide the credential, the certificate must still be provided via code. This certificate can be pulled out of the trust stores on the box, or from a file. However, to be used with client authentication, the [`X509Certificate`]({{< msdnref "system.security.cryptography.x509certificates.x509certificate" >}}) provided to the driver must contain the [`PrivateKey`]({{< msdnref "system.security.cryptography.x509certificates.x509certificate2.privatekey" >}}).

```csharp
var cert = new X509Certificate2("client.pfx", "mySuperSecretPassword");

var settings = new MongoClientSettings
{
    Credentials = new[] 
    {
        MongoCredential.CreateMongoX509Credential("CN=client,OU=user,O=organization,L=Some City,ST=Some State,C=Some Country")
    },
    SslSettings = new SslSettings
    {
        ClientCertificates = new[] { cert },
    },
    UseSsl = true
};
```

### GSSAPI/Kerberos

[MongoDB Enterprise](http://www.mongodb.com/products/mongodb-enterprise) supports authentication using [Kerberos/GSSAPI](http://docs.mongodb.org/manual/core/authentication/#kerberos-authentication). To create a Kerberos/GSSAPI credential, use the following method:

```csharp
var credential = MongoCredential.CreateGssapiCredential(username, password);
```

Or via the connection string:

```
mongodb://username%40REALM.com:password@myserver/?authMechanism=GSSAPI
```

{{% note %}}Note that the username will need to have a REALM associated with it. When used in a connection string, `%40` is the escape character for the `@` symbol.{{% /note %}}

If the process owner running your application is the same as the user needing authentication, you can omit the password:

```csharp
var credential = MongoCredential.CreateGssapiCredential(username);
```

Or via the connection string:

```
mongodb://username%40REALM.com@myserver/?authMechanism=GSSAPI
```

Depending on the kerberos setup, it may be required to specify some additional properties. These may be specified in the connection string or via code.

- **CANONICALIZE_HOST_NAME**
	
	Uses the DNS server to retrieve the fully qualified domain name (FQDN) of the host.
	
	```csharp
	credential = credential.WithMechanismProperty("CANONICALIZE_HOST_NAME", "true");
	```

	Or via the connection string:

	```
	mongodb://username@myserver/?authMechanism=GSSAPI&authMechanismProperties=CANONICALIZE_HOSTNAME:true
	```

- **REALM**

	This is used when the user's realm is different from the service's realm.

	```csharp
	credential = credential.WithMechanismProperty("REALM", "otherrealm");
	```

	Or via the connection string:

	```
	mongodb://username%40REALM.com@myserver/?authMechanism=GSSAPI&authMechanismProperties=REALM:otherrealm
	```

- **SERVICE_NAME**

	This is used when the service's name is different that the default `mongodb`.

	```csharp
	credential = credential.WithMechanismProperty("SERVICE_NAME", "othername");
	```

	Or via the connection string:

	```
	mongodb://username%40REALM.com@myserver/?authMechanism=GSSAPI&authMechanismProperties=SERVICE_NAME:othername
	```

In addition, it is possible to use multiple authentication mechanism properties either via code or in the connection string. In code, call `WithMechanismProperty` multiple times. In the connection string, separate the entries with a `,` (comma).

```
mongodb://username%40REALM.com@myserver/?authMechanism=GSSAPI&authMechanismProperties=SERVICE_NAME:othername,REALM:otherrealm
```


### LDAP (PLAIN)

[MongoDB Enterprise](http://www.mongodb.com/products/mongodb-enterprise) supports proxy authentication through a Lightweight Directory Access Protocol (LDAP) service. To create a credential of type LDAP use the following static factory method:

```csharp
var credential = MongoCredential.CreatePlainCredential("$external", username, password);
```

Or via the connection string:

```
mongodb://username:password@myserver/?authSource=$external&authMechanism=PLAIN
```

{{% note %}}Note that the method refers to the plain authentication mechanism instead of LDAP because technically the driver is authenticating via the PLAIN SASL mechanism. This means that your credentials are in plain text on the wire. Therefore, PLAIN should only be used in conjunction with SSL.{{% /note %}}
