# .NET Driver Version 3.1.0 Release Notes

This is the general availability release for the 3.1.0 version of the driver.

The main new features in 3.1.0 include:

+ Support token field type and array field expressions with Atlas Search builders for equals operator - [CSHARP-4926](https://jira.mongodb.org/browse/CSHARP-4926)
+ Support `SearchIndexType` option when creating Atlas Search indexes - [CSHARP-4960](https://jira.mongodb.org/browse/CSHARP-4960)
+ Support for valid SRV hostnames with less than 3 parts - [CSHARP-5200](https://jira.mongodb.org/browse/CSHARP-5200)
+ Support for search sequential pagination - [CSHARP-5420](https://jira.mongodb.org/browse/CSHARP-5420)
+ Support for Mql methods: `Exists`, `IsMissing` and `IsNullOrMissing` in filters when possible - [CSHARP-5427](https://jira.mongodb.org/browse/CSHARP-5427)
+ Support for Exact Vector Search (ENN) - [CSHARP-5212](https://jira.mongodb.org/browse/CSHARP-5212)
+ Allow sort option to be supplied to update commands (updateOne, etc.) - [CSHARP-5201](https://jira.mongodb.org/browse/CSHARP-5201)
+ Disabled TLS renegotiation when possible - [CSHARP-2843](https://jira.mongodb.org/browse/CSHARP-2843)
+ Fix a bug in discriminator convention inheritance - [CSHARP-5349](https://jira.mongodb.org/browse/CSHARP-5349)
+ New Serializers for ImmutableArray and other immutable collections - [CSHARP-5335](https://jira.mongodb.org/browse/CSHARP-5335)
+ Minor bug fixes and improvements.

The full list of issues resolved in this release is available at [CSHARP JIRA project](https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%203.1.0%20ORDER%20BY%20key%20ASC).

Documentation on the .NET driver can be found [here](https://www.mongodb.com/docs/drivers/csharp/v3.1/).
