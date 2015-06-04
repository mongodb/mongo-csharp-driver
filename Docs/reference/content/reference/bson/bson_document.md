+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "BsonDocument"
[menu.main]
  parent = "BSON"
  identifier = "BsonDocument"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## BsonDocument

[`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) is the default type used for documents. It handles dynamic documents of any complexity. For instance, the document `{ a: 1, b: [{ c: 1 }] }` can be built as follows:

```csharp
var doc = new BsonDocument
{
	{ "a", 1 },
	{ "b", new BsonArray
		   {
		   		new BsonDocument("c", 1)
		   }}
};
```

In addition, there is a [`Parse`]({{< apiref "M_MongoDB_Bson_BsonDocument_Parse" >}}) method to make reading a JSON string simple.

```csharp
var doc = BsonDocument.Parse("{ a: 1, b: [{ c: 1 }] }");
```