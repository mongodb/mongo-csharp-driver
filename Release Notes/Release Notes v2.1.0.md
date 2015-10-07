# .NET Driver Version 2.1.0-rc1 Release Notes

(Preliminary)

This is a minor release which supports all MongoDB server versions since 2.4.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.1.0.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?filter=18283

Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/


## New Features

Notable new features are listed below. For a full list, see the full list of JIRA issues linked above.


### GridFS

[CSHARP-1191](https://jira.mongodb.org/browse/CSHARP-1191) - GridFS support has been implemented.


### LINQ

[CSHARP-935](https://jira.mongodb.org/browse/CSHARP-935) LINQ support has been rewritten and now targets the aggregation framework. It is a more natural translation and enables many features of LINQ that were previously not able to be translated.

Simply use the new [`AsQueryable`]({{< apiref "M_MongoDB_Driver_IMongoCollectionExtensions_AsQueryable__1" >}}) method to work with LINQ.


### Eventing

[CSHARP-1374](https://jira.mongodb.org/browse/CSHARP-1374) - An eventing API has been added allowing a user to subscribe to one or more events from the core driver for insight into server discovery, server selection, connection pooling, and commands.


## Upgrading

There are no known backwards breaking changes in this release.