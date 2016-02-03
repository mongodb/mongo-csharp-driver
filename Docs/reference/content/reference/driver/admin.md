+++
date = "2015-05-07T15:36:56Z"
draft = false
title = "Administration"
[menu.main]
  parent = "Driver"
  identifier = "Administration"
  weight = 30
  pre = "<i class='fa'></i>"
+++

## Adminstration

The administration operations exist in multiple places in the driver's API. Database-related operations exist on the database object and collection-related operations exist on the collection object. If there isn't a method for the admin operation you want to use, you can run a command directly using the [`RunCommand`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_RunCommand__1" >}}) or [`RunCommandAsync`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_RunCommandAsync__1" >}}) methods on [`IMongoDatabase`]({{< apiref "T_MongoDB_Driver_IMongoDatabase" >}}) is available.

## Databases

These operations exist on the [`IMongoClient`]({{< apiref "T_MongoDB_Driver_IMongoClient" >}}) interface.

### Getting a database

To get a database, use the [`GetDatabase`]({{< apiref "M_MongoDB_Driver_IMongoClient_GetDatabase" >}}). 

{{% note %}}
There is no command for creating a database. The database will be created the first time it is used.
{{% /note %}}

```csharp
// get the test database
var db = client.GetDatabase("test");
```

### Dropping a database

Use the [`DropDatabase`]({{< apiref "M_MongoDB_Driver_IMongoClient_DropDatabase" >}}) or [`DropDatabaseAsync`]({{< apiref "M_MongoDB_Driver_IMongoClient_DropDatabaseAsync" >}}) methods.

```csharp
// drops the test database
client.DropDatabase("test");
```
```csharp
// drops the test database
await client.DropDatabaseAsync("test");
```

### Listing the databases

Use the [`ListDatabases`]({{< apiref "M_MongoDB_Driver_IMongoClient_ListDatabases" >}}) or [`ListDatabasesAsync`]({{< apiref "M_MongoDB_Driver_IMongoClient_ListDatabasesAsync" >}}) methods.

```csharp
using (var cursor = client.ListDatabase())
{
	var list = cursor.ToList();
	// do something with the list
}
```
```csharp
using (var cursor = await client.ListDatabaseAsync())
{
	var list = await cursor.ToListAsync();
	// do something with the list
}
```

## Collections

These operations exists on the [`IMongoDatabase`]({{< apiref "T_MongoDB_Driver_IMongoDatabase" >}}) interface.

### Getting a collection

The [`GetCollection<TDocument>`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_GetCollection__1" >}}) method returns an [`IMongoCollection<TDocument>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}}). 

The generic parameter on the method defines the schema your application will use when working with the collection. Generally, this type will either be a [`BsonDocument`]({{< relref "reference\bson\bson_document.md" >}}) which provides no schema enforcement or a [mapped class (POCO)]({{< relref "reference\bson\mapping\index.md" >}}).

```csharp
// gets a collection named "foo" using a BsonDocument
var collection = db.GetCollection<BsonDocument>("foo");
```

For more information on working with collections, see the [CRUD Operations section]({{< relref "reference\driver\crud\index.md" >}}).

### Creating a collection

Just like databases, there is no need to create a collection before working with it. It will be created upon first use. However, certain features of collections require explicit creation. The [`CreateCollection`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_CreateCollection" >}}) and [`CreateCollectionAsync`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_CreateCollectionAsync" >}}) methods allows you to specify not only a name, but also [`CreateCollectionOptions`]({{< apiref "T_MongoDB_Driver_CreateCollectionOptions" >}}).

```csharp
var options = new CreateCollectionOptions
{
    Capped = true,
    MaxSize = 10000
});
```
```csharp
// creates a capped collection named "foo" with a maximum size of 10,000 bytes
db.CreateCollection("foo", options); 
```
```csharp
// creates a capped collection named "foo" with a maximum size of 10,000 bytes
await db.CreateCollectionAsync("foo", options); 
```

### Dropping a collection

Use the [`DropCollection`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_DropCollection" >}}) or [`DropCollectionAsync`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_DropCollectionAsync" >}}) methods.

```csharp
// drops the "foo" collection
db.DropCollection("test");
```
```csharp
// drops the "foo" collection
await db.DropCollectionAsync("test");
```

### Listing the collections

Use the [`ListCollections`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_ListCollections" >}}) or [`ListCollectionsAsync`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_ListCollectionsAsync" >}}) methods.

```csharp
using (var cursor = db.ListCollections())
{
	var list = cursor.ToList();
	// do something with the list
}
```
```csharp
using (var cursor = await db.ListCollectionsAsync())
{
	var list = await cursor.ToListAsync();
	// do something with the list
}
```

### Renaming a collection

Use the [`RenameCollection`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_RenameCollection" >}}) or [`RenameCollectionAsync`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_RenameCollectionAsync" >}}) methods.

```csharp
// rename the "foo" collection to "bar"
db.RenameCollection("foo", "bar");
```
```csharp
// rename the "foo" collection to "bar"
await db.RenameCollectionAsync("foo", "bar");
```

## Indexes

[`IMongoCollection<T>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}}) contains an [`Indexes`]({{< apiref "P_MongoDB_Driver_IMongoCollection_1_Indexes" >}}) property which gives access to all the index-related operations for a collection.

A number of the methods take an [`IndexKeysDefinition<TDocument>`]({{< apiref "T_MongoDB_Driver_IndexKeysDefinition_1" >}}). See the documentation on the [index keys builder]({{< relref "reference\driver\definitions.md#index-keys ">}}) for more information.

### Creating an index

Use the [`CreateOne`]({{< apiref "M_MongoDB_Driver_IMongoIndexManager_1_CreateOne" >}}) or [`CreateOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoIndexManager_1_CreateOneAsync" >}})  methods to create a single index. For instance, to create an ascending index on the "x" and "y" fields,

```csharp
collection.Indexes.CreateOne("{ x : 1, y : 1 }");

// or

collection.Indexes.CreateOne(new BsonDocument("x", 1).Add("y", 1));

// or

collection.Indexes.CreateOne(Builders<BsonDocument>.IndexKeys.Ascending("x").Ascending("y"));
```
```csharp
await collection.Indexes.CreateOneAsync("{ x : 1, y : 1 }");

// or

await collection.Indexes.CreateOneAsync(new BsonDocument("x", 1).Add("y", 1));

// or

await collection.Indexes.CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending("x").Ascending("y"));
```

In addition, there are a number of options available when creating index. These are present on the optional [`CreateIndexOptions`]({{< apiref "T_MongoDB_Driver_CreateIndexOptions" >}}) parameter. For instance, to create a unique ascending index on "x":

```csharp
var options = new CreateIndexOptions { Unique = true };
```
```csharp
collection.Indexes.CreateOne("{ x : 1 }", options);
```
```csharp
await collection.Indexes.CreateOneAsync("{ x : 1 }", options);
```

### Dropping an index

To drop a single index use the [`DropOne`]({{< apiref "M_MongoDB_Driver_IMongoIndexManager_1_DropOne" >}}) or [`DropOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoIndexManager_1_DropOneAsync" >}}) methods.

```csharp
// drop the index named "x_1";
collection.Indexes.DropOne("x_1");
```
```csharp
// drop the index named "x_1";
await collection.Indexes.DropOneAsync("x_1");
```

To drop all indexes use the [`DropAll`]({{< apiref "M_MongoDB_Driver_IMongoIndexManager_1_DropAll" >}}) or [`DropAllAsync`]({{< apiref "M_MongoDB_Driver_IMongoIndexManager_1_DropAllAsync" >}}) methods


```csharp
// drop all indexes
collection.Indexes.DropAll();
```
```csharp
// drop all indexes
await collection.Indexes.DropAllAsync();
```

### Listing indexes

To list all the indexes in a collection, use the [`List`]({{< apiref "M_MongoDB_Driver_IMongoIndexManager_1_List" >}}) or [`ListAsync`]({{< apiref "M_MongoDB_Driver_IMongoIndexManager_1_ListAsync" >}}) methods.

```csharp
using (var cursor = collection.Indexes.List())
{
	var list = cursor.ToList();
	// do something with the list...
}
```
```csharp
using (var cursor = await collection.Indexes.ListAsync())
{
	var list = await cursor.ToListAsync();
	// do something with the list...
}
```