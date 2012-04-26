C# Driver Version 1.4.2 Release Notes
=====================================

This minor release fixes a few issues found in the 1.4.1 release.

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.4.2-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.4.2-Driver.txt

These release notes describe the changes at a higher level, and omit describing
some of the minor changes.

Breaking changes
----------------

After 1.4.1 was released it was discovered that there were some minor breaking
changes. The breaking changes were in methods that we considered to be internal,
but that were not made private so that they leaked out into the public API.
Those methods have now been marked obsolete and will be made private in 
a future release. The 1.4.2 release restores backward compatibility for these
methods (GetDocumentId and SetDocumentId in BsonDocument).

JIRA issues resolved
--------------------

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=11409

BSON library changes
====================

GetDocumentId/SetDocumentId marked obsolete
-------------------------------------------

These methods were intended to be private. They have been marked as obsolete
and will be made private in a future release.

Driver changes
==============

Query.All/In/NotIn
------------------

There was an issue with Query.All/In/NotIn that might have affected you. If you
cast a BsonArray to IEnumerable&lt;BsonValue&gt; before calling Query.All/In/NotIn
you would get an exception. This only happened when casting a BsonArray to
IEnumerable&lt;BsonValue&gt;. If you passed a BsonArray to the BsonArray overload or
passed an IEnumerable&lt;BsonValue&gt; that was not a BsonArray to the
IEnumerable&lt;BsonValue&gt; overload no exception was thrown.

RequestStart/RequestDone
------------------------

Calling RequestStart when the connection pool was oversubscribed would often
result in a deadlock. This has been fixed in the 1.4.2 release.

Ping/VerifyState
----------------

These methods are usually called from a timer to monitor the state of the
server (or of multiple servers if connected to a replica set), but you can
also call them yourself. These methods now use a new connection instead
of one from the connection pool so that they are not delayed waiting for a
connection when the connection pool is oversubscribed.
