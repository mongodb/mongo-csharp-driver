+++
date = "2018-07-02T20:36:00Z"
draft = false
title = "Change Streams"
[menu.main]
  parent = "Driver"
  identifier = "Change Streams"
  weight = 55
  pre = "<i class='fa'></i>"
+++

## Change Streams

Change streams allow an application to receive a stream of events representing changes to documents in:

1. A single collection
2. All collections in a single database
3. All collections in all databases

An application starts watching a change stream by calling one of the Watch methods.

### Watching changes in a single collection

To watch changes in a single collection call the Watch or WatchAsync method of IMongoCollection.

```csharp
using (var cursor = collection.Watch())
{
    foreach (var change in cursor.ToEnumerable())
    {
        // process change event
    }
}
```

or

```csharp
using (var cursor = await collection.WatchAsync())
{
    await cursor.ForEachAsync(change =>
    {
        // process change event
    });
}
```

### Watching changes in all collections in a single database

To watch changes in all collections in a single database call the Watch or WatchAsync method of IMongoDatabase.

```csharp
using (var cursor = database.Watch())
{
    foreach (var change in cursor.ToEnumerable())
    {
        // process change event
    }
}
```

or

```csharp
using (var cursor = await database.WatchAsync())
{
    await cursor.ForEachAsync(change =>
    {
        // process change event
    });
}
```

### Watching changes in all collections in all databases

To watch changes in all collections in all databases call the Watch or WatchAsync method of IMongoClient.

```csharp
using (var cursor = client.Watch())
{
    foreach (var change in cursor.ToEnumerable())
    {
        // process change event
    }
}
```

or

```csharp
using (var cursor = await client.WatchAsync())
{
    await cursor.ForEachAsync(change =>
    {
        // process change event
    });
}
```

### Strongly typed ChangeStreamDocument&lt;TDocument&gt; class

The cursor returned from the Watch methods returns the change stream events wrapped in a strongly typed C# class called ChangeStreamDocument&lt;TDocument&gt; (unless you used a pipeline that changed the shape of the results). The underlying change stream events are documented in the server documentation [here](https://docs.mongodb.com/manual/reference/change-events/).

```csharp
public class ChangeStreamDocument<TDocument>
{
    public BsonDocument ClusterTime { get; }
    public CollectionNamespace CollectionNamespace { get; }
    public BsonDocument DocumentKey { get; }
    public TDocument FullDocument { get; }
    public ChangeStreamOperationType OperationType { get; }
    public BsonDocument ResumeToken { get; }
    public ChangeStreamUpdateDescription UpdateDescription { get; }
}
```

ClusterTime is the timestamp from the oplog entry associated with the event.

CollectionNamespace is the full namespace of the collection containing the changed document.

DocumentKey contains the _id of the document created or modified by the operation. For sharded collections it also contains the shard key of the document.

For Insert and Replace operations,  FullDocument is the new document created by the operation. For Delete operations FullDocument is null as the document no longer exists. For Update operations FullDocument is only present if you set the FullDocument option in the options passed to Watch to ChangeStreamFullDocumentOption.UpdateLookup, in which case it contains the most current majority-committed version of the document modified by the Update operation (see the server documentation for fullDocument [here](https://docs.mongodb.com/manual/reference/change-events/) for details).

OperationType is one of: Insert, Update, Replace, Delete or Invalidate.

ResumeToken is metadata identifying the change stream event. It can be passed to Watch in the ResumeAfter option to start a new change stream that will resume with the next change event after this one. This is useful when your application needs to restart a change stream after a network outage.

UpdateDescription is only present when the OperationType is Update. It describes the fields that were updated or removed by the Update operation.

### Using an optional pipeline

All the Watch and WatchAsync methods have overloads that take a pipeline argument. An application can pass in a pipeline to filter or modify the change stream in some way.

Only certain modifications are valid in a change stream pipeline. See the server documentation [here](https://docs.mongodb.com/manual/changeStreams/#modify-change-stream-output) for details.

For example, if you are only interested in monitoring inserted documents, you could use a pipeline to filter the change stream to only include insert operations.

```csharp
var pipeline = 
    new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
    .Match(x => x.OperationType == ChangeStreamOperationType.Insert);
using (var cursor = collection.Watch(pipeline))
{
    foreach (var change in cursor.ToEnumerable())
    {
        // process insert change event
    }
}
```

{{% note %}}
The implementation of Watch depends on the presence of a "resume token" in the change stream. The resume token is the value of the "_id" field in the change stream documents (represented as the ResumeToken property of the ChangeStreamDocument class). If you use a pipeline and the pipeline changes the shape of the change stream documents, the pipeline MUST preserve the presence of the "_id" field, and it MUST NOT change its value.
{{% /note %}}

### Passing options to the Watch and WatchAsync methods

You can pass options to the Watch and WatchAsync methods in the optional options argument, which is of type ChangeStreamOptions.

```csharp
public class ChangeStreamOptions
{
    public int? BatchSize { get; set; }
    public Collation Collation { get; set; }
    public ChangeStreamFullDocumentOption FullDocument { get; set; }
    public TimeSpan? MaxAwaitTime { get; set; }
    public BsonDocument ResumeAfter { get; set; }
    public BsonTimestamp StartAtOperationTime { get; set; }
}
```

BatchSize determines the maximum number of change events the server will return at one time. The server might return fewer.

FullDocument can be set to ChangeStreamFullDocumentOption.UpdateLookup if you want the change stream event for Update operations to include a copy of the full document (the full document might include additional changes that are the result of subsequent change events, see the server documentation [here](https://docs.mongodb.com/manual/reference/change-events/#update-event)).

ResumeAfter and StartAtOperationTime are useful when you want to resume or start a change stream from some point in time.

For example, if you want to monitor only updates, and you want each change event to include the full document, you would write:

```csharp
var pipeline = 
    new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
    .Match(x => x.OperationType == ChangeStreamOperationType.Update);

var changeStreamOptions = new ChangeStreamOptions
{
    FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
};

using (var cursor = collection.Watch(pipeline, changeStreamOptions))
{
    foreach (var change in cursor.ToEnumerable())
    {
        // process updated document in change.FullDocument
    }
}
```
