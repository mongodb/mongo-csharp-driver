+++
date = "2022-07-11T16:56:14Z"
draft = false
title = "Frequently Encountered Issues"
[menu.main]
  parent = "Issues & Help"
  identifier = "Frequently Encountered Issues"
  weight = 10
  pre = "<i class='fa'></i>"
+++

# Frequently Encountered Issues

## Timeout during server selection

Each driver operation requires choosing of a healthy server satisfying the [server selection criteria](https://www.mongodb.com/docs/manual/core/read-preference-mechanics/). If an appropriate server cannot be found within the [server selection timeout]({{< apiref "T_MongoDB_Driver_ServerSelectionTimeout" >}}), the driver will throw a server selection timeout exception. The exception will look similar to the following:

```csharp
A timeout occurred after 30000ms selecting a server using CompositeServerSelector{ Selectors = MongoDB.Driver.MongoClient+AreSessionsSupportedServerSelector, LatencyLimitingServerSelector{ AllowedLatencyRange = 00:00:00.0150000 }, OperationsCountServerSelector }. 
Client view of cluster state is 
{ 
    ClusterId : "1", 
    Type : "Unknown", 
    State : "Disconnected", 
    Servers : 
    [{
        ServerId: "{ ClusterId : 1, EndPoint : "Unspecified/localhost:27017" }",
        EndPoint: "Unspecified/localhost:27017",
        ReasonChanged: "Heartbeat",
        State: "Disconnected",
        ServerVersion: ,
        TopologyVersion: ,
        Type: "Unknown",
        HeartbeatException: "<exception details>"
    }] 
}.
```
The error message consists of few parts:

1. The server selection timeout (e.g. 30000ms).
2. The server selectors considered (e.g. `CompositeServerSelector` containing `AreSessionsSupportedServerSelector`, `LatencyLimitingServerSelector`, and `OperationsCountServerSelector`.) 
3. The driver's current view of the cluster topology. A key part is the list of servers that the driver is aware of. Each server description contains an exhaustive description of its current state including information about an endpoint, a server version, a server type, and its current health state. If the server is not heathy then `HeartbeatException` will contain the exception from the last failed heartbeat. Analyzing the `HeartbeatException` on each cluster node can assist in diagnosing most server selection issues. Below are a few common heartbeat exceptions:

    * `No connection could be made because the target machine actively refused it`: The driver cannot see this cluster node. This can be because the cluster node has crashed, a firewall is preventing network traffic from reaching the cluster node or port, or some other network error is preventing traffic from being successfully routed to the cluster node.
    
    * `Attempted to read past the end of the stream`: This error happens when the driver can't connect to the cluster nodes due to a network error, misconfigured firewall, or other network issue. Ensure that all cluster nodes are reachable. One common cause of this error is when the client machine's IP address is not configured in the Atlas IPs Access List, which can be found under the Network Access tab for your Atlas Project.
    
    * `The remote certificate is invalid according to the validation procedure`: This error typically indicates a TLS/SSL-related problem such as an expired/invalid certificate or an untrusted root CA. You can use tools like `openssl s_client` to debug TLS/SSL-related certificate problems.

## MongoWaitQueueFullException. The wait queue for acquiring a connection to server is full.

This exception usually indicates a threading or concurrency problem in your application. When the driver executes a read or write, it will check out a connection from the selected server's connection pool. If that pool is already at `maxPoolSize` (default 100), then the requesting thread will block in a wait queue (which has a default size of `5 x maxPoolSize` or 500). If the wait queue is also full, then a `MongoWaitQueueFullException` will be thrown. To resolve this issue, you can try the following (in order of preference):

1. Tune your indexes. By improving the performance of your queries, operations will take less time to complete and reduce the number of concurrent connections needed for your workload.
2. If you have long-running, analytical queries, you may wish to isolate them to dedicated analytics nodes using `readPreferenceTags` or a hidden secondary.
3. Increase `maxPoolSize` to allow more simultaneous operations to a given cluster node. If your MongoDB cluster does not have sufficient resources to handle the additional connections and simultaneous workload, performance can actually decrease due to resource contention on the cluster nodes. Adjust this setting only with careful consideration and testing.
4. Increase `waitQueueMultiple` to allow more threads/tasks to block waiting for a connection. This is rarely the right solution. It is better to address the concurrency problems in your application.

## Unsupported LINQ or Builder expressions

Each LINQ or Builder expression must be translated into MQL (MongoDB Query Language), which is executed by the server. Unfortunately this is not always possible for two main reasons:

1. You are attempting to use a .NET feature that does not have an obvious or equivalent MongoDB representation. For example, .NET and MongoDB have different semantics around collations.
2. The driver doesn't support a particular transformation from LINQ or Builder expression into a server query. It may happen because the provided query is too complicated or because some feature has not been implemented yet by the driver.

If you see an `Unsupported filter ...` or `Expression not supported ...` exception message, we recommend trying the following steps:

1. Try configuring the new [LINQ3 provider]({{< relref "reference\driver\crud\linq3.md" >}}). The LINQ3 provider contains many fixes and new features over the LINQ2 provider. 
2. Try simplify your query as much as possible.
3. If the above steps don't resolve your issue, it's always possible to provide a query as a `BsonDocument` or JSON string. All driver definition classes such as `FilterDefinition`, `ProjectionDefinition`, and `PipelineDefinition` support implicit conversion from `BsonDocument` or JSON string. For example, the following filters will render to the same MQL when used in a query or aggregation:

```csharp
    FilterDefinition<Entity> typedFilter = Builders<Entity>.Filter.Eq(e => e.A, 1);
    FilterDefinition<Entity> bsonFilter = new BsonDocument {{ "a", 1 }};
    FilterDefinition<Entity> jsonFilter = "{ a : 1 }";
```

{{% note %}}
Note that if you use `BsonDocument` or JSON string, then [BsonClassMap]({{< relref "reference\bson\mapping\index.md" >}}), BSON serialization attributes, and serialization conventions will not be taken into account when rendering the MQL. Field names must match the names and casing as stored by the server. For example, when referencing the `_id` field, you must refer to it using `_id` (not `Id` as used in C#) in `BsonDocument` or JSON string definitions. Similarly if you have a property `FirstName` annotated with `[BsonElement("first_name")]`, you must refer to it as `first_name` in `BsonDocument` or JSON string definitions.
{{% /note %}}

It's also possible to combine the raw and typed forms in the same query:

```csharp
FilterDefinition<Entity> filter = Builders<Entity>.Filter.And(Builders<Entity>.Filter.Eq(e => e.A, 1), BsonDocument.Parse("{ b : 2 }"));
```
