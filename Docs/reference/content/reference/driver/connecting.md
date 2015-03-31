+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Connecting"
[menu.main]
  parent = "Driver"
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

The above connection string sets the "replicaSet" value to "rs0" and the "uuidRepresentation" to "standard".

For a comprehensive list of the available options, see the MongoDB [connection string](http://docs.mongodb.org/manual/reference/connection-string/) documentation.


## MongoClient

A [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) object will be the root object. It is all that is needed to handle connecting to servers, monitoring servers, and performing operations against those servers. Without any arguments, constructing a [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) instance will connect to "localhost" port 27017.

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


### Monitoring

[`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}) utilizes an [ICluster]({{< apiref "T_MongoDB_Driver_Core_Clusters_ICluster" >}}) from [MongoDB.Driver.Core]({{< relref "reference\driver_core\index.md" >}}) which handles monitoring the cluster.