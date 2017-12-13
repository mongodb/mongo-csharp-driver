# .NET Driver Version 2.5.0 Release Notes

The main new feature of 2.5.0 is support for the new features of the 3.6 version of the server:

* Sessions
* Causal consistency
* Retryable writes
* Change streams via the collection Watch method to observe changes to a collection
* Array filters for update operations
* Translating DateTime expressions in LINQ to $dateFromParts and $dateFromString operators
* The new "mongodb+srv://" connection string scheme
* Improved support for reading and writing UUIDs in BsonBinary subtype 4 format

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.5.0.md

The JIRA tickets resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.5%20ORDER%20BY%20key%20ASC

Upgrading

We believe there are only minor breaking changes in classes that normally would not be directly used by applications.

MongoDB server versions below version 2.6 are no longer supported.
