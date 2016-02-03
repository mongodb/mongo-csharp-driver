+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Reading"
[menu.main]
  parent = "Reference Reading and Writing"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Counting Documents

The [`Count`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_Count" >}}) and [`CountAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_CountAsync" >}}) methods can be used to count all the documents matching a particular filter.

```csharp
var count = collection.Count(new BsonDocument("x", 10));

// or

var count = collection.Count(x => x.Age > 20);
```
```csharp
var count = await collection.CountAsync(new BsonDocument("x", 10));

// or

var count = await collection.CountAsync(x => x.Age > 20);
```

Counting all the documents in a collection requires an empty filter:

```csharp
var count = collection.Count(new BsonDocument());
```
```csharp
var count = await collection.CountAsync(new BsonDocument());
```


## Finding Documents

Finding all the documents in a collection is done with an empty filter and the method [`Find`]({{< apiref "M_MongoDB_Driver_IMongoCollectionExtensions_Find__1" >}}). Once we have a cursor (of type [`IAsyncCursor<TDocument>`]({{< apiref "T_MongoDB_Driver_IAsyncCursor_1" >}})), we can iterate it like we manually iterate an [`IEnumerable<TDocument>`]({{< msdnref "9eekhta0" >}}).

```csharp
var filter = new BsonDocument();
using (var cursor = collection.Find(filter).ToCursor())
{
	while (cursor.MoveNext())
	{
		foreach (var doc in cursor.Current)
		{
			// do something with the documents
		}
	}
}
```
```csharp
var filter = new BsonDocument();
using (var cursor = await collection.Find(filter).ToCursorAsync())
{
	while (await cursor.MoveNextAsync())
	{
		foreach (var doc in cursor.Current)
		{
			// do something with the documents
		}
	}
}
```

{{% note %}}It is imperative that the cursor get disposed once you are finished with it to ensure that resources on the server are cleaned up.{{% /note %}}

Some options are available in the optional [`FindOptions`]({{< apiref "T_MongoDB_Driver_FindOptions" >}}) parameter such as setting maxTimeMS, a batch size, or a comment. Others are available as part of the fluent interface such as skip, limit, and sort.

```csharp
var filter = new BsonDocument();
var options = new FindOptions
{
	MaxTime = TimeSpan.FromMilliseconds(20)
};
```
```csharp
using (var cursor = collection.Find(filter, options).Skip(10).Limit(20).ToCursor())
{
	// etc...
}
```
```csharp
using (var cursor = await collection.Find(filter, options).Skip(10).Limit(20).ToCursorAsync())
{
	// etc...
}
```

{{% note %}}The order of the chained methods is unimportant. Limit followed by Skip is the same as Skip followed by Limit. In addition, specifying a method multiple times will result in the last one winning.{{% /note %}}


### Iteration

Other methods of iteration besides using a cursor directly are available.

First, the [`ToList`]({{< apiref "M_MongoDB_Driver_IAsyncCursorSourceExtensions_ToList__1" >}}) and [`ToListAsync`]({{< apiref "M_MongoDB_Driver_IAsyncCursorSourceExtensions_ToListAsync__1" >}}) methods are available. These methods are useful when the list will be small or you simply need them all as a list. If you are returning a large number of documents, then memory should be considered a factor.

```csharp
var list = collection.Find(filter)
	.Skip(10)
	.ToList();
```

```csharp
var list = await collection.Find(filter)
	.Skip(10)
	.ToListAsync();
```

Second, [`ForEachAsync`]({{< apiref "Overload_MongoDB_Driver_IAsyncCursorSourceExtensions_ForEachAsync" >}}) is available. This method is useful when you just need to process each document and don't need to keep them around.

```csharp
await collection.Find(filter)
	.Skip(10)
	.ForEachAsync(doc => Console.WriteLine(doc));
```

To avoid blocking while processing each document you can use an async lambda with `ForEachAsync`:

```csharp
await collection.Find(filter)
	.Skip(10)
	.ForEachAsync(async (doc) => await Console.WriteLineAsync(doc));
```

When using the synchronous API you can use the C# foreach statement to iterate over the documents in a cursor.

```csharp
var cursor = collection.Find(filter)
	.Skip(10)
	.ToCursor();
foreach (var doc in cursor.ToEnumerable())
{
	Console.WriteLine(doc);	
}
```

{{% note %}}These iteration methods don't require you to dispose of the cursor. That will be handled for you automatically.{{% /note %}}

### Single Results

When you only want to find one document, the [`First`]({{< apiref "M_MongoDB_Driver_IAsyncCursorSourceExtensions_First__1" >}}), [`FirstOrDefault`]({{< apiref "M_MongoDB_Driver_IAsyncCursorSourceExtensions_FirstOrDefault__1" >}}), [`Single`]({{< apiref "M_MongoDB_Driver_IAsyncCursorSourceExtensions_Single__1" >}}), [`SingleOrDefault`]({{< apiref "M_MongoDB_Driver_IAsyncCursorSourceExtensions_SingleOrDefault__1" >}}) methods, and their asynchronous counterparts [`FirstAsync`]({{< apiref "M_MongoDB_Driver_IAsyncCursorSourceExtensions_FirstAsync__1" >}}), [`FirstOrDefaultAsync`]({{< apiref "M_MongoDB_Driver_IAsyncCursorSourceExtensions_FirstOrDefaultAsync__1" >}}), [`SingleAsync`]({{< apiref "M_MongoDB_Driver_IAsyncCursorSourceExtensions_SingleAsync__1" >}}), and [`SingleOrDefaultAsync`]({{< apiref "M_MongoDB_Driver_IAsyncCursorSourceExtensions_SingleOrDefaultAsync__1s" >}}) are available.

```csharp
var result = collection.Find(filter)
	.Skip(10)
	.FirstOrDefault();
```
```csharp
var result = await collection.Find(filter)
	.Skip(10)
	.FirstOrDefaultAsync();
```

## Aggregation

MongoDB offers the [aggregation framework]({{< docsref "core/aggregation-pipeline/" >}}) which can be accessed via the [`Aggregate`]({{< apiref "M_MongoDB_Driver_IMongoCollectionExtensions_Aggregate__1" >}}) method. The result type is [`IAggregateFluent`]({{< apiref "T_MongoDB_Driver_IAggregateFluent_1" >}}) and provides access to a fluent API to build up an aggregation pipeline.

The first example from [MongoDB's documentation]({{< docsref "tutorial/aggregation-zip-code-data-set/#return-states-with-populations-above-10-million" >}}) is done in a type-safe manner below:

```csharp
[BsonIgnoreExtraElements]
class ZipEntry
{
    [BsonId]
    public string Zip { get; set; }

    [BsonElement("city")]
    public string City { get; set; }

    [BsonElement("state")]
    public string State { get; set; }

    [BsonElement("pop")]
    public int Population { get; set; }
}
```
```csharp
var results = db.GetCollection<ZipEntry>.Aggregate()
	.Group(x => x.State, g => new { State = g.Key, TotalPopulation = g.Sum(x => x.Population) })
	.Match(x => x.TotalPopulation > 20000)
	.ToList();
```
```csharp
var results = await db.GetCollection<ZipEntry>.Aggregate()
	.Group(x => x.State, g => new { State = g.Key, TotalPopulation = g.Sum(x => x.Population) })
	.Match(x => x.TotalPopulation > 20000)
	.ToListAsync();
```

This will result in the following aggregation pipeline getting sent to the server:

```json
[{ group: { _id: '$state', TotalPopulation: { $sum : '$pop' } } },
{ $match: { TotalPopulation: { $gt: 20000 } } }]
```

{{% note %}}You can call `ToString` on the pipeline to see what would be sent to the server.{{% /note %}}

More samples are located in the [source]({{< srcref "MongoDB.Driver.Tests/Samples/AggregationSample.cs" >}}).

### Stage Operators

All the [stage operators]({{< docsref "reference/operator/aggregation/#aggregation-pipeline-operator-reference" >}}) are supported, however some of them must use the [`AppendStage`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_AppendStage__1" >}}) method due to lack of support for certain projections in the language.

{{% note class="important" %}}Unlike `Find`, the order that stages are defined in matters. `Skip(10).Limit(20)` is not the same as `Limit(20).Skip(10)`.{{% /note %}}

#### $project

A `$project` is defined using the [`Project`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_Project__1" >}}) method and its overloads. Unlike in `Find`, an aggregate projection is not executed client-side and must be fully translatable to the server's supported expressions.  See [expressions]({{< relref "reference\driver\expressions.md#projections" >}}) for more detail about the expressions available inside a $project.

```csharp
Project(x => new { Name = x.FirstName + " " + x.LastName });
```
```json
{ $project: { Name: { $concat: ['$FirstName', ' ', '$LastName'] } } }
```

{{% note %}}In an aggregation framework projection, a new type, either anonymous or named, must be used.{{% /note %}}

#### $match

A `$match` stage is defined using the [`Match`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_Match" >}}) method and its overloads. It follows the same requirements as that of `Find`.

```csharp
Match(x => x.Age > 21);
```
```json
{ $match: { Age: { $gt: 21 } } }
```

#### $redact

There is no method defined for a `$redact` stage. However, it can be added using [`AppendStage`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_AppendStage__1" >}}).

#### $limit

A `$limit` stage is defined using the [`Limit`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_Limit" >}}) method.

```csharp
Limit(20);
```
```json
{ $limit: 20 }
```

#### $skip

A `$skip` stage is defined using the [`Skip`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_Skip" >}}) method.

```csharp
Skip(20);
```
```json
{ $skip: 20 }
```

#### $unwind

An `$unwind` stage is defined using the [`Unwind`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_Unwind__1" >}}) method and its overloads. Because $unwind is a type of projection, you must provide a return type, although not specifying one will use the overload that projects into a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}).

```csharp
Unwind(x => x.ArrayFieldToUnwind);
```
```json
{ $unwind: 'ArrayFieldToUnwind' }
```

#### $group

A `$group` stage is defined using the [`Group`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_Group__1" >}}) method and its overloads. Because $unwind is a type of projection, you must provide a return type. The most useful of the overloads is where two lambda expressions are expressed, the first for the key and the second for the grouping. See [expressions]({{< relref "reference\driver\expressions.md#grouping" >}}) for more detail about the expressions available inside a $group.

```csharp
Group(x => x.Name, g => new { Name = g.Key, AverageAge = g.Average(x = x.Age) });
```
```json
{ $group: { _id: '$Name', AverageAge: { $avg: '$Age'} } }
```

As in project, it is required that the result of the grouping be a new type, either anonymous or named. If the `Key` of the grouping is not used, an `_id` will still be inserted as this is required by the `$group` operator.

#### $sort

A `$sort` stage is defined using the [`Sort`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_Sort" >}}) method. However, `SortBy`, `SortByDescending`, `ThenBy`, and `ThenByDescending` are also available. 

```csharp
SortBy(x => x.LastName).ThenByDescending(x => x.Age);
```
```json
{ $sort: { LastName: 1, Age: -1 } }
```

#### $geoNear

There is no method defined for a `$geoNear` stage. However, it can be added using [`AppendStage`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_AppendStage__1" >}}).

#### $out

A `$out` stage is defined using the [`Out`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_Out" >}}) or [`OutAsync`]({{< apiref "M_MongoDB_Driver_IAggregateFluent_1_OutAsync" >}}) methods. It must be the final stage and it triggers execution of the operation.

```csharp
Out("myNewCollection");
```
```csharp
await OutAsync("myNewCollection");
```
```json
{ $out: 'myNewCollection' }
```