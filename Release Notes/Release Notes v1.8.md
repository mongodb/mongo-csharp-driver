C# Driver Version 1.8 Release Notes
===================================

This is a major release.

This release has two major goals: to support the new features of the 2.4 release
of the server and to begin preparing the path for some major cleanup in the 2.0
release of the driver by marking as obsolete in the 1.8 release items that we plan to
remove in the 2.0 release.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v1.8.md

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.8-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.8-Driver.txt

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=13193

Documentation on the C# driver can be found at:

http://www.mongodb.org/display/DOCS/CSharp+Language+Center
http://api.mongodb.org/csharp/current/

General changes
===============

In this release we are deprecating a large number of methods that we intend to
remove in a subsequent release. With few exceptions, these deprecated methods
still exist and continue to work as they used to, so you can choose when to
transition off of them. Because they are deprecated they will result in 
compile time warnings.

BSON Library Changes
====================

Changes to the IO classes
-------------------------

The ReadBinaryData, ReadObjectId and ReadRegularExpression methods have simplified
signatures. There is a new ReadBytes method, as well as new ReadRawBsonArray
and ReadRawBsonDocument methods.

The WriteBinaryData, WriteObjectId and WriteRegularExpression methods have simplified
signatures. There is a new WriteBytes method, as well as new WriteRawBsonArray
and WriteRawBsonDocument methods.

The settings classes for the binary readers and writers now have an Encoding property
that you can use to provide an encoding. The value you supply must be an instance of a
UTF8Encoding, but you can configure that encoding any way you want. In particular,
you would want to configure lenient decoding if you have existing data that does not
conform strictly to the UTF8 standard.

BsonDocument object model changes
---------------------------------

We are deprecating all the static Create factory methods and encourage you to
simply call the constructors instead. We are also no longer attempting to
cache any precreated instances of any BSON value (e.g. BsonInt32.Zero). These
classes are so light weight that there is no need to cache them.

A really helpful change is that you can now use indexers on documents and
arrays without having to use AsBsonDocument or AsBsonArray first. You can
now write:

    var zip = person["Addresses"][0]["Zip"].AsString;

instead of

    var zip = person["Addresses"].AsBsonArray[0].AsBsonDocument["Zip"].AsString;

The new LazyBsonDocument and LazyBsonArray classes allow you to defer
deserialization of elements until you first access them. The document or
array is kept in its raw byte form until you first access it, at which point
one level is deserialized. This can be a big performance win if you only
access some parts of a large document.

The new RawBsonDocument and RawBsonArray classes are similar to their lazy
counterparts, except that the data is always kept in raw bytes form only
and any elements you access are deserialized every time you access them.

The new Lazy and Raw classes are useful when you want to copy large documents
from one place to another and only need to examine a small part of the 
documents.

New conventions system for automatically creating class maps
------------------------------------------------------------

The BsonClassMapSerializer relies on class maps to control how it
serializes documents. These class maps have to be created. The easiest
way to create the class maps is to let the driver create them automatically,
which it does based on a number of conventions that control the process.
The implementation of the conventions system is completely new in this
release. Conventions implemented using the old conventions architecture
are still supported but will be based on deprecated classes and interfaces
and should be rewritten to use the new architecture.

Deserialization can now call constructors or static factory methods
-------------------------------------------------------------------

You can now identify constructors or static factory methods that can
be used during deserialization to instantiate objects. This means that
it is now possible to deserialize immutable classes. During deserialization
values from the serialized document are matched to parameters of a constructor
or static factory method. If there is more than one possible constructor
or factory method then the one with the most matching parameters is chosen.

Standard serializers are now created and registered as late as possible
-----------------------------------------------------------------------

The standard built-in serializers are now created and instantiated as late
as possible, giving you a chance to create and register your own if you
prefer. You can either register the standard built-in serializers with 
different default serialization options, or you can write and register
your own.

BsonDocument object model serialization standardized
----------------------------------------------------

Serialization of BsonDocument object model classes has been completely
moved to IBsonSerializer based classes. The existing ReadFrom/WriteTo
methods in the BsonDocument object model have been deprecated.

Driver Changes
==============

New authentication model
------------------------

In previous versions of the driver there was only one kind of credential
(a username/password credential) and there was a mapping that specified
which credential to use for each database being accessed. Based on that
mapping the driver would determine which credential was needed for the
database being accessed and would ensure that the connection being used
was authenticated properly.

The authentication model on the server has changed drastically, and it is no
longer required that the credential used to access a database be stored
in that same database. Therefore it is impossible to determine in advance
which credential is the one that will give you access to a database. So
now the drivers simply work with a list of credentials, with no attempt
to figure out which credential should be used with which database. Instead,
all connections are authenticated against all of the credentials supplied.

Strict enforcement of MaxConnectionIdleTime and MaxConnectionLifeTime
---------------------------------------------------------------------

Previously these values were only loosely enforced and were treated more
like hints. Now they are enforced strictly. This helps prevent network
errors in certain environments where the networking infrastructure will
terminate connections that are idle for a certain amount of time or that
have been open too long (for example, some firewalls and Azure).

Driver no longer opens and closes a connection every 10 seconds
---------------------------------------------------------------

The driver pings the state of any server it is connected to once every
10 seconds. In earlier releases it would open and close a new connection
each time. While this guaranteed that the ping would happen wihtout delay
it had the unfortunate consequence of filling the server logs with
messages reporting the opening and closing of the connections. The
driver now tries to reuse an existing connection for the ping, and will
only open a new one if it can't acquire an existing one rapidly.

Removed DeprecatedQuery and DeprecatedQueryBuilder classes
----------------------------------------------------------

These classes have been deprecated for several releases now and have
been removed entirely.

Changes to Fields builder
-------------------------

There is a new ElemMatch method to select which elements of an array
to include in the result documents.

Changes to Query builder
------------------------

The new GeoIntersects method supports the $geoIntersects query operator with
GeoJson values. In addition, new overloads of Near and Within support
using GeoJson values with these existing query operators.

Changes to Update builder
-------------------------

New SetOnInsert method to support the $setOnInsert update operator.

New overload of PushEach that has a PushEachOptions parameter that can
be used to provide advanced options to the $pushEach update operator.

Changes to Index builder
------------------------

Added support for creating hashed and 2dsphere indexes.

GeoJson object model
--------------------

The GeoJson object model is a type-safe in memory representation of GeoJSON
objects. When these objects are serialized to BSON documents the resulting
BSON documents will be valid GeoJSON documents. You can use the GeoJson
object model to represent GeoJSON properties in your POCOs and to provide
values to the new methods added to the Query builder to support GeoJson queries.

Simplified settings classes
---------------------------

The settings classes have been simplified a bit and we have standardized how
settings are stored internally and how default values are applied. See the
comments below on the different settings classes.

MongoClientSettings
-------------------

The CredentialsStore and DefaultCredentials properties have been replaced by
the new Credentials property. The CredentialsStore property was a mapping from
a database name to the credential to use to access that database. The Credentials
property is simply a list of credentials that will be used with all connections,
regardless of which database is being accessed.

There is a new SslSettings property that lets you control every aspect of an
SSL connection to the server.

The new ReadEncoding and WriteEncoding settings can be used to configure the
details of UTF8 encoding.

MongoServerSettings
-------------------

While this class has not yet been deprecated, it eventually will be. We recommend
you always use MongoClientSettings instead. 

The new settings added to MongoClientSettings have also been added to
MongoServerSettings.

MongoDatabaseSettings
---------------------

The database name has been moved out of the settings class and is now an 
argument to the GetDatabase method. Therefore the DatabaseName property
has been deprecated.

The proper way to create an instance of MongoDatabaseSettings is to call the
constructor. The CreateDatabaseSettings method of MongoServer is deprecated.

The Credentials property has been removed. It is no longer necessary (or
possible) to provide a credential at the database level.

The new ReadEncoding and WriteEncoding settings can be used to configure the
details of UTF8 encoding. If not set, they are inherited from the server
settings.

MongoCollectionSettings
-----------------------

The big change is that the name of the collection and the default document type
have been moved out of the settings class and are now arguments of the
GetCollection method. As part of this change the MongoCollection<TDefaultDocument>
subclass is deprecated, as have the CollectionName and DefaultDocumentType
properties.

The proper way to create an instance of MongoCollectionSettings is to call the
constructor. The CreateCollectionSettings method of MongoDatabase is deprecated.

The new ReadEncoding and WriteEncoding settings can be used to configure the
details of UTF8 encoding. If not set, they are inherited from the database
settings.

New LINQ features
-----------------

You can now add WithIndex to LINQ queries to provide an index hint, and you
can use Explain to have the server explain how the resulting MongoDB query
was executed.

MongoServer changes
-------------------

MongoServer no longer maintains a mapping from database settings to MongoDatabase
instances, and therefore GetDatabase no longer returns the same instance
every time it is called with the same parameters. MongoDatabase is a light
weight object so there is not much benefit in caching it, and by returning
a new instance every time, GetDatabase no longer has to use a lock to be
thread safe.

The indexers for getting a database are deprecated, so use:

    var database = server.GetDatabase("test");

instead of:

    var database = server["test"];

All of the methods that take adminCredentials have been removed. It is no
longer possible to provide admin credentials for just one operation. You
have to provide the admin credentials in your MongoClientSettings if you
need to run admin commands. If you want to keep your admin credentials
separate from your regular credentials create two (or more) instances
of MongoClient with different MongoClientSettings.

MongonDatabase changes
----------------------

MongoDatabase no longer maintains a mapping from collection settings to
MongoCollection instances, and therefore GetCollection not longer returns
the same instance every time it is called with the same parameters.
MongoCollection is a light weight object so there is not much benefit
in caching it, and by returning a new instance every time, GetCollection
no longer has to use a lock to be thread safe.

The indexers for getting a collection are deprecated, so use:

    var collection = database.GetCollection<TDocument>("test");

instead of:

    var collection = database["test"]; // note: indexers can't have type parameters

One reason we are deprecating the indexers is that they can't have type
parameters, so there is no way to specify the default document type when
using an indexer.

All of the methods that take adminCredentials have been removed. It is no
longer possible to provide admin credentials for just one operation. You
have to provide the admin credentials in your MongoClientSettings if you
need to run admin commands.

MongoCollection changes
-----------------------

InsertBatch has always had the capability to break large batches into smaller
batches that fit within the server's maximum message length. There has been a
slight change in 1.8 to how those sub batches are processed. If you are using
a WriteConcern of Unacknowledged and the ContinueOnError flag is false, then
the driver will insert a GetLastError command after each sub batch except the
last to detect whether an error occurred. This is necessary to implement the
semantics of ContinueOnError being false, where an error in one of the sub
batches should prevent all following sub batches from being sent.
