# .NET Driver Version 2.2.0 Release Notes

This is a minor release which supports all MongoDB server versions from 2.4 through 3.2.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.2.0.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20status%20in%20(Resolved%2C%20Closed)%20AND%20fixVersion%20%3D%202.2%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/


## New Features

Notable new features are listed below. For a full list, see the list of JIRA issues linked above.

### Sync API

The 2.0 and 2.1 versions of the .NET driver featured a new async-only API. Some users gave us feedback
that they wanted a choice whether to use a sync or an async API. Version 2.2 introduces sync versions
of every async method.

### Support for server 3.2

- Support for bypassing document validation for write operations on collections where document validation
has been enabled
- Support for write concern for FindAndModify methods
- Support for read concern
- Builder support for new aggregation stages and new accumulators in $group stage
- Support for version 3 text indexes

## Upgrading

There are no known backwards breaking changes in this release.