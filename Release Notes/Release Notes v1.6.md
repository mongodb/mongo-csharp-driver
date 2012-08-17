C# Driver Version 1.6 Release Notes
===================================

This is a major release featuring support for server 2.2.  The major change is
support for[read preferences](http://docs.mongodb.org/manual/applications/replication/#read-preference)
allowing tag based granularity for selecting servers to send commands and
queries to.  In addition, we have added support for SSL and a helper method
for the new aggregation framework.

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.6-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.6-Driver.txt

These release notes describe the changes at a higher level, and omits describing
some of the minor changes.

Breaking changes
----------------

- ConnectWaitFor has been removed and replaced with read preferences.  Anyone
  using the ConnectWaitFor enumeration will need to change their code.
- Commands are no longer always sent to primaries.  If you are expecting this
  behavior, ensure that your read preference is set to Primary.

JIRA issues resolved
--------------------

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?reset=true&jqlQuery=project+%3D+CSHARP+AND+fixVersion+%3D+%221.6%22+AND+status+%3D+Closed+ORDER+BY+priority+DESC&mode=hide
