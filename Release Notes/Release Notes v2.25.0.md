# .NET Driver Version 2.25.0 Release Notes

This is the general availability release for the 2.25.0 version of the driver.

NOTICE: MongoDB 3.6 reached end-of-life in April 2021. The .NET/C# Driver will be removing support for MongoDB 3.6 in an upcoming release.

The main new features in 2.25.0 include:
+ Support of MONGODB-OIDC Authentication mechanism - [CSHARP-4448](https://jira.mongodb.org/browse/CSHARP-4448)
+ MONGODB-OIDC: Automatic token acquisition for Azure Identity Provider - [CSHARP-4474](https://jira.mongodb.org/browse/CSHARP-4474)
+ Improved error message when no matching constructor found - [CSHARP-5007](https://jira.mongodb.org/browse/CSHARP-5007)
+ Driver Container and Kubernetes Awareness - [CSHARP-4718](https://jira.mongodb.org/browse/CSHARP-4718)
+ Logging of executed MQL for a LINQ query - [CSHARP-4684](https://jira.mongodb.org/browse/CSHARP-4684)
+ Allow custom service names with srvServiceName URI option - [CSHARP-3745](https://jira.mongodb.org/browse/CSHARP-3745)
+ BulkWrite enumerates requests argument only once - [CSHARP-1378](https://jira.mongodb.org/browse/CSHARP-1378)
+ Support of Range Explicit Encryption - [CSHARP-5009](https://jira.mongodb.org/browse/CSHARP-5009)
+ Multiple bug fixes and improvements.

The full list of issues resolved in this release is available at [CSHARP JIRA project](https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.25.0%20ORDER%20BY%20key%20ASC).

Documentation on the .NET driver can be found [here](https://www.mongodb.com/docs/drivers/csharp/v2.25/).
