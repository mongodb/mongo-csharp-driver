# .NET Driver Version 2.26.0 Release Notes

This is the general availability release for the 2.26.0 version of the driver.

The main new features in 2.26.0 include:

+ Support SelectMany inside Project/Select - [CSHARP-5081](https://jira.mongodb.org/browse/CSHARP-5081)
+ Support Dictionary.ContainsValue in LINQ queries - [CSHARP-2509](https://jira.mongodb.org/browse/CSHARP-2509)
+ Support string concatenation of mixed types - [CSHARP-5071](https://jira.mongodb.org/browse/CSHARP-5071)
+ Enable use of native crypto in libmongocrypt bindings - [CSHARP-4944](https://jira.mongodb.org/browse/CSHARP-4944)
+ Support serialization of Memory and ReadOnlyMemory structs - [CSHARP-4807](https://jira.mongodb.org/browse/CSHARP-4807)
+ OIDC: support for GCP Identity Provider - [CSHARP-4610](https://jira.mongodb.org/browse/CSHARP-4610)
+ Implement signing of NuGet packages - [CSHARP-5050](https://jira.mongodb.org/browse/CSHARP-5050)
+ Direct read/write retries to another mongos if possible - [CSHARP-3757](https://jira.mongodb.org/browse/CSHARP-3757)
+ Multiple bug fixes and improvements.

The full list of issues resolved in this release is available at [CSHARP JIRA project](https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.26.0%20ORDER%20BY%20key%20ASC).

Documentation on the .NET driver can be found [here](https://www.mongodb.com/docs/drivers/csharp/v2.26/).
