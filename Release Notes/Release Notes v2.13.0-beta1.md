# .NET Driver Version 2.13.0-beta1 Release Notes

This is a beta release for the 2.13.0 version of the driver.

The main new features in 2.13.0-beta1 include:

* Versioned MongoDB API for Drivers
* Implemented change stream oplog parsing code for delta oplog entries
* `estimatedDocumentCount()` now uses the `$collStats` aggregation stage instead of the `count` command
* Reduced lock contention in BsonSerializer.LookupActualType

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.13.0-beta1.md

The full list of JIRA issues that are currently scheduled to be resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.13.0%20ORDER%20BY%20key%20ASC

The list may change as we approach the release date.

Documentation on the .NET driver can be found at:

https://mongodb.github.io/mongo-csharp-driver/

