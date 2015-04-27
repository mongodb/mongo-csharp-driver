# .NET Driver Version 2.0.0 Release Notes

This is a major release which supports all MongoDB server versions since 2.4.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.0.0.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?filter=16826

Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/

## New Features

Notable new features are listed below. For a full list, see the full list of JIRA issues linked above.

### Async

As has been requested for a while now, the driver now offers a full async stack. Since it uses Tasks, it is fully usable
with async and await. 

While we offer a mostly backwards-compatible sync API, it is calling into the async stack underneath. Until you are ready
to move to async, you should measure against the 1.x versions to ensure performance regressions don't enter your codebase.

All new applications should utilize the New API.


### New API

Because of our async nature, we have rebuilt our entire API. The new API is accessible via MongoClient.GetDatabase. 

- Interfaces are used (`IMongoClient`, `IMongoDatabase`, `IMongoCollection<TDocument>`) to support easier testing.

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


## Upgrading

As 2.0 is a major revision, there are some breaking changes when coming from the 1.x assemblies. We've tried our best to mitigate those breaking changes, but some were inevitable. These changes may not affect everyone, but take a moment to review the list of known changes below:


### System Requirements

- .NET 3.5 and .NET 4.0 are no longer supported. If you still must use these platforms, the 1.x series of the driver will continue to be developed.
- [CSHARP-952](https://jira.mongodb.org/browse/CSHARP-952): We've removed support for partially trusted callers.


### Packaging

- The nuget package [mongocsharpdriver](http://nuget.org/packages/mongocsharpdriver) now includes the legacy driver. It depends on 3 new nuget packages, [MongoDB.Bson](http://nuget.org/packages/MongoDB.Bson), [MongoDB.Driver.Core](http://nuget.org/packages/MongoDB.Driver.Core), and [MongoDB.Driver](http://nuget.org/packages/MongoDB.Driver). [MongoDB.Driver](http://nuget.org/packages/MongoDB.Driver) is the replacement for [mongocsharpdriver](http://nuget.org/packages/mongocsharpdriver).
- [CSHARP-616](https://jira.mongodb.org/browse/CSHARP-616): We are no longer strong naming  our assemblies. Our previous strong naming was signed with a key in our public repository. This did nothing other than satisfy certain tools. If you need MongoDB assemblies to be strongly named, it is relatively straight-forward to build the assemblies yourself.


### BSON

- [CSHARP-933](https://jira.mongodb.org/browse/CSHARP-933): Improved the BSON Serializer infrastructure. Anyone who has written a custom serializer will be affected by this. The changes are minor, but were necessary to support dynamic serializers as well as offering great speed improvements and improved memory management.
- Certain methods, such as `BsonMemberMap.SetRepresentation` have been removed. The proper way to set a representation, for instance, would be to use `SetSerializer` and configure the serializer with the appropriate representation.
- [CSHARP-939](https://jira.mongodb.org/browse/CSHARP-939): Dynamic DictionaryRepresentation has been removed. Its intent was to store, in some manner, anything in a .NET dictionary. In practice, this leads to the same values getting stored in different ways depending on factors such as a "." inside the key name. We made the decision to eliminate this variability. This means that documents that used to serialize correctly may start throwing a BsonSerializationException with a message indicating the key must be a valid string. [CSHARP-1165](https://jira.mongodb.org/browse/CSHARP-1165) has a solution to this problem. It should be noted that we will continue to read these disparate representations without error.


### Driver
- [CSHARP-979](https://jira.mongodb.org/browse/CSHARP-979): `MongoConnectionStringBuilder` has been removed. Use the documented mongodb connection string format and/or `MongoUrlBuilder`.
- `MongoServer` is a deprecated class. Anyone using `MongoClient.GetServer()` will encounter a deprecation warning and, depending on how your build is setup, may receive an error. It is still safe to use this API until your code is ported to the new API. *Note that this API requires the use of the [mongocsharpdriver](http://nuget.org/packages/mongocsharpdriver) to include the legacy API.
- [CSHARP-1043](https://jira.mongodb.org/browse/CSHARP-1043) and [CSHARP-1044](https://jira.mongodb.org/browse/CSHARP-1044): `ReadPreference` and `WriteConcern` were rewritten. These classes are now immutable. Any current application code that sets values on these classes will no longer function. Instead, you should use the With method to alter a `ReadPreference` or `WriteConcern`.
	
	``` csharp
	var writeConcern = myCurrentWriteConcern.With(journal: true);
	```