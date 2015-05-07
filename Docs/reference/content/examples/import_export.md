+++
date = "2015-03-17T15:36:56Z"
draft = true
title = "Import and Export"
[menu.main]
  parent = "Examples"
  identifier = "Import/Export"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Rewriting MongoImport and MongoExport

The .NET BSON library supports reading and writing JSON documents with the [`JsonReader`]({{< apiref "T_MongoDB_Bson_IO_JsonReader" >}}) and [`JsonWriter`]({{< apiref "T_MongoDB_Bson_IO_JsonWriter" >}}) classes. Both of these also handle both flavors of [MongoDB Extended JSON]({{< docsref "reference/mongodb-extended-json/" >}}).

Neither of these implementations are the most performant solutions, but they illustrate using multiple features of the driver together.

## Importing

The below program will import all documents from a file with one document per line into the collection.

Given the file's contents:

```json
{ "_id" : ObjectId("5513306a2dfd32ffd580e323"), "x" : 1.0 }
{ "_id" : ObjectId("5513306c2dfd32ffd580e324"), "x" : 2.0 }
{ "_id" : ObjectId("5513306e2dfd32ffd580e325"), "x" : 3.0 }
{ "_id" : ObjectId("551330712dfd32ffd580e326"), "x" : 4.0 }
```

And the program:

```csharp
string inputFileName; // initialize from to the input file
IMongoCollection<BsonDocument> collection; // initialize to the collection to write to.

using (var streamReader = new StreamReader(inputFileName))
using (var jsonReader = new JsonReader(streamReader))
{
    while (!jsonReader.IsAtEndOfFile())
    {
        var context = BsonDeserializationContext.CreateRoot(jsonReader);
        var doc = collection.DocumentSerializer.Deserialize(context);
        await collection.InsertOneAsync(doc);
    }
}
```

## Exporting 

The below program will export all documents from a collection to a file with one document per line. 

```csharp
string outputFileName; // initialize from to the output file
IMongoCollection<BsonDocument> collection; // initialize to the collection to read from

using (var streamWriter = new StreamWriter(outputFileName))
{
    await col.Find(new BsonDocument())
        .ForEachAsync(doc => streamWriter.WriteLine(doc.ToString()));
}
```

The result file should look like this:

```json
{ "_id" : ObjectId("5513306a2dfd32ffd580e323"), "x" : 1.0 }
{ "_id" : ObjectId("5513306c2dfd32ffd580e324"), "x" : 2.0 }
{ "_id" : ObjectId("5513306e2dfd32ffd580e325"), "x" : 3.0 }
{ "_id" : ObjectId("551330712dfd32ffd580e326"), "x" : 4.0 }
```