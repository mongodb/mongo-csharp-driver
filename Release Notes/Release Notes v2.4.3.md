# .NET Driver Version 2.4.3 Release Notes

This is a patch release that fixes a few bugs reported since 2.4.2 was released.

Most of the changes are minor, but if you use X509 certificates with SSL you should
definitely upgrade to 2.4.3. See:

https://jira.mongodb.org/browse/CSHARP-1914

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.4.3.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.4.3%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/

Upgrading

There are no known backwards breaking code changes in this release.

There is a minor backward breaking change in how the filter builders serialize values when the specified field type does
not match the actual field type. See:

https://jira.mongodb.org/browse/CSHARP-1975
