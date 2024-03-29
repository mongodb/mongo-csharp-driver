C#/.NET Driver Version 1.8.1 Release Notes
==========================================

This is a minor release. It should be considered a mandatory upgrade from 1.8. The
issues fixed were minor changes but their impact is not minor. In particular, if
you are using replica sets or are using InsertBatch with very large batches you
should consider 1.8.1 a mandatory upgrade.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v1.8.1.md

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.8.1-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.8.1-Driver.txt

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=13252

Documentation on the C#/.NET driver can be found at:

https://mongodb.github.io/mongo-csharp-driver/

BSON Library Changes
====================

Handling of closed sockets
--------------------------

There was an unfortunate regression in 1.8 with respect to sockets closed by the
server which causes the driver to hang waiting for data that is never going to
arrive.

This was discovered by a user who called Shutdown (which of course resulted in
all sockets being closed), but you could also encounter this issue if
you are connecting to replica sets and the primary closes all sockets when it
steps down (either due to an explicit step down or a re-election).

AscendingGuidGenerator
----------------------

If you are using Guids as your _ids and you want the values to be ascending so
that they are always inserted at the right hand side of the index you can use
this IdGenerator. Note that for the server to see the Guids as ascending you
have to make sure to store them in the right representation, which is
GuidRepresentation.Standard.

We used to recommend CombGuidGenerator for this use case, but we have since
realized that the Guids generated by the CombGuid are only considered ascending
when Guids are compared using SQL Server's method of comparing Guids.

Driver Changes
==============

MongoCollection
---------------

There is a new overload of Distinct which returns the values as TValue(s)
instead of as BsonValue(s).

There was a bug in InsertBatch that resulted in a high probability of
InsertBatch failing if the batch was big enough to have to be split into
multiple sub-batches.

MongoDatabase
-------------

RunCommandAs now uses the standard serialization mechanisms to deserialize
all command results returned from the server.
