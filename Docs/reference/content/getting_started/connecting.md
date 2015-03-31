+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Connecting to the Server"
[menu.main]
  parent = "Getting Started"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Connecting

A [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) instance is the root object to handle connecting to the database.

```csharp
var client = new MongoClient();
```

This will connect to a mongod or mongos running on localhost port 27017. If you'd like to use a remote host, you can provide a [connection string](http://docs.mongodb.org/manual/reference/connection-string/) to the constructor or construct a [`MongoClientSettings`]({{< apiref "T_MongoDB_Driver_MongoClientSettings" >}}) object and pass it in. To see more about connecting with a client, see the [reference guide]({{< relref "reference\driver\connecting.md" >}}).

{{% note %}}There is no connecting or disconnecting. A connection pool is used and connections are managed automatically.{{% /note %}}

## Database 

From here, you'll want to retrieve an [`IMongoDatabase`]({{< apiref "T_MongoDB_Driver_IMongoDatabase" >}}) instance.

```csharp
var db = client.GetDatabase("test");
```

This will retrieve a reference to the database named "test" in MongoDB. There is no need to create the database before hand. It will get created upon first use. If you use more than one database, call GetDatabase again with a different name.


## Collection

From here, you'll need to retrieve a reference to an [`IMongoCollection<TDocument>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}}) instance, where `TDocument` is the type of document with which to work. This will be either a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) or a custom class of your own. You would use a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) when the data you are working with is so free form that it would be difficult or impossible to define domain classes for it or because you want to handle the mapping yourself. 

One constraint on your custom class is that it must contain an `Id` field. You can read more about customizing classes in the [reference guide]({{< relref "reference\bson\mapping\index.md" >}}).

Consider the following class definition:

```csharp
public class Entity
{
    public ObjectId Id { get; set; }

    public string Name { get; set; }
}
```

Now we can get a reference to a collection.

```csharp
var collection = db.GetCollection<Entity>("entities");
```

Again, as with database, there is no need to create a collection before it's used. It will get created automatically.