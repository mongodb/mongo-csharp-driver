C#/.NET Driver Version 2.0.0-rc0 Release Notes
==============================================

(Preliminary)

This is a major release which supports all MongoDB server versions since 2.4.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.0.0.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=16826

Documentation on the .NET driver can be found at:

http://www.mongodb.org/display/DOCS/CSharp+Language+Center
http://api.mongodb.org/csharp/current/

New Features
============

Notable new features are listed below. For a full list, see the full list of JIRA issues linked above.

Async
-----

As has been requested for a while now, the driver now offers a full async stack. Since it uses Tasks, it is fully usable
with async and await. 

While we offer a mostly backwards-compatible sync API, it is calling into the async stack underneath. Until you are ready
to move to async, you should measure against the 1.x versions to ensure performance regressions don't enter your codebase.

All new applications should utilize the New API.

New Api
-------

Because of our async nature, we have rebuilt our entire API. The new API is accessible via MongoClient.GetDatabase. 

- Interfaces are used (IMongoClient, IMongoDatabase, IMongoCollection&lt;TDocument&gt;) to support easier testing.

- A fluent Find API is available with full support for expression trees including projections.

```csharp
	var people = await db.GetCollection<Person>("people")
		.Find(x => x.FirstName == "Jack")
		.SortBy(x => x.Age)
		.Projection(x => x.FirstName + " " + x.LastName)
		.ToListAsync();
```

- A fluent Aggregation API is available with mostly-full support for expression trees (Unwind is special).

```csharp
	var people = await db.GetCollection<Person>("people")
		.Aggregate()
		.Match(x => x.FirstName == "Jack")
		.GroupBy(x => x.LastName, g => new { _id = g.Key, TotalAge = g.Sum(x => x.Age)})
		.ToListAsync();
```

- Support for dynamic.

```csharp
	var person = new ExpandoObject();
	person.FirstName = "Jane";
	person.Age = 12;
	person.PetNames = new List<dynamic> { "Sherlock", "Watson" }
	await db.GetCollection<dynamic>("people").InsertOneAsync(person);
```

Experimental Features
=====================

One experimental feature has been introduced while we work through the best API both for us and for the community
of other drivers. As such, it is public and we welcome feedback, but should not be considered stable and we will not make
a major version bump when/if they are changed.

- A listener API to hook into events deep in the driver. Some uses of this are for logging or performance counters. We've included
a simple logging abstraction to a TextWriter as well as a PerformanceCounter implementation.


Breaking Changes
================

This is a major revision and we have taken the opportunity to make some necessary changes. These changes will not affect everyone. Below
are some that may affect a greater number of people:

- .NET 3.5 and .NET 4.0 are no longer supported. If you still must use these platforms, then the 1.x series of the driver will continue to be developed.

- The nuget package mongocsharpdriver now includes the legacy driver. It depends on 3 new nuget packages, MongoDB.Bson, MongoDB.Driver.Core, 
and MongoDB.Driver. MongoDB.Driver is the replacement for mongocsharpdriver.

- We are no longer strong naming (CSHARP-616) our assemblies. Our previous strong naming was signed with a key in our public repository. This did 
nothing other than satisfy certain tools. If you need MongoDB assemblies to be strongly named, it is relatively straight-forward to build the
assemblies yourself.

- We've removed support for partially trusted callers (CSHARP-952).

- MongoConnectionStringBuilder (CSHARP-979) has been removed. Use the documented mongodb connection string format and/or MongoUrlBuilder.

- MongoServer is a deprecated class. Anyone using MongoClient.GetServer() will encounter a deprecation warning and, depending on how your build is
setup, may receive an error. It is still safe to use this API until your code is ported to the new API.

- Improved the BsonSerializer infrastructure (CSHARP-933). Anyone who has written a custom serializer will be affected by this. The changes are minor,
but were necessary to support dynamic serializers as well as offering great speed improvements and improved memory management.

- ReadPreference(CSHARP-1043) and WriteConcern(CSHARP-1044) were rewritten. These classes are now immutable. Any current application
code that sets values on these classes will no longer function. Instead, you should use the With method to alter a ReadPreference or WriteConcern.
	var writeConcern = myCurrentWriteConcern.With(journal: true);

- Dynamic DictionaryRepresentation (CSHARP-939) has been removed. Its intent was to store, in some manner, anything in a .NET dictionary. In practice,
this leads to the same values getting stored in different ways depending on factors such as a "." inside the key name. We made the decision to
eliminate this variability. This means that documents that used to serialize correctly may start throwing a BsonSerializationException with a message
indicating the key must be a valid string. CSHARP-1165 has a solution to this problem. It should be noted that we will continue to read these disparate
representations without error.