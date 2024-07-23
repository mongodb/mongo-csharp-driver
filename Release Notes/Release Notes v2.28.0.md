# .NET Driver Version 2.28.0 Release Notes

This is the general availability release for the 2.28.0 version of the driver.

NOTICE: MongoDB 3.6 reached end-of-life in April 2021. The .NET/C# Driver will be removing support for MongoDB 3.6 in an upcoming release.

The main new features in 2.28.0 include:

+ Provide Strong-Named Assemblies - [CSHARP-1276](https://jira.mongodb.org/browse/CSHARP-1276)
+ Support additional numeric conversions involving Nullable<T> - [CSHARP-5180](https://jira.mongodb.org/browse/CSHARP-5180)
+ CSFLE/QE KMIP support "delegated" protocol - [CSHARP-4941](https://jira.mongodb.org/browse/CSHARP-4941)

## Bug fixes:
+ Verify that operands to numeric operators in LINQ expressions are represented as numbers on the server - [CSHARP-4985](https://jira.mongodb.org/browse/CSHARP-4985)
+ IReadOnlyDictionary indexer access fails to translate in v3 - [CSHARP-5171](https://jira.mongodb.org/browse/CSHARP-5171)
+ Projection Expressions Fail to Deserialize Data Correctly - [CSHARP-5162](https://jira.mongodb.org/browse/CSHARP-5162)
+ Enum conversion within IQueryable fails with Expression not supported exception - [CSHARP-5043](https://jira.mongodb.org/browse/CSHARP-5043)
+ IMongoCollection.AsQueryable().Select() fails for array type (regression) - [CSHARP-4957](https://jira.mongodb.org/browse/CSHARP-4957)

The full list of issues resolved in this release is available at [CSHARP JIRA project](https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.28.0%20ORDER%20BY%20key%20ASC).

Documentation on the .NET driver can be found [here](https://www.mongodb.com/docs/drivers/csharp/v2.28/).
