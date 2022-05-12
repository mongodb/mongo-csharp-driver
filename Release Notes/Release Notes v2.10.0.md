# .NET Driver Version 2.10.0 Release Notes

> IMPORTANT:
>
> If you are using unacknowledged writes (also known as w:0 writes) with versions 2.10.0 or 2.10.1 of the driver, we strongly recommend you upgrade to version 2.10.2 as soon as possible, to obtain the fix for a critical issue: https://jira.mongodb.org/browse/CSHARP-2960.

The main changes in 2.10.0 are:

1. A number of minor bug fixes
2. New ReplaceOptions parameter for the ReplaceOne CRUD methods
3. Client-side field level encryption (FLE)

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.10.0.md

The list of JIRA tickets resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.10.0%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

https://mongodb.github.io/mongo-csharp-driver/

Documentation on the new client-side field level encryption feature can be found at:

https://mongodb.github.io/mongo-csharp-driver/2.10/reference/driver/crud/client_side_encryption/

## Upgrading

There are no known backwards breaking changes in this release.
