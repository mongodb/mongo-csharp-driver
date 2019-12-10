# .NET Driver Version 2.9.1 Release Notes

This is a patch release that fixes one bug reported since 2.9.0 was released.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.9.1.md

The list of JIRA tickets resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.9.1%20ORDER%20BY%20key%20ASC


Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/

## Upgrading

There are no known backwards breaking changes in this release.

A bug in 2.9.0 prevents applications from connecting to replica sets via SRV. Applications connecting to replica sets over SRV should NOT upgrade to 2.9.0 and instead should upgrade directly to 2.9.1 or later.
