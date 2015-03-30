+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Reading"
[menu.main]
  parent = "CRUD Operations"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Counting Documents

The [`CountAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_CountAsync" >}}) method can be used to count all the documents matching a particular filter.

```csharp
var count = await collection.CountAsync(new BsonDocument("x", 10));

// or

var count = await collection.CountAsync(x => x.Age > 20);
```

Counting all the documents in a collection requires an empty filter:

```csharp
var count = await collection.CountAsync(new BsonDocument());
```


## Finding Documents

Finding all the documents in a collection is done with an empty filter and the method [`Find`]({{< apiref "M_MongoDB_Driver_IMongoCollectionExtensions_Find__1" >}}). Once we have a cursor (of type [`IAsyncCursor<TDocument>`]({{< apiref "T_MongoDB_Driver_IAsyncCursor_1" >}})), we can iterate it like we manually iterate an [`IEnumerable<TDocument>`]({{< msdnref "9eekhta0" >}}).

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
using (var cursor = await collection.Find(filter, options).Skip(10).Limit(20).ToCursorAsync())
{
	// etc...
}
```

{{% note %}}The order of the chained methods is unimportant. Limit followed by Skip is the same as Skip followed by Limit. In addition, specifying a method multiple times will result in the last one winning.{{% /note %}}


### Iteration

Other methods of iteration besides using a cursor directly are available.

First, `ToListAsync` is available. This is useful when the list will be small or you simply need them all as a list. If you are returning a large number of documents, then memory should be considered a factor.

```csharp
var list = await collection.Find(filter)
	.Skip(10)
	.ToListAsync();
```

Second, `ForEachAsync` is available. This method is useful when you just need to process each document and don't need to keep them around.

```csharp
await collection.Find(filter)
	.Skip(10)
	.ForEachAsync(doc => Console.WriteLine(doc));
```

To avoid blocking while processing each document you can use an async lambda:

```csharp
await collection.Find(filter)
	.Skip(10)
	.ForEachAsync(async (doc) => await Console.WriteLineAsync(doc));
```

{{% note %}}These iteration methods don't require you to dispose of the cursor. That will be handled for you automatically.{{% /note %}}

### Single Results

When you only want to find one document, the `FirstAsync`, `FirstOrDefaultAsync`, `SingleAsync`, and `SingleOrDefaultAsync` methods are available.

```csharp
var result = await collection.Find(filter)
	.Skip(10)
	.FirstOrDefaultAsync();
```