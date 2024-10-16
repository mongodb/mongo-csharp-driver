# .NET Driver Version 3.0.0 Release Notes
The MongoDB .NET/C# driver team is pleased to announce our v3.0.0 release! The 3.0.0 release incorporates many user-requested fixes and improvements that have been deferred as backwards-incompatible, as well as internal improvements to pay down technical debt and improve maintainability. Additional major changes include removal of a large area of the public API (mainly from MongoDB.Driver.Core), which was not intended for public use. Removed APIs are marked as deprecated in [v2.30.0](https://www.nuget.org/packages/MongoDB.Driver/2.30.0) version.
For all the breaking changes and for the upgrade guidlines, please see the [upgrade guide](https://www.mongodb.com/docs/drivers/csharp/v3.0/upgrade/v3/).

The main new features in 3.0.0 include:
- [CSHARP-4904](https://jira.mongodb.org/browse/CSHARP-4904): Adding .NET 6 target framework
- [CSHARP-4916](https://jira.mongodb.org/browse/CSHARP-4916): Removing .NETSTANDARD 2.0 target framework
- [CSHARP-5193](https://jira.mongodb.org/browse/CSHARP-5193): Removing LINQ2 provider
- [CSHARP-5233](https://jira.mongodb.org/browse/CSHARP-5233): Remove IMongoQueryable interface
- [CSHARP-4145](https://jira.mongodb.org/browse/CSHARP-4145): Improved Bulk Write API
- [CSHARP-4763](https://jira.mongodb.org/browse/CSHARP-4763): Client side projections with Find and Select
- [CSHARP-3899](https://jira.mongodb.org/browse/CSHARP-3899): Removing `MongoDB.Driver.Legacy` package
- [CSHARP-4917](https://jira.mongodb.org/browse/CSHARP-4917): Removing `MongoDB.Driver.Core` package and various non-user-facing APIs (see [v2.30.0](https://www.nuget.org/packages/MongoDB.Driver/2.30.0) for deprecation messages)
- [CSHARP-5232](https://jira.mongodb.org/browse/CSHARP-5232): Embedding MongoDB.Driver.GridFS package into `MongoDB.Driver` package
- [CSHARP-4912](https://jira.mongodb.org/browse/CSHARP-4912): Refactoring the Client Side field level description to an optional `MongoDB.Driver.Encryption` package. `MongoDB.Libmongocrypt` package is not in use anymore and will not get any further updates
- [CSHARP-4911](https://jira.mongodb.org/browse/CSHARP-4911): Refactoring the AWS authentication to an optional `MongoDB.Driver.Authentication.AWS` package
- [CSHARP-5291](https://jira.mongodb.org/browse/CSHARP-5291): Removing MONGODB-CR support
- [CSHARP-5263](https://jira.mongodb.org/browse/CSHARP-5263): Removing support for TLS1.0 and 1.1
- [CSHARP-2930](https://jira.mongodb.org/browse/CSHARP-2930): Changing default GUID serialization mode and removing GuidRepresentationMode
- [CSHARP-3717](https://jira.mongodb.org/browse/CSHARP-3717): Adding DateOnly/TimeOnly support

The full list of issues resolved in this release is available at [CSHARP JIRA project](https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%203.0.0%20ORDER%20BY%20key%20ASC).
Documentation on the .NET driver can be found [here](https://www.mongodb.com/docs/drivers/csharp/v3.0/).
