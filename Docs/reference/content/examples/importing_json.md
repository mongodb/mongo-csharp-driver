+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Importing JSON"
[menu.main]
  parent = "Examples"
  identifier = "Importing JSON"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## Importing JSON

The .NET BSON library supports reading JSON documents with the [`JsonReader`]({{< apiref "T_MongoDB_Bson_IO_JsonReader" >}}) class. 

The programs below will import all documents from a file with one document per line into the collection. There are two versions of the program, one using the synchronous API and the other using the asynchronous API.

Given the input file's contents:

```json
{ "_id" : ObjectId("5513306a2dfd32ffd580e323"), "x" : 1.0 }
{ "_id" : ObjectId("5513306c2dfd32ffd580e324"), "x" : 2.0 }
{ "_id" : ObjectId("5513306e2dfd32ffd580e325"), "x" : 3.0 }
{ "_id" : ObjectId("551330712dfd32ffd580e326"), "x" : 4.0 }
```

And the synchronous version of the program::

```csharp
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

// ...

string inputFileName; // initialize to the input file
IMongoCollection<BsonDocument> collection; // initialize to the collection to write to.

using (var streamReader = new StreamReader(inputFileName))
{
    string line;
    while ((line = streamReader.ReadLine()) != null)
    {
        using (var jsonReader = new JsonReader(line))
        {
            var context = BsonDeserializationContext.CreateRoot(jsonReader);
            var document = collection.DocumentSerializer.Deserialize(context);
            collection.InsertOne(document);
        }
    }
}
```

Or the asynchronous version of the program:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

// ...

string inputFileName; // initialize to the input file
IMongoCollection<BsonDocument> collection; // initialize to the collection to write to.

using (var streamReader = new StreamReader(inputFileName))
{
    string line;
    while ((line = await streamReader.ReadLineAsync()) != null)
    {
        using (var jsonReader = new JsonReader(line))
        {
            var context = BsonDeserializationContext.CreateRoot(jsonReader);
            var document = collection.DocumentSerializer.Deserialize(context);
            await collection.InsertOneAsync(document);
        }
    }
}
```

The collection's contents should look like this:

```bash
> db.mydata.find()
{ "_id" : ObjectId("5513306a2dfd32ffd580e323"), "x" : 1.0 }
{ "_id" : ObjectId("5513306c2dfd32ffd580e324"), "x" : 2.0 }
{ "_id" : ObjectId("5513306e2dfd32ffd580e325"), "x" : 3.0 }
{ "_id" : ObjectId("551330712dfd32ffd580e326"), "x" : 4.0 }
```