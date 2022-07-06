+++
date = "2022-07-11T16:56:14Z"
draft = false
title = "Common issues"
[menu.main]
  weight = 1000
  identifier = "Common issues"
  pre = "<i class='fa fa-life-ring'></i>"
+++

# Frequency Encountered Issues

## Timeout during server selecting

Each driver operation requires selecting a healthy server to be run on. In case if such server has not been found during server selection [timeout]({{< apiref "T_MongoDB_Driver_ServerSelectionTimeout" >}}), the driver will throw server selecting timeout exception. The exception will look similar to:

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
The error message consist of few parts:

1. Used server selection timeout and a description of server selector used for. 
2. The current cluster description. The main part of it is a list of servers that the driver is aware of. Each server description contains exhaustive description of its current state including information about an endpoint, a server version, a server type and its current health state. If the server is not heathy, most-likely it has happened because the latest heartbeat attempts were failed. The server description tracks such failures in HeartbeatException. This is the main area that should be analyzed for issue resolving. Most of heartbeat exception messages are self explanatory. Below there are few well-known frequent heartbeat exceptions:

    * `No connection could be made because the target machine actively refused it` - a driver doesn't see a server endpoint. In most cases this means that a server is not visible in network or has not been launched.
    
    * `Attempted to read past the end of the stream.` - this error happens when driver can't connect to a server due to network error. Make sure that a configured server is accessible. One typical example of when this error happens is when client's machine IP is not configured in the Atlas IPs white list.

## MongoWaitQueueFullException. The wait queue for acquiring a connection to server is full

In the vast majority of cases this is expected behavior related to provided a mongo client configuration or the way how a driver is used. This exception indicates that too many consumers are trying to use MongoDB at once. As soon as a number of acquiring connections at the same time has reached WaitQueueSize value, all next connection acquiring attempts will be postponed until any previous acquiring process will be finished. If it won't happen during WaitQueueTimeout, the driver will throw this exception.

## Unsupported LINQ or builder expressions

Each LINQ expression or builder configuration must be translated into an appropriate mongo query to be executed by a server. Unfortunately it's not always possible to do with a typed approach due 2 reasons:

1. A server doesn't have equvalent operators, methods or .dotnet types that have been configured in a LINQ expression.
2. A driver doesn't support a particular tranformation from LINQ expression or builder configuration into a server query. It may happen because the provided query is too complicated or because some feature has not been implemented yet by a driver.

In case if you see `Unsupported filter ..` or `Expression not supported ..` exception message, we recommend trying the following steps:

1. If you use a default LINQ2 provider, configure LINQ3 [provider]({{< relref "reference\driver\crud\linq3.md" >}}). This is the most modern LINQ provider that supports much more various cases that may be not implemented in LINQ2.
2. Try simplify your query as more as possible.
3. If the above steps don't resolve your issue, it's always possible to provide a query in a raw BsonDocument or string form. All mongo query definitions like FilterDefinition, ProjectionDefinition, PipelineDefinition and etc support implicit conversions from a raw form. That allows specyfying any query that is supported by a server. For example, the below filters are equvalent from a server point of view:

```csharp
    FilterDefinition<Entity> typedFilter = Builders<Entity>.Filter.Eq(e => e.A, 1);
    BsonDocument rawMQLfilter = BsonDocument.Parse("{ a : 1 }");
```

{{% note %}}
Pay attention that if you use a raw mongo query form, you also avoid using all applied configuration via bson attributes like `BsonElement` or static serialization with [BsonClassMap]({{< relref "reference\bson\mapping\index.md" >}}). So, the provided field naming should be represented in the same way as it's stored on the server.
{{% /note %}}
It's also possible to combine a raw and typed forms in the same query:

```csharp
FilterDefinition<Entity> filter = Builders<Entity>.Filter.And(Builders<Entity>.Filter.Eq(e => e.A, 1), BsonDocument.Parse("{ b : 2 }"));
```