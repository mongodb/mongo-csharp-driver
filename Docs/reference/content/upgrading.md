+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Upgrading"
[menu.main]
  parent = "What's New"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Upgrading

As 2.0 is a major revision, there are some breaking changes when coming from the 1.x assemblies. We've tried our best to mitigate those breaking changes, but some were inevitable. These changes may not affect everyone, but take a moment to review the list of known changes below:


## System Requirements

- .NET 3.5 and .NET 4.0 are no longer supported. If you still must use these platforms, the 1.x series of the driver will continue to be developed.
- [CSHARP-952](https://jira.mongodb.org/browse/CSHARP-952): We've removed support for partially trusted callers.


## Packaging

- The nuget package [mongocsharpdriver](http://nuget.org/packages/mongocsharpdriver) now includes the legacy driver. It depends on 3 new nuget packages, [MongoDB.Bson](http://nuget.org/packages/MongoDB.Bson), [MongoDB.Driver.Core](http://nuget.org/packages/MongoDB.Driver.Core), and [MongoDB.Driver](http://nuget.org/packages/MongoDB.Driver). [MongoDB.Driver](http://nuget.org/packages/MongoDB.Driver) is the replacement for [mongocsharpdriver](http://nuget.org/packages/mongocsharpdriver).
- [CSHARP-616](https://jira.mongodb.org/browse/CSHARP-616): We are no longer strong naming  our assemblies. Our previous strong naming was signed with a key in our public repository. This did nothing other than satisfy certain tools. If you need MongoDB assemblies to be strongly named, it is relatively straight-forward to build the assemblies yourself.


## BSON

- [CSHARP-933](https://jira.mongodb.org/browse/CSHARP-933): Improved the BSON Serializer infrastructure. Anyone who has written a custom serializer will be affected by this. The changes are minor, but were necessary to support dynamic serializers as well as offering great speed improvements and improved memory management.
- Certain methods, such as `BsonMemberMap.SetRepresentation` have been removed. The proper way to set a representation, for instance, would be to use `SetSerializer` and configure the serializer with the appropriate representation.
- [CSHARP-939](https://jira.mongodb.org/browse/CSHARP-939): Dynamic DictionaryRepresentation has been removed. Its intent was to store, in some manner, anything in a .NET dictionary. In practice, this leads to the same values getting stored in different ways depending on factors such as a "." inside the key name. We made the decision to eliminate this variability. This means that documents that used to serialize correctly may start throwing a BsonSerializationException with a message indicating the key must be a valid string. [CSHARP-1165](https://jira.mongodb.org/browse/CSHARP-1165) has a solution to this problem. It should be noted that we will continue to read these disparate representations without error.


## Driver
- [CSHARP-979](https://jira.mongodb.org/browse/CSHARP-979): `MongoConnectionStringBuilder` has been removed. Use the documented mongodb connection string format and/or `MongoUrlBuilder`.
- `MongoServer` is a deprecated class. Anyone using `MongoClient.GetServer()` will encounter a deprecation warning and, depending on how your build is setup, may receive an error. It is still safe to use this API until your code is ported to the new API. *Note that this API requires the use of the [mongocsharpdriver](http://nuget.org/packages/mongocsharpdriver) to include the legacy API.
- [CSHARP-1043](https://jira.mongodb.org/browse/CSHARP-1043) and [CSHARP-1044](https://jira.mongodb.org/browse/CSHARP-1044): `ReadPreference` and `WriteConcern` were rewritten. These classes are now immutable. Any current application code that sets values on these classes will no longer function. Instead, you should use the With method to alter a `ReadPreference` or `WriteConcern`.
	
	``` csharp
	var writeConcern = myCurrentWriteConcern.With(journal: true);
	```

