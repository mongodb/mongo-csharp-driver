+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Upgrading"
[menu.main]
  parent = "What's New"
  identifier = "Upgrading"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Breaking Changes

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
- `null` is no longer ignored in BsonValue-derived constructors. Anyone relying on null getting ignored will now receive an `ArgumentNullException`.


### Driver
- [CSHARP-979](https://jira.mongodb.org/browse/CSHARP-979): `MongoConnectionStringBuilder` has been removed. Use the documented mongodb connection string format and/or `MongoUrlBuilder`.
- `MongoServer` is a deprecated class. Anyone using `MongoClient.GetServer()` will encounter a deprecation warning and, depending on how your build is setup, may receive an error. It is still safe to use this API until your code is ported to the new API. *Note that this API requires the use of the [mongocsharpdriver](http://nuget.org/packages/mongocsharpdriver) to include the legacy API.
- [CSHARP-1043](https://jira.mongodb.org/browse/CSHARP-1043) and [CSHARP-1044](https://jira.mongodb.org/browse/CSHARP-1044): `ReadPreference` and `WriteConcern` were rewritten. These classes are now immutable. Any current application code that sets values on these classes will no longer function. Instead, you should use the With method to alter a `ReadPreference` or `WriteConcern`.
    
    ``` csharp
    var writeConcern = myCurrentWriteConcern.With(journal: true);
    ```

## Migrating

Below are some common actions in the old API and their counterpart in the new API.

### Builders

_[More information.]({{< relref "reference\driver\definitions.md" >}})_

The old builders (`Query`, `Update`, etc...) have all been replaced by `Builders<T>.Filter`, `Builders<T>.Update`, etc...

```csharp
// old API
var query = Query.And(
    Query<Person>.EQ(x => x.FirstName, "Jack"), 
    Query<Person>.LT(x => x.Age, 21));

// new API
var builder = Builders<Person>.Filter;
var filter = builder.Eq(x => x.FirstName, "Jack") & builder.Lt(x => x.Age, 21);
```

```csharp
// old API
var update = Update.Combine(
    Update<Person>.Set(x => x.LastName, "McJack"),
    Update<Person>.Inc(x => x.Age, 1));

// new API
var update = Builders<Person>.Update
    .Set(x => x.LastName, "McJack")
    .Inc(x => x.Age, 1);
```

### Finding All Documents

_[More information.]({{< relref "reference\driver\crud\reading.md#finding-documents" >}})_

To match all documents, you must specify an empty filter.

```csharp
// old API
var list = collection.FindAll().ToList();

// new API
var list = await collection.Find(new BsonDocument()).ToListAsync();
```

### Finding One Document

_[More information.]({{< relref "reference\driver\crud\reading.md#single-results" >}})_

To match all documents, you must specify an empty filter.

```csharp
// old API
var document = collection.FindOne();

// new API
var document = await collection.Find(new BsonDocument()).FirstOrDefaultAsync();
```

### Iteration

_[More information.]({{< relref "reference\driver\crud\reading.md#iteration" >}})_

You cannot iterate synchronously using a foreach loop without first getting a list. However, there are alternative methods provided to loop over all the documents in an asynchronous manner.

```csharp
// old API
foreach (var document in collection.Find(new QueryDocument("Name", "Jack")))
{
    // do something
}

// new API
await collection.Find(new BsonDocument("Name", "Jack"))
    .ForEachAsync(document =>
    {
        // do something
    });

await collection.Find(new BsonDocument("Name", "Jack"))
    .ForEachAsync(async document =>
    {
        // do something with await
    });
```

### Counting All Documents

_[More information.]({{< relref "reference\driver\crud\reading.md#counting-documents" >}})_

To match all documents, you must specify an empty filter.

```csharp
// old API
var count = collection.Count();

// new API
var count = await collection.CountAsync(new BsonDocument());
```

### Mapping - SetRepresentation

_[More information.]({{< relref "reference\bson\mapping\index.md#representation" >}})_

You can still use attributes as you have done before. To set the representation via code:

```csharp
class Test
{
    public char RepresentAsInt32 { get; set; }
}

// old API
BsonClassMap.RegisterClassMap<Person>(cm =>
{
    // snip...
    cm.MapMember(x => x.RepresentAsInt32).SetRepresentation(BsonType.Int32);
});

// new API
BsonClassMap.RegisterClassMap<Person>(cm =>
{
    // snip...
    cm.MapMember(x => x.RepresentAsInt32).SetSerializer(new CharSerializer(BsonType.Int32));
});
```