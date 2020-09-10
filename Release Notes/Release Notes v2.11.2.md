# .NET Driver Version 2.11.2 Release Notes

This is a patch release that fixes a bug reported since 2.11.1 was released.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.11.2.md

The list of JIRA tickets resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.11.2%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/

## Upgrading

Everyone using versions 2.11.0 or 2.11.1 of the C# driver with version 4.4.0 or later of the server should upgrade to 2.11.2.
The issue fixed is related to simultaneous authentication of two or more connections, in which case a change introduced
in 2.11.0 can result in authentication failing. Under loads low enough that only one connection is ever opened at the same
time the issue does not happen.

See: https://jira.mongodb.org/browse/CSHARP-3196

There are no known backwards breaking changes in this release.
