C#/.NET Driver Version 1.8.3 Release Notes
==========================================

This is a minor release but is a recommended upgrade for all 1.8.2 users, particularly
if you use the limit option with queries (1.8.3 fixes a performance issue that occurs
when small limits are used with queries that would otherwise return a large number of
results).

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/v1.8.3/Release%20Notes/Release%20Notes%20v1.8.3.md

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/v1.8.3/Release%20Notes/Change%20Log%20v1.8.3-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/v1.8.3/Release%20Notes/Change%20Log%20v1.8.3-Driver.txt

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?filter=14059

Documentation on the C#/.NET driver can be found at:

http://docs.mongodb.org/ecosystem/drivers/csharp/
http://api.mongodb.org/csharp/current/

BSON Library Changes
====================

Id mapping change
-----------------

If the Id field is declared abstract or virtual in a base class and later overridden
in a derived class we were incorrectly mapping it twice. We now only map it once in 
the class where it is first declared.

Driver Changes
==============

Better handling of server state when errors occurr
--------------------------------------------------

When an error occurred on one connection we were setting the server instance state
to Unknown, which caused problems on other connections. Now we simply tell the server
instance to refresh its state as soon as possible.

GeoJson deserialization accepts numeric types besides doubles
-------------------------------------------------------------

In 1.8.2 the GeoJson deserializers required that the coordinate values be stored as
doubles. In 1.8.3 the Serialize method still stores them as doubles but the Deserialize
method can convert from other numeric types to doubles.

Id generators
-------------

The container parameter to the GenerateId method of the IIdGenerator interface was
inadvertently changed in 1.8.2. In 1.8.3 the value of the container parameter is once
again the MongoCollection instance for which an Id needs to be generated.

Performance problem with queries that used limit
------------------------------------------------

The Execute method of the new QueryOperation class would in certain cases fetch one
more batch of results than necessary from the server. This did not affect the correctness
of the results but could result in a substantial loss of performance. This has been
fixed in 1.8.3.

Save method and _id values
--------------------------

The Save method has been simplified to correctly serialize the _id value for all
possible serializers. The new approach is slightly less efficient in some cases but
is the only approach that will work if custom serializers are being used. Since all 
Save does is call either Insert or Update you can get slightly better performance 
by calling Insert or Update yourself (call Insert if you know it's a new document
or call Update with an appropriate query and the Upsert flag if you are not sure).
