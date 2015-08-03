+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Quick Tour"
[menu.main]
  parent = "Getting Started"
  weight = 20
  identifier = "Quick Tour"
  pre = "<i class='fa'></i>"
+++

## MongoDB Driver Quick Tour

This is the first part of the MongoDB driver quick tour. In this part, we will look at how to perform basic CRUD (create, read, update, delete) operations. In the [next part]({{< relref "getting_started\admin_quick_tour.md" >}}), we'll look at performing some adminstrative functions.

{{% note %}}See the [installation guide]({{< relref "getting_started\installation.md" >}}) for instructions on how to install the MongoDB Driver.{{% /note %}}

## Make a connection

The following example shows three ways to connect to a server or servers on the local machine.

```csharp
// To directly connect to a single MongoDB server
// (this will not auto-discover the primary even if it's a member of a replica set)
var client = new MongoClient();

// or use a connection string
var client = new MongoClient("mongodb://localhost:27017");

// or, to connect to a replica set, with auto-discovery of the primary, supply a seed list of members
var client = new MongoClient("mongodb://localhost:27017,localhost:27018,localhost:27019");
```

The `client` instance now holds a pool of connections to the server or servers specified in the connection string.


## MongoClient

The [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) instance actually represents a pool of connections to the database; you will only need one instance of class MongoClient even with multiple threads.

{{% note class="important" %}}Typically you only create one [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) instance for a given cluster and use it across your application. Creating multiple [`MongoClients`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) will, however, still share the same pool of connections if and only if the connection strings are identical.{{% /note %}}


## Get a Database

To get a database, specify the name of the database to the [`GetDatabase`]({{< apiref "M_MongoDB_Driver_MongoClient_GetDatabase" >}}) method on `client`. It's ok if the database doesn't yet exist. It will be created upon first use.

```csharp
var database = client.GetDatabase("foo");
```

The `database` variable now holds a reference to the "foo" database.


## Get a Collection

To get a collection to operate upon, specify the name of the collection to the [`GetCollection<TDocument>`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_GetCollection__1" >}}) method on `database`. It's ok if the collection doesn't yet exist. It will be created upon first use.

```csharp
var collection = database.GetCollection<BsonDocument>("bar");
```

The `collection` variable now holds a reference to the "bar" collection in the "foo" database.

{{% note %}}The generic parameter `TDocument` represents the schema that exists in your collection. Above, we've used a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) to indicate that we have no pre-defined schema. It is possible to use your plain-old-C#-objects (POCOs) as well. See the [mapping documentation]({{< relref "reference\bson\mapping\index.md" >}}) for more information.{{% /note %}}


## Insert a Document

Once you have the `collection` instance, you can insert documents into the collection. For example, consider the following JSON document; the document contains a field info which is an embedded document:

```json
{
     "name": "MongoDB",
     "type": "database",
     "count": 1,
     "info": {
   	     x: 203,
         y: 102
     }
}
```

To create the document using the .NET driver, use the [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) class. You can use this class to create the embedded document as well.

```csharp
var document = new BsonDocument
{
    { "name", "MongoDB" },
    { "type", "Database" },
    { "count", 1 },
    { "info", new BsonDocument
              {
                  { "x", 203 },
                  { "y", 102 }
              }}
};
```

To insert the document into the collection, use the [`InsertOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_InsertOneAsync" >}}) method.

```csharp
await collection.InsertOneAsync(doc);
```

{{% note %}}The .NET driver is fully async. For more information on async and await, please see the [MSDN documentation](https://msdn.microsoft.com/en-us/library/hh191443.aspx).{{% /note %}}


## Insert Multiple Documents

To insert multiple documents, you can use the [`InsertManyAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_InsertManyAsync" >}}) method.

```csharp
// generate 100 documents with a counter ranging from 0 - 99
var documents = Enumerable.Range(0, 100).Select(i => new BsonDocument("counter", i));

await collection.InsertManyAsync(documents);
```


## Counting Documents

Now that we’ve inserted 101 documents (the 100 we did in the loop, plus the first one), we can check to see if we have them all using the [`CountAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_CountAsync" >}}) method. The following code should print 101.

```csharp
var count = await collection.CountAsync(new BsonDocument());

Console.WriteLine(count);
```

{{% note %}}The empty [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) parameter to the [`CountAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_CountAsync" >}}) method is a filter. In this case, it is an empty filter indicating to count all the documents.{{% /note %}}


## Query the Collection

Use the [`Find`]({{< apiref "Overload_MongoDB_Driver_IMongoCollectionExtensions_Find" >}}) method to query the collection. The [`Find`]({{< apiref "Overload_MongoDB_Driver_IMongoCollectionExtensions_Find" >}}) method returns an [`IFindFluent<TDocument, TProjection>`]({{< apiref "T_MongoDB_Driver_IFindFluent_2" >}}) instance that provides a fluent interface for chaining or controlling find operations.

### Find the First Document in a Collection

To get the first document in the collection, call the [`FirstOrDefaultAsync`]({{< apiref "M_MongoDB_Driver_IFindFluentExtensions_FirstOrDefaultAsync__2" >}}) method. `await collection.Find(new BsonDocument()).FirstOrDefaultAsync()` returns the first document or null. This is useful for queries that should only match a single document, or if you are interested in the first document only.

The following example prints the first document found in the collection.

```csharp
var document = await collection.Find(new BsonDocument()).FirstOrDefaultAsync();
Console.WriteLine(document.ToString());
```

The example should print the following document:

```json
{ 
	"_id": ObjectId("551582c558c7b4fbacf16735") },
  	"name": "MongoDB", 
  	"type": "database", 
  	"count": 1,
  	"info": { "x" : 203, "y" : 102 } 
}
```

{{% note %}}The "_id" element has been added automatically by MongoDB to your document and your value will differ from that shown. MongoDB reserves field names that start with "_" and "$" for internal use.{{% /note %}}

### Find All Documents in a Collection

To retrieve all the documents in the collection, call the [`ToListAsync`]({{< apiref "M_MongoDB_Driver_IAsyncCursorSourceExtensions_ToListAsync__1" >}}) method. This is useful when the number of documents expected to be returned is small.

```csharp
var documents = await collection.Find(new BsonDocument()).ToListAsync();
```

If the number of documents is expected to be large or they can be processed iteratively, the [`ForEachAsync`]({{< apiref "Overload_MongoDB_Driver_IAsyncCursorSourceExtensions_ForEachAsync" >}}) will invoke a callback for each document returned.

```csharp
await collection.Find(new BsonDocument()).ForEachAsync(d => Console.WriteLine(d));
```

Each of the above examples will print the exact same thing to the console. For more information on iteration, see the [reference documention]({{< relref "reference\driver\crud\reading.md#finding-documents" >}}).


## Get a Single Document with a Filter

We can create a filter to pass to the [`Find`]({{< apiref "Overload_MongoDB_Driver_IMongoCollectionExtensions_Find" >}}) method to get a subset of the documents in our collection. For example, if we wanted to find the document for which the value of the “i” field is 71, we would do the following:

```csharp
var filter = Builders<BsonDocument>.Filter.Eq("i", 71);

var document = await collection.Find(filter).FirstAsync();
Console.WriteLine(document);
```

and it should print just one document:

```json
{ "_id" : ObjectId("5515836e58c7b4fbc756320b"), "i" : 71 }
```

{{% note %}}Use the [Filter]({{< relref "reference\driver\definitions.md#filters" >}}), [Sort]({{< relref "reference\driver\definitions.md#sorts" >}}), and [Projection]({{< relref "reference\driver\definitions.md#projections" >}}) builders for simple and concise ways of building up queries.{{% /note %}} 

## Get a Set of Documents with a Filter

We can also get a set of documents from our collection. For example, if we wanted to get all documents where `i > 50`, we could write:

```csharp
var filter = Builders<BsonDocument>.Filter.Gt("i", 50);

await collection.Find(filter).ForEachAsync(d => Console.WriteLine(d));
```

We could also get a range, say `50 < i <= 100`:

```csharp
var filterBuilder = Builders<BsonDocument>.Filter;
var filter = filterBuilder.Gt("i", 50) & filterBuilder.Lte("i", 100);

await collection.Find(filter).ForEachAsync(d => Console.WriteLine(d));
```

## Sorting Documents

We add a sort to a find query by calling the [`Sort`]({{< apiref "M_MongoDB_Driver_IFindFluent_2_Sort" >}}) method. Below we use the [`Exists`]({{< apiref "Overload_MongoDB_Driver_FilterDefinitionBuilder_1_Exists" >}}) filter builder method and [`Descending`]({{< apiref "Overload_MongoDB_Driver_SortDefinitionBuilder_1_Descending" >}}) sort builder method to sort our documents:

```csharp
var filter = Builders<BsonDocument>.Filter.Exists("i");
var sort = Builders<BsonDocument>.Sort.Descending("i");

var document = await collection.Find(filter).Sort(sort).FirstAsync();
```

## Projecting Fields

Many times we don’t need all the data contained in a document. The [Projection]({{< relref "reference\driver\definitions.md#projections" >}}) builder will help build the projection parameter for the find operation. Below we’ll exclude the "_id" field and output the first matching document:

```csharp
var projection = Builders<BsonDocument>.Projection.Exclude("_id");
var document = await collection.Find(new BsonDocument()).Project(projection).FirstAsync();
Console.WriteLine(document.ToString());
```

## Updating Documents

There are numerous [update operators](http://docs.mongodb.org/manual/reference/operator/update-field/) supported by MongoDB.

To update at most 1 document (may be 0 if none match the filter), use the [`UpdateOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_UpdateOneAsync" >}}) method to specify the filter and the update document. Here we update the first document that meets the filter `i == 10` and set the value of `i` to `110`:

```csharp
var filter = Builders<BsonDocument>.Filter.Eq("i", 10);
var update = Builders<BsonDocument>.Update.Set("i", 110);

await collection.UpdateOneAsync(filter, update);
```

To update all documents matching the filter use the [`UpdateManyAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_UpdateManyAsync" >}}) method. Here we increment the value of `i` by `100` where `i < 100`.

```csharp
var filter = Builders<BsonDocument>.Filter.Lt("i", 100);
var update = Builders<BsonDocument>.Update.Inc("i", 100);

var result = await collection.UpdateOneAsync(filter, update);

if (result.IsModifiedCountAvailable)
{
	Console.WriteLine(result.ModifiedCount);
}
```

The update methods return an [`UpdateResult`]({{< apiref "T_MongoDB_Driver_UpdateResult" >}}) which provides information about the operation including the number of documents modified by the update.

{{% note %}}Depending on the version of the server, certain features may not be available. In those cases, we've tried to surface the ability to check for their availability.{{% /note %}}

## Deleting Documents

To delete at most 1 document (may be 0 if none match the filter) use the [`DeleteOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_DeleteOneAsync" >}}) method:

```csharp
var filter = Builders<BsonDocument>.Filter.Eq("i", 110));

await collection.DeleteOneAsync(filter);
```

To delete all documents matching the filter use the [`DeleteManyAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_DeleteManyAsync" >}}) method method. Here we delete all documents where `i >= 100`:

```csharp
var filter = Builders<BsonDocument>.Filter.Gte("i", 100));

var result = await collection.DeleteManyAsync(filter);

Console.WriteLine(result.DeletedCount);
```

The delete methods return a [`DeleteResult`]({{< apiref "T_MongoDB_Driver_DeleteResult" >}}) which provides information about the operation including the number of documents deleted.


## Bulk Writes

There are two types of bulk operations:

1. **Ordered bulk operations.**
	
	Executes all the operations in order and errors out on the first error.

1. **Unordered bulk operations.**
	
	Executes all the operations and reports any errors. Unordered bulk operations do not guarantee the order of execution.

Let’s look at two simple examples using ordered and unordered operations:

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
await collection.BulkWrite(models);

// 2. Unordered bulk operation - no guarantee of order of operation
await collection.BulkWrite(models, new BulkWriteOptions { IsOrdered = false });
```

{{% note class="important" %}}Use of the bulkWrite methods is not recommended when connected to pre-2.6 MongoDB servers, as this was the first server version to support bulk write commands for insert, update, and delete in a way that allows the driver to implement the correct semantics for BulkWriteResult and BulkWriteException. The methods will still work for pre-2.6 servers, but performance will suffer, as each write operation has to be executed one at a time.{{% /note %}}