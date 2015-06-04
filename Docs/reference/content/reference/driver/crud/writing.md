+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Writing"
[menu.main]
  parent = "Reference Reading and Writing"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## Insert

Use the [`InsertOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_InsertOneAsync" >}}) method to insert a document. For example, when working with an [`IMongoCollection<BsonDocument>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}})., a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) would be used.

```csharp
var doc = new BsonDocument("x", 1);
await collection.InsertOneAsync(doc);
```

Upon a successful insertion (no exception was thrown), `doc` will contain an `_id` field. This identifer will be automatically generated if an `_id` is not already present in the document. This is also true for [mapped classes]({{< relref "reference\bson\mapping\index.md" >}}) if they have an identifier property.

{{% note %}}For classes, it is possible to customize the Id generation process. See the reference on [mapping]({{< relref "reference\bson\mapping\index.md#id-generators" >}}).{{% /note %}}

To insert multiple documents at the same time, the [`InsertManyAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_InsertManyAsync" >}}) method exists.

```csharp
var docs = Enumerable.Range(0, 10).Select(i => new BsonDocument("x", i));
await collection.InsertManyAsync(docs);
```

As before, each of the documents will have their `_id` field set.

{{% note %}}MongoDB reserves fields that start with `_` and `$` for internal use.{{% /note %}}


## Update and Replace

There are 3 methods for updating documents. When intending to update an entire document, the [`ReplaceOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_ReplaceOneAsync" >}}) method should be used.

```csharp
var newDoc = new BsonDocument { { "_id", 10 }, { "x", 2 } };
var result = await collection.ReplaceOneAsync(
	filter: new BsonDocument("_id", 10),
	replacement: newDoc);
```

{{% note class="important" %}}`_id` values in MongoDB documents are immutable. If you specify an `_id` in the replacement document, it must match the `_id` of the existing document.{{% /note %}}

When using a [MongoDB update specification]({{< docsref "reference/operator/update/" >}}), the [`UpdateOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_UpdateOneAsync" >}}) and [`UpdateManyAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_UpdateManyAsync" >}}) methods are available.

```csharp
var filter = new BsonDocument();
var update = new BsonDocument("$set", new BsonDocument("x", 1));
var result = await collection.UpdateOneAsync(filter, update);
```

{{% note %}}Even if multiple documents match the filter (as would be true in the above example), only one will be updated because we used UpdateOneAsync.{{% /note %}}

To update all documents matching the filter:

```csharp
var result = await collection.UpdateManyAsync(filter, update);
```


### Upserts

To specify that you'd like to upsert a document, each method provides an optional [`UpdateOptions`]({{< apiref "T_MongoDB_Driver_UpdateOptions" >}}) parameter.

```csharp
var filter = new BsonDocument();
var update = new BsonDocument("$set", new BsonDocument("x", 1));
var options = new UpdateOptions { IsUpsert = true };
var result = await collection.UpdateManyAsync(filter, update, options);
```

{{% note %}}If an identifier is not specified in the filter, replacement, or update, the server will generate an `_id` of type ObjectId automatically.{{% /note %}}


## Delete

Deleting documents takes 2 forms, [`DeleteOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_DeleteOneAsync" >}}) and [`DeleteManyAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_DeleteManyAsync" >}}) which, respectively, delete one or many documents matching the provided `filter`.

```csharp
var filter = new BsonDocument();
var result = await collection.DeleteOneAsync(filter);

// and

var result = await collection.DeleteManyAsync(filter);
```

## Find And Modify

There are a certain class of operations using the [findAndModify command]({{< docsref "reference/command/findAndModify/" >}}) from the server. This command will perform some operation on the document and return either the original version of the document as it was before the operation, or the new version of the document as it became after the operation. By default, the original version of the document is returned.

### FindOneAndDelete

A single document can be deleted atomically using [`FindOneAndDeleteAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_FindOneAndDeleteAsync__1" >}}).

```csharp
var filter = new BsonDocument("FirstName", "Jack");
var result = await collection.FindOneAndDeleteAsync(filter);

if (result != null)
{
    Assert(result["FirstName"] == "Jack");
}
```

The above will find a document where the `FirstName` is `Jack` and delete it. It will then return the document that was just deleted.


### FindOneAndReplace

A single document can be replaced atomically using [`FindOneAndReplaceAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_FindOneAndReplaceAsync__1" >}}).

```csharp
var filter = new BsonDocument("FirstName", "Jack");
var replacementDocument = new BsonDocument("FirstName", "John");

var result = await collection.FindOneAndReplaceAsync(filter, doc);

if (result != null)
{
	Assert(result["FirstName"] == "Jack");
}
```

The above will find a document where the `FirstName` is `Jack` and replace it with `replacementDocument`. It will then return the document that was replaced.

### FindOneAndUpdate

A single document can be updated atomically using [`FindOneAndUpdateAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_FindOneAndUpdateAsync__1" >}}).

```csharp
var filter = new BsonDocument("FirstName", "Jack");
var update = Builders<BsonDocument>.Update.Set("FirstName", "John");

var result = await collection.FindOneAndUpdateAsync(filter, update);

if (result != null)
{
	Assert(result["FirstName"] == "Jack");
}
```

The above will find a document where the `FirstName` is `Jack` and set its `FirstName` field to `John`. It will then return the document that was replaced.


### Options

Each of the 3 operations has certain options available.

#### ReturnDocument

For Replacing and Updating, the [`ReturnDocument`]({{< apiref "T_MongoDB_Driver_ReturnDocument" >}}) enum can be provided. It allows you to choose which version of the document to return, either the original version as it was before the operation, or the modified version as it became after the operation.

For example:

```csharp
var filter = new BsonDocument("FirstName", "Jack");
var update = Builders<BsonDocument>.Update.Set("FirstName", "John");

var options = new FindOneAndUpdateOptions<BsonDocument>
{
    ReturnDocument = ReturnDocument.After
};
var result = await collection.FindOneAndUpdateAsync(filter, update, options);

if (result != null)
{
    Assert(result["FirstName"] == "John");
}
```

#### Projection

A projection can be provided to shape the result. The easiest way to build the projection is using a [projection builder]({{< relref "reference\driver\definitions.md#projections" >}}).

#### Sort

Since only a single document is selected, for filters that could result in multiple choices, a sort should be provided and the first document in the sort order will be the one modified.

#### IsUpsert

For Replacing and Updating, `IsUpsert` can be specified such that, if the document isn't found, one will be inserted.