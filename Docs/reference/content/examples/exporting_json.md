+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Exporting JSON"
[menu.main]
  parent = "Examples"
  identifier = "Exporting JSON"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Exporting JSON

The .NET BSON library supports writing JSON documents with the [`JsonWriter`]({{< apiref "T_MongoDB_Bson_IO_JsonWriter" >}}) class. 

The programs below will export all documents from a collection to a file with one document per line. There are two versions of the program, one using the synchronous API and the other using the asynchronous API.

Given the collection's contents:

```bash
> db.mydata.find()
{ "_id" : ObjectId("5513306a2dfd32ffd580e323"), "x" : 1.0 }
{ "_id" : ObjectId("5513306c2dfd32ffd580e324"), "x" : 2.0 }
{ "_id" : ObjectId("5513306e2dfd32ffd580e325"), "x" : 3.0 }
{ "_id" : ObjectId("551330712dfd32ffd580e326"), "x" : 4.0 }
```

And the synchronous version of the program:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

// ...

string outputFileName; // initialize to the output file
IMongoCollection<BsonDocument> collection; // initialize to the collection to read from

using (var streamWriter = new StreamWriter(outputFileName))
{
    var cursor = collection.Find(new BsonDocument()).ToCursor();
    foreach (var document in cursor.ToEnumerable())
    {
        using (var stringWriter = new StringWriter())
        using (var jsonWriter = new JsonWriter(stringWriter))
        {
            var context = BsonSerializationContext.CreateRoot(jsonWriter);
            collection.DocumentSerializer.Serialize(context, document);
            var line = stringWriter.ToString();
            streamWriter.WriteLine(line);
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

string outputFileName; // initialize to the output file
IMongoCollection<BsonDocument> collection; // initialize to the collection to read from

using (var streamWriter = new StreamWriter(outputFileName))
{
    await collection.Find(new BsonDocument())
        .ForEachAsync(async (document) =>
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var context = BsonSerializationContext.CreateRoot(jsonWriter);
                collection.DocumentSerializer.Serialize(context, document);
                var line = stringWriter.ToString();
                await streamWriter.WriteLineAsync(line);
            }
        });
}
```

The output file should look this:

```json
{ "_id" : ObjectId("5513306a2dfd32ffd580e323"), "x" : 1.0 }
{ "_id" : ObjectId("5513306c2dfd32ffd580e324"), "x" : 2.0 }
{ "_id" : ObjectId("5513306e2dfd32ffd580e325"), "x" : 3.0 }
{ "_id" : ObjectId("551330712dfd32ffd580e326"), "x" : 4.0 }
```