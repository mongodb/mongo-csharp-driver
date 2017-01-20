+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "What's New"
[menu.main]
  weight = 20
  identifier = "What's New"
  pre = "<i class='fa fa-star'></i>"
+++

## What's new in 2.4.0

The 2.4.0 driver is a minor release that adds support for new features introduced in server 3.4 and fixes bugs reported since 2.3.0 was released.

### Decimal128 data type

Server 3.4 introduces the new decimal BSON data type. Version 2.4.0 of the .NET Driver adds support for the decimal BSON data type with the new BsonDecimal128
and Decimal128 classes. BsonDecimal128 is the subclass of BsonValue for Decimal128 values, and the Decimal128 struct holds the actual value. This is the same
relationship that exists between the BsonObjectId and ObjectId classes.

There are no arithmetic operations provided on Decimal128 values, but Decimal128 values can be converted to C# decimal or double values and you can operate on those.

### New convention for automapping immutable classes for serialization

The new ImmutableTypeClassMapConvention supports automapping immutable classes for automatic serialization using the BsonClassMapSerializer.

### EnumRepresentationConvention now applies to nullable enums also

The EnumRepresentationConvention can be used to specify the representation of enum values, but prior to 2.4.0 it did not apply to nullable enums.
This convention has been enhanced to apply to both nullable and regular enums.

### Drivers now identify themselves to the server when connecting

Drivers now identify themselves to the server when they connect and this information is logged by the server. Applications can add an application name to the
information sent to the server, either by adding "applicationName=xyz" to the connection string or by using the ApplicationName property of MongoClientSettings.

### ReadPreference support for MaxStaleness

ReadPreference has a new MaxStaleness property that can be used when reading from secondaries to prevent reading from secondaries that are too far behind the primary.

### New Linearizable ReadConcernLevel

There is a new ReadConcernLevel called Linearizable in addition to the existing Local and Majority levels. You specify the read concern level by assigning a value to 
the ReadConcernLevel property of a ReadConcern value.

### Support for collations

The following database methods now support collations:

- CreateCollection/CreateCollectionAsync

The following collection methods now support collations:

- Aggregate fluent API
- Aggregate/AggregateAsync
- BulkWrite/BulkWriteAsync
- Count/CountAsync
- DeleteMany/DeleteManyAsync
- DeleteOne/DeleteOneAsync
- Distinct/DistincAsync
- Find fluent API
- FindSync/FindAsync
- FindOneAndDelete/FindOneAndDeleteAsync
- FindOneAndReplace/FindOneAndReplaceAsync
- FindOneAndUpdate/FindOneAndUpdateAsync
- MapReduce/MapReduceAsync
- ReplaceOne/ReplaceOneAsync
- UpdateMany/UpdateManyAsync
- UpdateOne/UpdateOneAsync

The following index management metods now support collations:

- CreateMany/CreateManyAsync

All new methods support collations where appropriate as well.

### Support for creating read-only views

The IMongoDatabase interface has new CreateView/CreateViewAsync methods that support creating read-only views.

### Support for new aggregation framework pipeline operators

The aggregate fluent API supports the following new aggregation pipeline operators:

- Bucket ($bucket)
- BucketAuto ($bucketAuto)
- Count ($count)
- Facet ($facet)
- GraphLookup ($graphLookup)
- ReplaceRoot ($replaceRoot)
- SortByCount ($sortByCount)

LINQ now supports the following additional LINQ methods and maps them to equivalent aggregation pipeline operator(s):

- Aggregate ($reduce)
- Average ($avg)
- IndexOf ($indexOfBytes or $indexOfCP)
- Length ($strLenBytes or $strLenCP)
- Range ($range)
- Reverse ($reverseArray)
- Split ($split)
- Substr ($substr or $substrCP)
- Zip ($zip)

### New Aggregate Pipeline and Stage builders

In earlier versions of the driver the aggregate fluent API had methods supporting creating a pipeline to be
executed when the aggregate fluent object was executed, but there was no way to build a standalone pipeline value.

With the introduction of the CreateView method and Facet pipeline operators there is now a need to create
pipelines separately from the aggregate fluent API. 

The new PipelineDefinitionBuilder class can be used to create pipelines.

### Aggregate and LINQ expressions now use the underlying field serializer to serialize constant values

When building pipelines and converting LINQ expressions to pipelines constants are now serialized using the
underlying field serializer so that the values are serialized the same way that they are stored in the database.

### ClusterRegistry has a new UnregisterAndDisposeCluster method

Normally the driver creates a pool of connections to a server the first time that server is used. This is
advantageous because it speeds up subsequent uses of that server since there are already open connections
that can be re-used.

But sometimes users write applications that connect to hundreds of servers. If these applications only use
these servers for a brief period of time and then don't use them anymore the resulting connection pool will
continue to hold a number of open connections that are no longer needed.

If you find yourself needing to shut down a connection pool you can use the new UnregisterAndDisposeCluster
method of the ClusterRegistry class.
