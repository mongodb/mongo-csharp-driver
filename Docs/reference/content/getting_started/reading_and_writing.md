+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Reading and Writing"
[menu.main]
  parent = "Getting Started"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## Reading and Writing

This page is a brief overview of performing basic reading and writing operations with the driver. All the operations available in the API are async utilizing [`Tasks`]({{< msdnref "system.threading.tasks.task" >}}).

For more information about interacting with the server, refer to the [reference guide]({{< relref "reference\driver\crud\index.md" >}}).

The rest of this document assumes the following class and collection:

```csharp
public class Person
{
    public ObjectId Id { get; set; }

    public string Name { get; set; }

    public int Age { get; set; }

    public string Profession { get; set; }
}

var collection = db.GetCollection<Person>("people");
```

## Inserting a document

To insert a document, use the [`InsertOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_InsertOneAsync" >}}) method.

```csharp
var jane = new Person { Name = "Jane McJane", Age = 24, Profession = "Hacker" };

await collection.InsertOneAsync(jane);
```

After insertion, the Jane's `Id` property will contain the automatically generated identifier. For more on Id generation, see the [reference guide]({{< relref "reference\bson\mapping\index.md#id-generators" >}}).

## Finding a Document

To find all the people who are younger than 42, use the [`Find`]({{< apiref "M_MongoDB_Driver_IMongoCollectionExtensions_Find__1_1" >}}) method such as follows:

```csharp
var people = await collection.Find(x => x.Age < 42).ToListAsync();
```

The lambda expression gets translated into the BSON filter `{ Age: { $lt: 42 } }`. Not everything is supported via expression trees, but most of the common operations are. See the [reference guide]({{< relref "reference\driver\definitions.md#filters" >}}) for more information.

## Updating

To update a document, there are 2 methods for doing so. First, you can update specific fields. For instance, I'd like to change Tom's profession to "Musician".

```csharp
var result = await collection.UpdateOneAsync(
	x => x.Name == "Tom",
	Builders<Person>.Update.Set(x => x.Profession, "Musician"));
```

This will generate a filter of `{ Name: "Tom" }` and an update specification of `{ $set: { Profession: "Musician" } }`. Only one document will get updated even if there is more than one person named "Tom" because we used [`UpdateOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_UpdateOneAsync" >}}) instead of [`UpdateManyAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_UpdateManyAsync" >}}). More information on updates is available in the [reference guide]({{< relref "reference\driver\crud\writing.md#update-and-replace" >}}).

Alternatively, if we want to replace a  document completely, we can use the [`ReplaceOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_ReplaceOneAsync" >}}) method. Assuming Tom's `Id` value is `550c4aa98e59471bddf68eef`:

```csharp
var tom = await collection.Find(x => x.Id == ObjectId.Parse("550c4aa98e59471bddf68eef"))
	.SingleAsync();

tom.Name = "Thomas";
tom.Age = 43;
tom.Profession = "Hacker";

var result = await collection.ReplaceOneAsync(x => x.Id == tom.Id, tom);
```

{{% note %}}Identifiers in MongoDB are immutable, so you can't replace a document with another one where the identifier is different.{{% /note %}} 


## Deleting

Finally, to delete Tom, you would use the [`DeleteOneAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_DeleteOneAsync" >}}) or [`DeleteManyAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_DeleteManyAsync" >}}) methods, such as in the following:

```csharp
var result = await collection.DeleteOneAsync(x => x.Id == tom.Id);
```