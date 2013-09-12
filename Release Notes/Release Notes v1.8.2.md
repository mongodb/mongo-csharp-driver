C#/.NET Driver Version 1.8.2 Release Notes
==========================================

This is a minor release.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v1.8.2.md

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.8.2-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.8.2-Driver.txt

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=13830

Documentation on the C#/.NET driver can be found at:

http://docs.mongodb.org/ecosystem/drivers/csharp/
http://api.mongodb.org/csharp/current/

BSON Library Changes
====================

Performance improvements
------------------------

Serialization and deserialization of enumerable types that serialize to BSON 
arrays has been sped up. The serializer for the nominal type is only looked
up once, and when using polymorphic types, the actual serializer only has to
be looked up when the actual type changes so runs of identical subtypes are
handled more efficiently.

WriteCString error checking
---------------------------

The BSON spec does not allow CStrings to have embedded nulls. The driver now
enforces this restriction thoroughly.

BsonMemberMaps are now frozen when BsonClassMap is frozen
---------------------------------------------------------

The BsonMemberMap now has a Freeze method, and BsonClassMap now calls Freeze
on all the member maps when the Freeze is called on the class map.

Better support for mutable default values
-----------------------------------------

A default value for mutable types is vulnerable to being altered by the 
application, which would affect future uses of the default value. When using
a mutable type we really need a new instance of the default value every time.
There is now a new overload of SetDefaultValue that allows you to provide a
creator function instead of a value, so the creator function can instantiate
a new instance of the default value each time one is needed.

Driver Changes
==============

Improved tracking of the primary for replica sets
-------------------------------------------------

Tracking of the current primary for replica sets has been made more reliable.
There were certain scenarios in which the driver might have two members
marked as being the current primary. With these changes there is a single 
field that tracks the most recently seen primary so by definition there will
never be more than one.

Internal restructuring
----------------------

Some changes were made to the internal implementation of the driver which do
not affect the public API. There is a new set of operation classes that
encapsulate the handling of wire protocol messages and some logic that used
to exist in MongoCollection has been moved to the operations. In addition,
the way commands are run has been refactored somewhat.

IndexCache removed
------------------

In the past drivers and the mongo shell kept track of calls to EnsureIndex in
order to optimize away additional round trips to the server for the same index.
But this approach has inherent problems, one of which is that it can't see
any changes made to the indexes by other processes. Therefore, the driver
no longer tracks calls to EnsureIndex and all calls to EnsureIndex are sent to
the server and it is up to the server to decide if the index already exists or
not. Typically this will not cause any backward compatibility problems and
the performance hit will be very small (unless you were calling EnsureIndex
very frequently).
