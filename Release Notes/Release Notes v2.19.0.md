# .NET Driver Version 2.19.0 Release Notes

This is the general availability release for the 2.19.0 version of the driver.

The main new features in 2.19.0 include:

* Atlas Search builders
* Default LinqProvider changed to LINQ3
* Support for Range Indexes preview
* ObjectSerializer allowed types configuration
* Bucket and BucketAuto stages support in LINQ3
* Support Azure VM-assigned Managed Identity for Automatic KMS Credentials
* Native support for AWS IAM Roles

This version addresses [CVE-2022-48282](https://www.cve.org/CVERecord?id=CVE-2022-48282).

### ObjectSerializer allowed types configuration

The `ObjectSerializer` has been changed to only allow deserialization of types that are considered safe. 
What types are considered safe is determined by a new configurable `AllowedTypes` function (of type `Func<Type, bool>`).
The default `AllowedTypes` function is `ObjectSerializer.DefaultAllowedTypes` which returns true for a number of well-known framework types that we have deemed safe.
A typical example might be to allow all the default allowed types as well as your own types. This could be accomplished as follows:

```
var objectSerializer = new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith("MyNamespace"));
BsonSerializer.RegisterSerializer(objectSerializer);
```

More information about the `ObjectSerializer` is available in [our FAQ](https://www.mongodb.com/docs/drivers/csharp/v2.19/faq).

### Default LinqProvider changed to LINQ3
Default LinqProvider has been changed to LINQ3.
LinqProvider can be changed back to LINQ2 in the following way:

```
var connectionString = "mongodb://localhost";
var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
clientSettings.LinqProvider = LinqProvider.V2;
var client = new MongoClient(clientSettings);
```
If you encounter a bug in LINQ3 provider, please report it in [CSHARP JIRA project](https://jira.mongodb.org/projects/CSHARP/issues).

An online version of these release notes is available [here](https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.19.0.md).

The full list of issues resolved in this release is available at [CSHARP JIRA project](https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.19.0%20ORDER%20BY%20key%20ASC).

Documentation on the .NET driver can be found [here](https://www.mongodb.com/docs/drivers/csharp/v2.19/).
