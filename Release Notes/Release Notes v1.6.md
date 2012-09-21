C# Driver Version 1.6 Release Notes
===================================

This is a major release featuring support for server 2.2. The major change is
support for read preferences, allowing tag based granularity for selecting
servers to send commands and queries to. In addition, we have added support
for SSL and a helper method for the new aggregation framework.

An online version (with possible corrections) of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v1.6.md

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.6-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.6-Driver.txt

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=12011

Information about read preferences is available online at:

http://docs.mongodb.org/manual/applications/replication/#read-preference

These release notes describe the changes at a higher level, and omits describing
some of the minor changes.

Breaking changes
----------------

- Commands are no longer always sent to primaries.  If you are expecting this
  behavior, ensure that your read preference is set to Primary.
- ConnectWaitFor has been removed and replaced with read preferences.  Anyone
  using the ConnectWaitFor enumeration will need to change their code.
- The serialized representation for a C# null of type BsonNull has been changed
  from { $csharpnull : true } to { _csharpnull : true } to work around limitations
  of the server. Existing data will still be correctly deserialized but new data
  will be written in the new format. This is very unlikely to affect you because
  it is very unlikely you have any properties of type BsonNull in your classes.

New features
------------

- There is a new \[BsonSerializer] attribute that can be used to specify which
  serializer to use for a class.
- Instances of ReadOnlyCollection are now serializable/deserializable.
- Queries involving Mod now work with 64-bit integers also.
- Support for TTL collections (see IndexOptions.SetTimeToLive).
- Simple helper method for aggregation framework (see MongoCollection.Aggregate).
- SlaveOK has been deprecated and replaced with the more flexible ReadPreference options.
- Support for SSL connections.
- Improved support for LINQ queries from VB.NET.
- Support for connecting to multiple mongos’ with load balancing
