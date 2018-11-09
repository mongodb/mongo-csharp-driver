# .NET Driver Version 2.7.2 Release Notes

This is a patch release that fixes one bug reported since 2.7.1 was released.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/v2.7.x/Release%20Notes/Release%20Notes%20v2.7.2.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.7.2%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/

Upgrading

There are no known backwards breaking changes in this release.

If your application is running on Linux or OS X and you were planning to upgrade
to the 2.7.1 release of the driver, you must upgrade to 2.7.2 or later rather than 2.7.1.

In the 2.7.1 release, the driver enables TCP KeepAlive and configures the
KeepAlive interval, but the method that it uses throws a PlatformNotSupportedException
on Linux and OS X. In the 2.7.2 release the driver catches that exception rather than
failing to connect, and falls back to simply enabling KeepAlive. If that also throws a
PlatformNotSupportedException, it will connect without enabling KeepAlive.
