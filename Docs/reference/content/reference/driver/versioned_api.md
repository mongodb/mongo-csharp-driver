+++
date = "2021-02-12T15:36:56Z"
draft = false
title = "Versioned API"
[menu.main]
  parent = "Driver"
  identifier = "Versioned API"
  weight = 70
  pre = "<i class='fa'></i>"
+++

## Versioned API

Versioned API is a new feature in MongoDB 5.0 that allows user-selectable API versions, subsets of MongoDB
server semantics, to be declared on a client. During communication with a server, clients with a declared
API version will force the server to behave in a manner compatible with the API version. Declaring an API
version on a client can be used to ensure consistent responses from a server, providing long term API
stability for an application. The declared API version is applied to all commands run through the client, including those sent through
the generic RunCommand helper.

You can specify [`ServerApi`]({{< apiref "T_MongoDB_Driver_Core_ServerApi" >}}) via [`MongoClientSettings`]({{< apiref "T_MongoDB_Driver_MongoClientSettings" >}}):

```csharp
var serverApi = new ServerApi(ServerApiVersion.V1);
var settings = new MongoClientSettings { ServerApi = serverApi };
var client = new MongoClient(settings);
```

The [`ServerApi`]({{< apiref "T_MongoDB_Driver_Core_ServerApi" >}}) can be specified only when creating a [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) and cannot be changed during the course of execution. Thus to run any command with a different
API version or without declaring one, create a separate [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) that declares the appropriate API version.

The [`ServerApi`]({{< apiref "T_MongoDB_Driver_Core_ServerApi" >}}) consists of 3 fields. One mandatory: "serverApiVersion" and two optional: "strict" and "deprecationErrors".

### Server API version

The [`ServerApiVersion`]({{< apiref "T_MongoDB_Driver_Core_ServerApiVersion" >}}) is a required parameter of [`ServerApi`]({{< apiref "T_MongoDB_Driver_Core_ServerApi" >}}) and represents the version number for server to behave in compatiblity with. Currently only version 1 is available. It can be acquired via constant field:

```csharp
var serverApiVersion = ServerApiVersion.V1;
```

### Strict flag

The strict flag is optional and defaults to false. Setting true causes commands (or their specific behavior, like command options or aggregation pipeline stages) to fail if they are not part of the declared API version.

```csharp
var serverApi = new ServerApi(ServerApiVersion.V1, strict: true);
var settings = new MongoClientSettings { ServerApi = serverApi };
var client = new MongoClient(settings);
var database = client.GetDatabase("db");
var collection = database.GetCollection<BsonDocument>("coll");
var result = collection.Distinct((FieldDefinition<BsonDocument, int>)"a.b", new BsonDocument("x", 1)); // Fails with:
// MongoDB.Driver.MongoCommandException : Command distinct failed: Provided apiStrict:true, but the command distinct is not in API Version 1.
```

### Deprecation errors flag

The deprecation errors flag is optional and defaults to false. Setting true causes commands (or their specific behavior) to fail with APIDeprecationError if they are deprecated in declared API version.

{{% note %}}Currently there are no deprecations in version 1, so theoretical example is used.{{% /note %}}

```csharp
var serverApi = new ServerApi(ServerApiVersion.V1, deprecationErrors: true);
var settings = new MongoClientSettings { ServerApi = serverApi };
var client = new MongoClient(settings);
var database = client.GetDatabase("db");
var result = database.RunCommand<BsonDocument>(new BsonDocument("commandDeprecatedInV1", 1)); // Example fail:
// MongoDB.Driver.MongoCommandException : Command commandDeprecatedInV1 failed: Provided deprecationErrors:true, but the command commandDeprecatedInV1 is deprecated in API Version 1.
```
