# .NET Driver Version 2.13.0 Release Notes

This is the general availability release for the 2.13.0 version of the driver.

The main new features in 2.13.0 include:

* Load-balanced mode for Atlas Serverless
* Versioned MongoDB API for Drivers
* Implemented change stream oplog parsing code for delta oplog entries
* Snapshot reads on secondaries
* Support for creating time-series collections
* Permit dots and dollars in field names
* Improved error messages from document validation
* Better ExpandoObject support
* `estimatedDocumentCount()` now uses the `$collStats` aggregation stage instead of the `count` command
* Reduced lock contention in BsonSerializer.LookupActualType
* `slaveOk` connection string option removed; use `readPreference` instead

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.13.0.md

The full list of JIRA issues that are resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.13.0%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

https://www.mongodb.com/docs/drivers/csharp/

