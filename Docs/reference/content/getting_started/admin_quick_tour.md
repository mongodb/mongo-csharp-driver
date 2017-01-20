+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Admin Quick Tour"
[menu.main]
  parent = "Getting Started"
  weight = 30
  identifier = "Admin Quick Tour"
  pre = "<i class='fa'></i>"
+++

## MongoDB Driver Admin Quick Tour

This is the second part of the MongoDB driver quick tour. In this part, we'll look at performing some adminstrative functions. In the [first part]({{< relref "getting_started\quick_tour.md" >}}), we looked at how to perform basic CRUD (create, read, update, delete) operations. 

{{% note %}}See the [installation guide]({{< relref "getting_started\installation.md" >}}) for instructions on how to install the MongoDB Driver.{{% /note %}}


## Setup

To get started we’ll quickly connect and create `client`, `database`, and `collection` variables for use in the examples below:

```csharp
var client = new MongoClient();
var database = client.GetDatabase("foo");
var collection = client.GetCollection<BsonDocument>("bar");
```

{{% note %}}Calling the [`GetDatabase`]({{< apiref "M_MongoDB_Driver_MongoClient_GetDatabase" >}}) method on `client` does not create a database. Likewise, calling the [`GetCollection<BsonDocument>`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_GetCollection__1" >}}) method on `database` will not create a collection. Only when a database or collection are written to will they be created. Examples include the creation of an index or the insertion of a document into a previously non-existent collection.{{% /note %}}


## List the Databases

You can list all the databases using the [`ListDatabases`]({{< apiref "M_MongoDB_Driver_IMongoClient_ListDatabases" >}}) or [`ListDatabasesAsync`]({{< apiref "M_MongoDB_Driver_IMongoClient_ListDatabasesAsync" >}}) methods.

```csharp
using (var cursor = client.ListDatabases())
{
    foreach (var document in cursor.ToEnumerable())
    {
        Console.WriteLine(document.ToString()));
    }
}
```

```csharp
using (var cursor = await client.ListDatabasesAsync())
{
    await cursor.ForEachAsync(document => Console.WriteLine(document.ToString()));
}
```


## Drop a Database

You can drop a database using the [`DropDatabase`]({{< apiref "M_MongoDB_Driver_IMongoClient_DropDatabase" >}}) or [`DropDatabaseAsync`]({{< apiref "M_MongoDB_Driver_IMongoClient_DropDatabaseAsync" >}}) methods.

```csharp
client.DropDatabase("foo");
```

```csharp
await client.DropDatabaseAsync("foo");
```


## Create a Collection

A collection in MongoDB is created automatically simply by inserting a document into it. Using the [`CreateCollection`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_CreateCollection" >}}) or [`CreateCollectionAsync`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_CreateCollectionAsync" >}}) methods, you can also create a collection explicitly in order to to customize its configuration. For example, to create a capped collection sized to 1 megabyte:

```csharp
var options = new CreateCollectionOptions { Capped = true, MaxSize = 1024 * 1024 };
```
```csharp
database.CreateCollection("cappedBar", options);
```
```csharp
await database.CreateCollectionAsync("cappedBar", options);
```

## Drop a Collection

You can drop a collection with the [`DropCollection`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_DropCollection" >}}) or [`DropCollectionAsync`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_DropCollectionAsync" >}}) methods:

```csharp
database.DropCollection("cappedBar");
``` 
```csharp
await database.DropCollectionAsync("cappedBar");
``` 


## Create an Index

MongoDB supports secondary indexes. To create an index, you just specify the field or combination of fields, and for each field specify the direction of the index for that field; `1` for ascending and `-1` for descending. The following creates an ascending index on the `i` field:

```csharp
collection.Indexes.CreateOne(new BsonDocument("i", 1));

// or

var keys = Builders<BsonDocument>.IndexKeys.Ascending("i");
collection.Indexes.CreateOne(keys);
```
```csharp
await collection.Indexes.CreateOneAsync(new BsonDocument("i", 1));

// or

var keys = Builders<BsonDocument>.IndexKeys.Ascending("i");
await collection.Indexes.CreateOneAsync(keys);
```

More information about the IndexKeys definition builder is in the [reference section]({{< relref "reference\driver\definitions.md#index-keys" >}}).


## List the Indexes in a Collection

Use the [`List`]({{< apiref "M_MongoDB_Driver_IMongoIndexManager_1_List" >}}) or [`ListAsync`]({{< apiref "M_MongoDB_Driver_IMongoIndexManager_1_ListAsync" >}}) methods to list the indexes in a collection:

```csharp
using (var cursor = collection.Indexes.List())
{
    foreach (var document in cursor.ToEnumerable())
    {
        Console.WriteLine(document.ToString());
    }
}
``` 
```csharp
using (var cursor = await collection.Indexes.ListAsync())
{
	await cursor.ForEachAsync(document => Console.WriteLine(document.ToString()));	
}
``` 

The example should print the following indexes:

```json
{ "v" : 1, "key" : { "_id" : 1 }, "name" : "_id_", "ns" : "mydb.test" }
{ "v" : 1, "key" : { "i" : 1 }, "name" : "i_1", "ns" : "mydb.test" }
```


### Text Indexes

MongoDB also provides text indexes to support searching of string content. Text indexes can include any field whose value is a string or an array of string elements. To create a text index specify the string literal “text” in the index document:

```csharp
collection.Indexes.CreateOne(new BsonDocument("content", "text"));

// or

var keys = Builders<BsonDocument>.IndexKeys.Text("content");
collection.Indexes.CreateOne(keys);
```
```csharp
await collection.Indexes.CreateOneAsync(new BsonDocument("content", "text"));

// or

var keys = Builders<BsonDocument>.IndexKeys.Text("content");
await collection.Indexes.CreateOneAsync(keys);
```

As of MongoDB 2.6, text indexes are now integrated into the main query language and enabled by default:

```csharp
// insert some documents
collection.InsertMany(new []
{
    new BsonDocument("_id", 0).Add("content", "textual content"),
    new BsonDocument("_id", 1).Add("content", "additional content"),
    new BsonDocument("_id", 2).Add("content", "irrelevant content"),
});

// find them using the text index
var filter = Builders<BsonDocument>.Filter.Text("textual content -irrelevant");
var matchCount = collection.Count(filter);
Console.WriteLine("Text search matches: {0}", matchCount);

// find them using the text index with the $language operator
var englishFilter = Builders<BsonDocument>.Filter.Text("textual content -irrelevant", "english");
var matchCount = collection.Count(filter);
Console.WriteLine("Text search matches (english): {0}", matchCount);

// find the highest scoring match
var projection = Builders<BsonDocument>.Projection.MetaTextScore("score");
var doc = collection.Find(filter).Project(projection).First();
Console.WriteLine("Highest scoring document: {0}", doc);
```
```csharp
// insert some documents
await collection.InsertManyAsync(new []
{
    new BsonDocument("_id", 0).Add("content", "textual content"),
    new BsonDocument("_id", 1).Add("content", "additional content"),
    new BsonDocument("_id", 2).Add("content", "irrelevant content"),
});

// find them using the text index
var filter = Builders<BsonDocument>.Filter.Text("textual content -irrelevant");
var matchCount = await collection.CountAsync(filter);
Console.WriteLine("Text search matches: {0}", matchCount);

// find them using the text index with the $language operator
var englishFilter = Builders<BsonDocument>.Filter.Text("textual content -irrelevant", "english");
var matchCount = await collection.CountAsync(filter);
Console.WriteLine("Text search matches (english): {0}", matchCount);

// find the highest scoring match
var projection = Builders<BsonDocument>.Projection.MetaTextScore("score");
var doc = await collection.Find(filter).Project(projection).FirstAsync();
Console.WriteLine("Highest scoring document: {0}", doc);
```

and it should print:

```text
Text search matches: 2
Text search matches (english): 2
Highest scoring document: { "_id" : 1, "content" : "additional content", "score" : 0.75 }
```

For more information about text search, see the [text index]({{< docsref "core/index-text/" >}}) and the [$text query operator]({{< docsref "reference/operator/query/text/" >}}) documentation.


## Running a Command

Not all commands have a specific helper, however you can run any command by using the [`RunCommand`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_RunCommand__1" >}}) or [`RunCommandAsync`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_RunCommandAsync__1" >}}) methods. Here we call the [buildInfo]({{< docsref "reference/command/buildInfo" >}}) command: 

```csharp
var buildInfoCommand = new BsonDocument("buildinfo", 1);
```
```csharp
var result = database.RunCommand(buildInfoCommand);
```
```csharp
var result = await database.RunCommandAsync(buildInfoCommand);
```