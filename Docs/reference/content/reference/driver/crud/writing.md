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


## Bulk Writes

The [`BulkWriteAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_BulkWriteAsync" >}}) method takes a variable number of [`WriteModel<>`]({{< apiref "T_MongoDB_Driver_WriteModel_1" >}}) instances and sends them to the server in the fewest possible number of batches. The size of a batch is limited by the maximum document size and each batch must consist of the same kind of write operations (i.e. deletes, inserts or updates).

For example, to run two delete operations with one call to the server:

```csharp
var models = new WriteModel<BsonDocument>[] 
{
  new DeleteManyModel<BsonDocument>("{ x: 10 }"), // delete all documents where x == 10
  new DeleteOneModel<BsonDocument>("{ x: 11 }") // delete 1 document where x == 11
};

await collection.BulkWriteAsync(models);
```

However, providing one insert and one delete would result in each getting sent in a different call to the server:

```csharp
var models = new WriteModel<BsonDocument>[] 
{
  new InsertOneModel<BsonDocument>("{ _id: 1}"),
  new DeleteOneModel<BsonDocument>("{ x: 11 }") // delete 1 document where x == 11
};

await collection.BulkWriteAsync(models); // will send one batch with the insert and one with the delete
```


### Write Models

There are 6 types of write models:

1. [`InsertOneModel`]({{< apiref "T_MongoDB_Driver_InsertOneModel_1" >}})
1. [`DeleteOneModel`]({{< apiref "T_MongoDB_Driver_DeleteOneModel_1" >}})
1. [`DeleteManyModel`]({{< apiref "T_MongoDB_Driver_DeleteManyModel_1" >}})
1. [`UpdateOneModel`]({{< apiref "T_MongoDB_Driver_UpdateOneModel_1" >}}) 
1. [`UpdateManyModelModel`]({{< apiref "T_MongoDB_Driver_UpdateManyModel_1" >}})
1. [`ReplaceOneModel`]({{< apiref "T_MongoDB_Driver_ReplaceOneModel_1" >}})


### Ordered and Unordered

Bulk writes can be ordered or unordered. The default is ordered.

1. Ordered bulk writes execute all the operations in order and error out on the first error. 
1. Unordered bulk writes execute all the operations and report any errors at the end. Because the writes are unordered, the driver and/or server may re-order the operations in order to gain better performance.

```csharp
var models = new WriteModel<BsonDocument>[] 
{
  new InsertOneModel<BsonDocument>(new BsonDocument("_id", 4)),
  new InsertOneModel<BsonDocument>(new BsonDocument("_id", 5)),
  new InsertOneModel<BsonDocument>(new BsonDocument("_id", 6)),
  new UpdateOneModel<BsonDocument>(
    new BsonDocument("_id", 1), 
    new BsonDocument("$set", new BsonDocument("x", 2))),
  new DeleteOneModel<BsonDocument>(new BsonDocument("_id", 3)),
  new ReplaceOneModel<BsonDocument>(
    new BsonDocument("_id", 3), 
    new BsonDocument("_id", 3).Add("x", 4))
};

// 1. Ordered bulk operation - order of operation is guaranteed
await collection.BulkWriteAsync(models);

// 2. Unordered bulk operation - no guarantee of order of operation
await collection.BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = false });
```

{{% note class="important" %}}Use of the bulk write methods is not recommended when connected to pre-2.6 MongoDB servers, as this was the first server version to support bulk write commands for insert, update, and delete in a way that allows the driver to implement the correct semantics for BulkWriteResult and BulkWriteException. The methods will still work for pre-2.6 servers, but performance will suffer, as each write operation has to be executed one at a time.{{% /note %}}