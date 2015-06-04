+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "BSON/JSON"
[menu.main]
  parent = "BSON"
  identifier = "BSON/JSON"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Reading

The [`IBsonReader`]({{< apiref "T_MongoDB_Bson_IO_IBsonReader" >}}) interface contains all the methods necessary to read a [BSON](http://bsonspec.org) document or a [JSON](http://json.org) document. There is an implementation for each format.


### BSON

[`BsonBinaryReader`]({{< apiref "T_MongoDB_Bson_IO_BsonBinaryReader" >}}) is for reading binary BSON. For example, to read a BSON file containing the document `{ a: 1 }`:

```csharp
string inputFileName; // initialize to a file containing BSON

using (var stream = File.OpenRead(inputFileName))
using (var reader = new BsonBinaryReader(stream))
{
	reader.ReadStartDocument();
	string fieldName = reader.ReadName();
	int value = reader.ReadInt32();
	reader.ReadEndDocument();
}
```


### JSON

In the same way, we can read a JSON string using a [`JsonReader`]({{< apiref "T_MongoDB_Bson_IO_JsonReader" >}}). For example, to read the document `{ a: 1 }`:

```csharp
var jsonString = "{ a: 1 }";
using (var reader = new JsonReader(jsonString))
{
	reader.ReadStartDocument();
	string fieldName = reader.ReadName();
	int value = reader.ReadInt32();
	reader.ReadEndDocument();
}
```

[`JsonReader`]({{< apiref "T_MongoDB_Bson_IO_JsonReader" >}}) supports reading strict JSON as well as both flavors of [MongoDB Extended JSON]({{< docsref "reference/mongodb-extended-json/" >}}).


## Writing

The [`IBsonWriter`]({{< apiref "T_MongoDB_Bson_IO_IBsonWriter" >}}) interface contains all the methods necessary to write a [BSON](http://bsonspec.org) document or a [JSON](http://json.org) document. There is an implementation for each format.


### BSON

[`BsonBinaryWriter`]({{< apiref "T_MongoDB_Bson_IO_BsonBinaryWriter" >}}) is for writing binary BSON. For example, to write the document `{ a: 1 }` to a BSON file:

```csharp
string outputFileName; // initialize to the file to write to.

using (var stream = File.OpenWrite(outputFileName))
using (var writer = new BsonBinaryWriter(stream))
{
    writer.WriteStartDocument();
    writer.WriteName("a");
    writer.WriteInt32(1);
    writer.WriteEndDocument();
}
```

### JSON

In the same way, we can write a JSON string using a [`JsonWriter`]({{< apiref "T_MongoDB_Bson_IO_JsonWriter" >}}). For example, to write the document `{ a: 1 }`:

```csharp
string outputFileName; // initialize to the file to write to.

using (var output = new StreamWriter(outputFileName))
using (var writer = new JsonWriter(output))
{
    writer.WriteStartDocument();
    writer.WriteName("a");
    writer.WriteInt32(1);
    writer.WriteEndDocument();
}
```

#### Settings

[`JsonWriter`]({{< apiref "T_MongoDB_Bson_IO_JsonWriter" >}}) supports writing strict JSON as well as both flavors of [MongoDB Extended JSON]({{< docsref "reference/mongodb-extended-json/" >}}). This, and other things, can be customized with the [`JsonWriterSettings`]({{< apiref "T_MongoDB_Bson_IO_JsonWriterSettings" >}}) class.

For instance, to write in a format for the [MongoDB Shell](http://docs.mongodb.org/manual/administration/scripting/), you can set the [`OutputMode`]({{< apiref "P_MongoDB_Bson_IO_JsonWriterSettings_OutputMode" >}}) to `Shell` and also set the [`ShellVersion`]({{< apiref "P_MongoDB_Bson_IO_JsonWriterSettings_ShellVersion" >}}) to the desired shell version.

```csharp
var settings = new JsonWriterSettings
{
	OutputMode = JsonOutputMode.Shell,
	ShellVersion = new Version(3.0) // target the syntax of MongoDB 3.0
};
```