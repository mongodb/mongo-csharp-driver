+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "What's New"
[menu.main]
  weight = 20
  identifier = "What's New"
  pre = "<i class='fa fa-star'></i>"
+++

## What's New in 2.18.0

The main new features in 2.18.0 include:

* Snappy compression now uses a managed implementation
* ZStandard compression now uses a managed implementation
* Cache AWS credentials when possible
* New cross driver standard logging support
* Support for $documents aggregation pipeline stage

## What's New in 2.17.0

The main new features in 2.17.0 include:

* Support for MongoDB server version 6.0.0 GA
* [BETA] Support for Queryable Encryption
* LINQ3 bug fixes and improvements
* Add arbitrary aggregation stages to LINQ queries using `IMongoQueryable.AppendStage()` method (LINQ3)
* Support for `$topN` and related accumulators in `$group` aggregation stage

## What's New in 2.16.0

The main new features in 2.16.0 include:

* Support for MongoDB server version 6.0.0
* [BETA] Support for Queryable Encryption
* Support for creating collections with clustered indexes
* Use `count` command for `estimatedDocumentCount`
* LINQ3 bug fixes and improvements
* Support for carry forward (`$locf`) and numerical/date densification (`$densify`)
* Support for `limit` in `$filter` expressions
* Support point-in-time pre-/post-images in change streams
* Preemptively cancel in-progress operations when SDAM heartbeats timeout

## What's New in 2.15.0

The main new features in 2.15.0 include:

* Reimplement CMAP Maintance and SDAM threads to use dedicated threads
* Support for Window Functions using $setWindowFields
* Support $merge and $out executing on secondaries
* Publish symbols to NuGet.org Symbol Server and add Source Link support for improved debugging experience
* Switch to using maxWireVersion rather than buildInfo to determine feature support
* Support 'let' option for multiple CRUD commands
* Support authorizedCollections option for listCollections helpers
* Add support for 'comment' field in multiple commands for profiling
* Upgrade DnsClient.NET up to 1.6.0. This should address problems that some users have had in containerized environments like Kubernetes

## What's New in 2.14.0

The main new features in 2.14.0 include:

* Added a new improved LINQ provider (known as LINQ3, see: [LINQ3]({{< relref "reference\driver\crud\linq3.md" >}})), which is available on an opt-in basis
* The current LINQ provider (known as LINQ2) continues to be available and is still the default LINQ provider for this version
* Support for Zstandard and Snappy on Linux and MacOS
* Added connection storm avoidance features
* Added KMIP support for client-side field level encryption (CSFLE)
* Added `srvMaxHosts` URI option to limit the number of `mongos` nodes used in connecting to sharded clusters
* Use "hello" command for monitoring if supported
* Removed support for .NET Framework 4.5.2; minimum is now 4.7.2
* Removed support for .NET Standard 1.5; minimum is now 2.0
* Minimum server version is now MongoDB 3.6+

## What's New in 2.13.0

The main new features in 2.13.0 include:

* Load-balanced mode for Atlas Serverless
* Versioned MongoDB API for Drivers
* Implemented change stream oplog parsing code for delta oplog entries
* Snapshot reads on secondaries
* Support for creating time-series collections
* Permit dots and dollars in field names
* Improved error messages from document validation
* Better ExpandoObject support
* `estimatedDocumentCount()` now uses the `$collStats` aggregation stage instead of the `count` command
* Reduced lock contention in BsonSerializer.LookupActualType
* `slaveOk` connection string option removed; use `readPreference` instead

## What's New in 2.12.0

The main new features in 2.12.0 include:

* Support for Hidden Indexes in MongoDB 4.4
* Support for AWS temporary credentials in client-side field level encryption (CSFLE)
* Support for Azure and GCP keystores in client-side field level encryption (CSFLE)
* Support for client-side field level encryption (CSFLE) on Linux and Mac OSX
* Support for GSSAPI/Kerberos on Linux
* Support for .NET Standard 2.1
* Various improvements in serialization performance
* Fixed DNS failures in Kubernetes and Windows Subsystem for Linux (WSL/WSL2)
* Fixed memory leak in heartbeat when cluster is inaccessible
* Fixed SDAM deadlock when invalidating former primary

## What's New in 2.11.0

The main new features in 2.11.0 support new features in MongoDB 4.4.0. These features include:

* Support for all new
  [``$meta``](https://www.mongodb.com/docs/manual/reference/operator/projection/meta/)
  projections: `randVal`, `searchScore`, `searchHighlights`,
  `geoNearDistance`, `geoNearPoint`, `recordId`, `indexKey` and
  `sortKey`
* Support for passing a hint to update commands as well as
  `findAndModify` update and replace operations
* Support for `allowDiskUse` on find operations
* Support for `MONGODB-AWS` authentication using Amazon Web Services
  (AWS) Identity and Access Management (IAM) credentials
* Support for stapled OCSP (Online Certificate Status Protocol) (macOS only)
* Support for shorter SCRAM (Salted Challenge Response Authentication Mechanism) conversations
* Support for speculative SCRAM and MONGODB-X509 authentication
* Support for the `CommitQuorum` option in `createIndexes`
* Support for [hedged reads](https://www.mongodb.com/docs/manual/core/read-preference-hedge-option/)

Other new additions and updates in this beta include:

* A new target of .NET Standard 2.0
* Support for Snappy compression on .NET Core on Windows (in addition
  to existing support on .NET Framework)
* Support for Zstandard compression on Windows on 64-bit platforms
* A new URI option `tlsDisableCertificateRevocationCheck` to enable or disable
  certificate revocation checking.
* An expanded list of retryable write errors via the inclusion of
  `ExceededTimeLimit`, `LockTimeout` and `ClientDisconnect`
* A new GuidRepresentationMode setting to opt-in to the new V3 GuidRepresentation mode
* Improved SDAM (Server Discovery and Monitoring) error handling
* Support for the `AuthorizedDatabases` option in `ListDatabases`
* Session support for `AsQueryable`

## What's New in 2.10.0

{{% note %}}
If you are using unacknowledged writes (also known as w:0 writes) with versions 2.10.0 or 2.10.1 of the driver, we strongly recommend you upgrade to version 2.10.2 as soon as possible, to obtain the fix for a critical issue: https://jira.mongodb.org/browse/CSHARP-2960
{{% /note %}}

Some of the changes in 2.10.0 include:

* [Client-side field level encryption support]({{< relref "reference\driver\crud\client_side_encryption.md" >}}) for Windows
* Added new ReplaceOptions parameter for the ReplaceOne CRUD methods

## What's New in 2.9.0

Some of the changes in 2.9.0 include:

* Retryable writes are enabled by default
* Retryable reads support (enabled by default)
* Distributed transactions on sharded clusters
* Convenient API for transactions via `IClientSession.WithTransaction()`
* Support for message compression
* SRV polling for `mongodb+srv` connection scheme
* Update specification using an aggregation framework pipeline
* SCRAM-SHA authentication caching
* Connections to the replica set primary are no longer closed after a step-down, allowing in progress read operations to complete
* New aggregate helper methods support running database-level aggregations
* `$merge` support
* Change stream helpers now support the `startAfter` option
* Index creation helpers now support wildcard indexes

## What's New in 2.8.0

Some of the changes in 2.8.0 include:

* A number of minor bug fixes
* Update dependency on System.Net.Security to 4.3.2
* Update dependency on System.Runtime.InteropServices.RuntimeInformation to 4.3.0
* Update dependency on DnsClient to 1.2.0 and work around breaking changes in the latest version of DnsClient

## What's New in 2.7.0

The major new feature in 2.7.0 is support for new server 4.0 features including:

* Transactions
* ReadConcern Snapshot
* Change streams support extended to include all changes for an entire database or cluster
* SCRAM-SHA-256 authentication

## What's New in 2.6.0

The 2.6.0 driver improves support for running with FIPS mode enabled in the operating system.

## What's New in 2.5.0

The 2.5.0 driver adds support for many of the new features introduced by the 3.6.0 server.

### Change streams

The 2.5.0 driver adds support for [change streams](https://www.mongodb.com/docs/manual/changeStreams).
See the `Watch` and `WatchAsync` methods in [`IMongoCollection`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}}).

### Retryable writes

The 2.5.0 driver adds support for retryable writes using the `RetryWrites` setting in
[`MongoClientSettings`]({{< apiref "T_MongoDB_Driver_MongoClientSettings" >}}).

### Causal consistency

The 2.5.0 driver adds support for [causal consistency](https://www.mongodb.com/docs/manual/core/read-isolation-consistency-recency/#causal-consistency) via the new
[`IClientSession`]({{< apiref "T_MongoDB_Driver_IClientSession" >}}) API. To start a new causally consistent session
set the `CausalConsistency` property to true in [`ClientSessionOptions`]({{< apiref "T_MongoDB_Driver_ClientSessionOptions" >}})
when calling the `StartSession` or `StartSessionAsync` methods in [`IMongoClient`]({{< apiref "T_MongoDB_Driver_IMongoClient" >}}).

### Servers older than 2.6.0 are no longer supported

The 2.5.0 driver only supports versions 2.6.0 and newer of the server. Servers older than 2.6.0 are no longer supported.

## What's New in 2.4.0

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

## What's New in 2.3.0

The 2.3.0 driver is a minor release with few new features. The most notable is discussed below.

### Support for .NET Core

You can now use the .NET driver with .NET Core.

The Nuget packages target two versions of the .NET frameworks: net45 and netstandard1.5. The net45 target allows the driver to be used with the full .NET Framework
version 4.5 and later, and the netstandard1.5 target allows the driver to be used with any framework that supports netstandard1.5, which includes .NET Core 1.0.

## What's New in 2.2.0

The 2.2 driver ships with a number of new features. The most notable are discussed below.

### Sync API

The 2.0 and 2.1 versions of the .NET driver featured a new async-only API. Version 2.2 introduces sync versions of every async method.

### Support for server 3.2

* Support for bypassing document validation for write operations on collections where document validation has been enabled
* Support for write concern for FindAndModify methods
* Support for read concern
* Builder support for new aggregation stages and new accumulators in $group stage
* Support for version 3 text indexes

## What's New in 2.1.0

The 2.1 driver ships with a number of new features. The most notable are discussed below.

### GridFS

[CSHARP-1191](https://jira.mongodb.org/browse/CSHARP-1191) - GridFS support has been implemented.

### LINQ

[CSHARP-935](https://jira.mongodb.org/browse/CSHARP-935) LINQ support has been rewritten and now targets the aggregation framework. It is a more natural translation and enables many features of LINQ that were previously not able to be translated.

Simply use the new [`AsQueryable`]({{< apiref "M_MongoDB_Driver_IMongoCollectionExtensions_AsQueryable__1" >}}) method to work with LINQ.

## What's New in 2.0.0

The 2.0.0 driver ships with a host of new features. The most notable are discussed below.

### Async

As has been requested for a while now, the driver now offers a full async stack. Since it uses Tasks, it is fully usable
with async and await.

While we offer a mostly backwards-compatible sync API, it is calling into the async stack underneath. Until you are ready
to move to async, you should measure against the 1.x versions to ensure performance regressions don't enter your codebase.

All new applications should utilize the New API.

### New API

Because of our async nature, we have rebuilt our entire API. The new API is accessible via MongoClient.GetDatabase.

- Interfaces are used ([`IMongoClient`]({{< apiref "T_MongoDB_Driver_IMongoClient" >}}), [`IMongoDatabase`]({{< apiref "T_MongoDB_Driver_IMongoDatabase" >}}), [`IMongoCollection<TDocument>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}})) to support easier testing.
- A fluent Find API is available with full support for expression trees including projections.

	``` csharp
	var names = await db.GetCollection<Person>("people")
		.Find(x => x.FirstName == "Jack")
		.SortBy(x => x.Age)
		.Project(x => x.FirstName + " " + x.LastName)
		.ToListAsync();
	```

- A fluent Aggregation API is available with mostly-full support for expression trees.

	``` csharp
	var totalAgeByLastName = await db.GetCollection<Person>("people")
		.Aggregate()
		.Match(x => x.FirstName == "Jack")
		.GroupBy(x => x.LastName, g => new { _id = g.Key, TotalAge = g.Sum(x => x.Age)})
		.ToListAsync();
	```

- Support for dynamic.

	``` csharp
	var person = new ExpandoObject();
	person.FirstName = "Jane";
	person.Age = 12;
	person.PetNames = new List<dynamic> { "Sherlock", "Watson" }
	await db.GetCollection<dynamic>("people").InsertOneAsync(person);
	```

### Experimental Features

We've also include some experimental features which are subject to change. These are both based on the Listener API.

#### Logging

It is possible to see what is going on deep down in the driver by listening to core events. We've included a simple text logger as an example:

``` csharp
var settings = new MongoClientSettings
{
	ClusterConfigurator = cb =>
	{
		var textWriter = TextWriter.Synchronized(new StreamWriter("mylogfile.txt"));
		cb.AddListener(new LogListener(textWriter));
	}
};
```

#### Performance Counters

Windows Performance Counters can be enabled to track statistics like average message size, number of connections in the pool, etc...

``` csharp
var settings = new MongoClientSettings
{
	ClusterConfigurator = cb =>
	{
		cb.UsePeformanceCounters("MyApplicationName");
	}
};
```
