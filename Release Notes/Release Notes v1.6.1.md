C# Driver Version 1.6.1 Release Notes
=====================================

This is a minor release containing a few bug fixes, particularly related to ReadPreference support
and sending commands to secondaries.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v1.6.1.md

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.6.1-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.6.1-Driver.txt

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=12609

Changes to ReadPreference
-------------------------

The implementation of ReadPreference has been changed to more accurately follow the ReadPreference spec:

http://docs.mongodb.org/manual/applications/replication/#read-preference

The changes are:

- SecondaryPreferred only uses the Primary if no secondaries are available (regardless of latency)
- SecondayAcceptableLatency is now configurable
- when sending queries to mongos:
    - ReadPreference.Primary is encoded setting the SlaveOk bit on the wire protocol to 0
	- ReadPreference.SecondaryPreferred (without tags) is encoded setting the SlaveOk bit on the wire protocol to 1
	- all other ReadPreferences are encoded using $readPreference on the wire
    - $query is now encoded before $readPreference as required by mongos
- commands now correctly use the collection settings (they were using the database settings)

Sending commands to secondaries
-------------------------------

Only a limited set of commands are now allowed to be sent to secondaries. All other commands
will be sent to the primary regardless of the ReadPreference you specify. The commands
that can be sent to secondaries are:

- aggregate
- collStats
- count
- dbStats
- distinct
- geoNear
- geoSearch
- geoWalk
- group
- mapReduce (but *only* if using Inline results)

The corresponding helper methods in the C# driver are:

- MongoCollection.Aggregate
- MongoCollection.GetStats
- MongoCollection.Count, MongoCursor.Count and MongoCursor.Size
- MongoDatabase.GetStats
- MongoCollection.Distinct
- MongoCollection.GeoNear and MongoCollection.GeoNearAs
- MongoCollection.GeoHaystackSearch and MongoCollection.GeoHaystackSearchAs
- MongoCollection.Group
- MongoCollection.MapReduce (with MapReduceOutputMode.Inline)

There is no helper method (yet) for the geoWalk command.
