+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Connecting"
[menu.main]
  parent = "Driver"
  identifier = "Reference Connecting"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Connection String

The [connection string](http://docs.mongodb.org/manual/reference/connection-string/) is the simplest way to connect to one or more MongoDB servers. A connection string mostly follows [RFC 3986](http://tools.ietf.org/html/rfc3986) with the exception of the domain name. For MongoDB, it is possible to list multiple domain names separated by a comma. Below are some example connection strings


- For a standalone mongod, mongos, or a direct connection to a member of a replica set:
	
	```
	mongodb://host:27017
	```

- To connect to multiple mongos or a replica set:

	```
	mongodb://host1:27017,host2:27017
	```

The [authentication guide]({{< relref "authentication.md" >}}) contains information on how to provide credentials.

{{% note class="important" %}}When connecting to a replica set, it is highly suggested that you include the replica set name as a connection string option. This will allow the driver to skip the cluster discovery step and ensure that all hosts on the seedlist are connecting to the intended replica set.{{% /note %}}

### The Database Component

The database component is optional and is used to indicate which database to authenticate against. When the database component is not provided, the "admin" database is used.

```
mongodb://host:27017/mydb
```

Above, the database by the name of "mydb" is where the credentials are stored for the application.

{{% note %}}Some drivers utilize the database component to indicate which database to work with by default. The .NET driver, while it parses the database component, does not use the database component for anything other than authentication.{{% /note %}}

### Options

Many options can be provided via the connection string. The ones that cannot may be provided in a [`MongoClientSettings`]({{< apiref "T_MongoDB_Driver_MongoClientSettings" >}}) object. To provide an option on the connection string, append a `?` and separate multiple options by an `&`.

```
mongodb://host:27017/?replicaSet=rs0&uuidRepresentation=standard
```

The above connection string sets the `replicaSet` value to `rs0` and the `uuidRepresentation` to `standard`.

For a comprehensive list of the available options, see the MongoDB [connection string](http://docs.mongodb.org/manual/reference/connection-string/) documentation.


## Mongo Client

A [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) object will be the root object. It is thread-safe and is all that is needed to handle connecting to servers, monitoring servers, and performing operations against those servers. Without any arguments, constructing a [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) instance will connect to "localhost" port 27017.

```
var client = new MongoClient();
```

Alternatively, a connection string may be provided:

```
var client = new MongoClient("mongodb://host:27017,host2:27017/?replicaSet=rs0");
```

Finally, [`MongoClientSettings`]({{< apiref "T_MongoDB_Driver_MongoClientSettings" >}}) provides an in code way to set the same options.

```
var settings = new MongoClientSettings { ReplicaSetName = "rs0" };
var client = new MongoClient(settings);
```

### Low-Level Customization

There are a number of settings that are not configurable directly from [`MongoClientSettings`]({{< apiref "T_MongoDB_Driver_MongoClientSettings" >}}). These settings are able to be configured through the [`ClusterConfigurator`]({{< apiref "P_MongoDB_Driver_MongoClientSettings_ClusterConfigurator" >}}) property which provides a [`ClusterBuilder`]({{< apiref "T_MongoDB_Driver_Core_Configuration_ClusterBuilder" >}}) to manipulate. An example of these is adding a logger using the [eventing infrastructure]({{< relref "reference\driver_core\events.md" >}}).

### Re-use

It is recommended to store a [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) instance in a global place, either as a static variable or in an IoC container with a singleton lifetime. 

However, multiple [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) instances created with the same settings will utilize the same connection pools underneath. Unfortunately, certain types of settings are not able to be compared for equality. For instance, the [`ClusterConfigurator`]({{< apiref "P_MongoDB_Driver_MongoClientSettings_ClusterConfigurator" >}}) property is a delegate and only its address is known for comparison. If you wish to construct multiple [`MongoClients`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}), ensure that your delegates are all using the same address if the intent is to share connection pools.

### Monitoring

[`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) utilizes an [`ICluster`]({{< apiref "T_MongoDB_Driver_Core_Clusters_ICluster" >}}) from [MongoDB.Driver.Core]({{< relref "reference\driver_core\index.md" >}}) which handles monitoring the cluster.


## Mongo Database

An [`IMongoDatabase`]({{< apiref "T_MongoDB_Driver_IMongoDatabase" >}}) represents a database in a MongoDB server. Databases are retrieved from an [`IMongoClient`]({{< apiref "T_MongoDB_Driver_IMongoClient" >}}) instance using the [`GetDatabase`]({{< apiref "M_MongoDB_Driver_IMongoClient_GetDatabase" >}}) method:

```csharp
var db = client.GetDatabase("hr");
```

Above, we have gotten the "hr" database. If the database does not exist on the server, it will be created automatically upon first use. If you want to use more than one database, call [`GetDatabase`]({{< apiref "M_MongoDB_Driver_IMongoClient_GetDatabase" >}}) once for each database you'd like to work with.

### Re-use

The implementation of [`IMongoDatabase`]({{< apiref "T_MongoDB_Driver_IMongoDatabase" >}}) provided by a [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) is thread-safe and is safe to be stored globally or in an IoC container.


### Mongo Collection

An [`IMongoCollection<TDocument>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}}) represents a collection in a MongoDB database. Collections are retrieved from an [`IMongoDatabase`]({{< apiref "T_MongoDB_Driver_IMongoDatabase" >}}) with the [`GetCollection<TDocument>`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_GetCollection__1" >}}) method:

```csharp
var collection = db.GetCollection<BsonDocument>("people");
```

Above, we have gotten the "people" collection. IF the collection does not exist on the server, it will be created automatically upon first use. If you want to use more than one database, call [`GetCollection<TDocument>`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_GetCollection__1" >}}) once for each database you'd like to work with.

The generic parameter `TDocument` is the type of document that is stored in your collection. It can, effectively, be any type that can be mapped to and from BSON. The driver utilizes the [BSON library]({{< relref "reference\bson\index.md" >}}) to handle this mapping. The most common types you will use are:

1. Custom Class - useful for representing known schemas. See the [mapping section]({{< relref "reference\bson\mapping\index.md" >}}) for more information. The majority of applications will not work with dynamic schemas, but rather with something more rigid. In addition, if you work in a static language, it is nice to work with static types that provide compile time type checking.
1. [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) - useful for representing dynamic schemas.

{{% note %}}It is possible to mix both these models by utilizing a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) inside your custom class.{{% /note %}}

A majority of the methods and extension methods for an [`IMongoCollection<TDocument>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}}) utilize the `TDocument` generic parameter in some fashion.

### Re-use

The implementation of [`IMongoCollection<TDocument>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}}) ultimately provided by a [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) is thread-safe and is safe to be stored globally or in an IoC container.