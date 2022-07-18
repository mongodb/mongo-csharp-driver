# .NET Driver Version 2.17.0 Release Notes

This is the general availability release for the 2.17.0 version of the driver.

The main new features in 2.17.0 include:

* Support for MongoDB server version 6.0.0 GA
* [BETA] Support for Queryable Encryption
* LINQ3 bug fixes and improvements
* Add arbitrary aggregation stages to LINQ queries using `IMongoQueryable.AppendStage()` method (LINQ3)
* Support for `$topN` and related accumulators in `$group` aggregation stage

### EstimatedDocumentCount and Stable API

`EstimatedDocumentCount` is implemented using the `count` server command. Due to an oversight in versions
5.0.0-5.0.8 of MongoDB, the `count` command, which `EstimatedDocumentCount` uses in its implementation,
was not included in v1 of the Stable API. If you are using the Stable API with `EstimatedDocumentCount`,
you must upgrade to server version 5.0.9+ or set `strict: false` when configuring `ServerApi` to avoid
encountering errors.

For more information about the Stable API see:

https://mongodb.github.io/mongo-csharp-driver/2.16/reference/driver/stable_api/

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.17.0.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.17.0%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

https://mongodb.github.io/mongo-csharp-driver/

