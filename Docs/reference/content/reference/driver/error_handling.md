+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Error Handling"
[menu.main]
  parent = "Driver"
  identifier = "Error Handling"
  weight = 60
  pre = "<i class='fa'></i>"
+++

## Error Handling

Error handling using the .NET driver is a requirement for a fault-tolerant system. While MongoDB supports [automatic replica set failover]({{< docsref "core/replica-set-high-availability/" >}}) and the driver supports multiple [mongos]({{< docsref "reference/program/mongos/" >}})'s, this doesn't make a program immune to errors.

Almost all errors will occur while attempting to perform an operation on the server. In other words, you will not receive an error when constructing a [`MongoClient`]({{< apiref "T_MongoDB_Driver_MongoClient" >}}), getting a database, or getting a collection. This is because we are connecting to servers in the background and continually trying to reconnect when a problem occurs. Only when you attempt to perform an operation do those errors become apparent.

There are a few types of errors you will see.

## Server Selection Errors

Even when some servers are available, it might not be possible to satisfy a request. For example, using [tag sets]({{< docsref "core/read-preference/#tag-sets" >}}) in a read preference when no server exists with those tags or attempting to write to a [replica set]({{< docsref "core/replication-introduction/" >}}) when the Primary is unavailable. Both of these would result in a [`TimeoutException`]({{< msdnref "system.timeoutexception" >}}). Below is an example exception (formatted for readability) when attempting to insert into a replica set without a primary.

```bash
System.TimeoutException: A timeout occured after 30000ms selecting a server using 
CompositeServerSelector{ 
    Selectors = 
        WritableServerSelector, 
        LatencyLimitingServerSelector{ AllowedLatencyRange = 00:00:00.0150000 } 
}. 

Client view of cluster state is 
{ 
    ClusterId : "1", 
    Type : "ReplicaSet", 
    State : "Connected", 
    Servers : [
        { 
            ServerId: "{ ClusterId : 1, EndPoint : "Unspecified/clover:30000" }", 
            EndPoint: "Unspecified/clover:30000", 
            State: "Disconnected", 
            Type: "Unknown" 
        }, 
        { 
            ServerId: "{ ClusterId : 1, EndPoint : "Unspecified/clover:30001" }", 
            EndPoint: "Unspecified/clover:30001", 
            State: "Connected", 
            Type: "ReplicaSetSecondary", 
            WireVersionRange: "[0, 3]" 
        }, 
        { 
            ServerId: "{ ClusterId : 1, EndPoint : "Unspecified/clover:30002" }", 
            EndPoint: "Unspecified/clover:30002", 
            State: "Disconnected", 
            Type: "Unknown"
        }, 
    ] 
}.
```

Inspecting this error will tell you that we were attempting to find a writable server. The servers that are known are `clover:30000`, `clover:30001`, and `clover:30002`. Of these, `clover:30000` and `clover:30002` are disconnected and `clover:30001` is connected and a secondary. By default, we wait for 30 seconds to fulfill the request. During this time, we are trying to connect to `clover:30000` and `clover:30002` in hopes that one of them becomes available and and takes the role of primary. In this case, we timed out waiting.

{{% note %}}Even though the above scenario prevents any writes from occuring, reads can still be sent to `clover:30001` as long as the read preference is `Secondary`, `SecondaryPreferred`, or `Nearest`.{{% /note %}}


## Connection Errors

A server may be unavailable for a variety of reasons. Each of these reasons will manifest themselves as a [`TimeoutException`]({{< msdnref "system.timeoutexception" >}}). This is because, over time, it is entirely possible for the state of the servers to change in such a way
that a matching server becomes available.

{{% note class="important" %}}There is nothing that can be done at runtime by your application to resolve these problems. The best thing to do is to catch them, log them, and raise a notification so that someone in your ops team can handle them.{{% /note %}}

{{% note %}}
If you are having trouble discovering why the driver can't connect, [enabling network tracing]({{< msdnref "ty48b824" >}}) on the `System.Net.Sockets` source may help discover the problem.{{% /note %}} 


### Server is unavailable

When a server is not listening at the location specified, the driver can't connect to it.

```bash
System.TimeoutException: A timeout occured after 30000ms selecting a server using
CompositeServerSelector{ 
    Selectors = 
        WritableServerSelector, 
        LatencyLimitingServerSelector{ AllowedLatencyRange = 00:00:00.0150000 } 
}. 

Client view of cluster state is 
{ 
    ClusterId : "1", 
    Type : "Unknown", 
    State : "Disconnected", 
    Servers : [
        { 
            ServerId: "{ ClusterId : 1, EndPoint : "Unspecified/localhost:27017" }", 
            EndPoint: "Unspecified/localhost:27017", 
            State: "Disconnected", 
            Type: "Unknown", 
            HeartbeatException: "MongoDB.Driver.MongoConnectionException: An exception occurred while opening a connection to the server. 
---> System.Net.Sockets.SocketException: No connection could be made because the target machine actively refused it 127.0.0.1:27017
   at System.Net.Sockets.Socket.EndConnect(IAsyncResult asyncResult).
   ... snip ..."
        }
    ]
}
``` 

We see that we are attempting to connect to `localhost:27017`, but "No connection could be made because the target machine actively refused it".

Fixing this problem either involves starting a server at the specified location or restarting your application with the correct host and port.


### DNS problems

When DNS is misconfigured, or the hostname provided is not registered, resolution from hostname to IP address may fail. 

```bash
System.TimeoutException: A timeout occured after 30000ms selecting a server using 
CompositeServerSelector{ 
    Selectors = 
        WritableServerSelector, 
        LatencyLimitingServerSelector{ AllowedLatencyRange = 00:00:00.0150000 } 
}. 

Client view of cluster state is 
{ 
    ClusterId : "1", 
    Type : "Unknown", 
    State : "Disconnected", 
    Servers : [
        { 
            ServerId: "{ ClusterId : 1, EndPoint : "Unspecified/idonotexist:27017" }", 
            EndPoint: "Unspecified/idonotexist:27017", 
            State: "Disconnected", 
            Type: "Unknown", 
            HeartbeatException: "MongoDB.Driver.MongoConnectionException: An exception occurred while opening a connection to the server. 
---> System.Net.Sockets.SocketException: No such host is known
   at System.Net.Sockets.Socket.EndConnect(IAsyncResult asyncResult)
   ... snip ..."
        }
    ]
}
```

We see that we are attempting to connect to `idonotexist:27017`, but "No such host is known".

Fixing this problem either involves bringing up a server at the specified location, fixing DNS to resolve the host correctly, or modifing your hosts file.


#### Replica Set Misconfiguration

DNS problems might be seen when a replica set is misconfigured. It is imperative that the hosts in your replica set configuration be DNS resolvable from the client. Just because the replica set members can talk to one another does not mean that your application servers can also talk to the replica set members. 

{{% note class="warning" %}}Even when providing a seed list with resolvable host names, if the replica set configuration uses unresolvable host names, the driver will fail to connect.{{% /note %}} 


### Taking too long to respond

When the latency between the driver and the server is too great, the driver may give up.

```bash
System.TimeoutException: A timeout occured after 30000ms selecting a server using
CompositeServerSelector{ 
    Selectors = 
        WritableServerSelector, 
        LatencyLimitingServerSelector{ AllowedLatencyRange = 00:00:00.0150000 } 
}.

Client view of cluster state is 
{ 
    ClusterId : "1", 
    Type : "Unknown", 
    State : "Disconnected", 
    Servers : [
        {
            ServerId: "{ ClusterId : 1, EndPoint : "Unspecified/somefaroffplace:27017" }", 
            EndPoint: "Unspecified/somefaroffplace:27017", 
            State: "Disconnected", 
            Type: "Unknown",
            HeartbeatException: "MongoDB.Driver.MongoConnectionException: An exception occurred while opening a connection to the server. 
---> System.TimeoutException: Timed out connecting to Unspecified/somefaroffplace:27017. Timeout was 00:00:30.
   at MongoDB.Driver.Core.Connections.TcpStreamFactory.<ConnectAsync>d__7.MoveNext()
   ... snip ...
        }
    ]
}
```

We see that attempting to connect to `somefaroffplace:27017` failed because we "Timed out connecting to Unspecified/somefaroffplace:27017. Timeout was 00:00:30."

The default connection timeout is 30 seconds and can be changed using the [`MongoClientSettings.ConnectTimeout`]({{< apiref "P_MongoDB_Driver_MongoClientSettings_ConnectTimeout" >}}) property or the `connectTimeout` option on a [connection string]({{< docsref "reference/connection-string/" >}}).


### Authentication Errors

When the credentials or the authentication mechanism is incorrect, the application will fail to connect.

```bash
System.TimeoutException: A timeout occured after 30000ms selecting a server using
CompositeServerSelector{ 
    Selectors = 
        WritableServerSelector, 
        LatencyLimitingServerSelector{ AllowedLatencyRange = 00:00:00.0150000 } 
}. 
        
Client view of cluster state is 
{ 
    ClusterId : "1", 
    Type : "Unknown", 
    State : "Disconnected", 
    Servers : [
        {
            ServerId: "{ ClusterId : 1, EndPoint : "Unspecified/localhost:27017" }", 
            EndPoint: "Unspecified/localhost:27017",
            State: "Disconnected", 
            Type: "Unknown", 
            HeartbeatException: "MongoDB.Driver.MongoConnectionException: An exception occurred while opening a connection to the server. 
 ---> MongoDB.Driver.MongoAuthenticationException: Unable to authenticate using sasl protocol mechanism SCRAM-SHA-1. 
 ---> MongoDB.Driver.MongoCommandException: Command saslStart failed: Authentication failed..
   ... snip ...
        }
    ]
}
```

We see that attempting to connect to `localhost:27017` failed because the driver was "Unable to authenticate using sasl protocol mechanism SCRAM-SHA-1." In place of SCRAM-SHA-1 could be any other authentication protocol supported by MongoDB.

Fixing this problem either involves adding the specified user to the server or restarting the application with the correct credentials and mechanisms.


## Operation Errors

After successfully selecting a server to run an operation against, errors are still possible. Unlike [connection errors]({{< relref "#connection-errors" >}}), it is sometimes possible to take action at runtime.

{{% note %}}Most of the exceptions that are thrown from an operation inherit from [`MongoException`]({{< apiref "T_MongoDB_Driver_MongoException" >}}). In many cases, they also inherit from [`MongoServerException`]({{< apiref "T_MongoDB_Driver_MongoServerException" >}}). The server exception contains a [`ConnectionId`]({{< apiref "P_MongoDB_Driver_MongoServerException_ConnectionId" >}}) which can be used to tie the operation back to a specific server and a specific connection on that server. It is then possible correllate the error you are seeing in your application with an error in the server logs.{{% /note %}}

### Connection Errors

A server may go down after it was selected, but before the operation was executed. These will always manifest as a [`MongoConnectionException`]({{< apiref "T_MongoDB_Driver_MongoConnectionException" >}}). Inspecting the inner exception will provide the actual details of the error.

```bash
MongoDB.Driver.MongoConnectionException: An exception occurred while receiving a message from the server. 
---> System.IO.IOException: Unable to read data from the transport connection: Anestablished connection was aborted by the software in your host machine. 
---> System.Net.Sockets.SocketException: An established connection was aborted by the software in your host machine at System.Net.Sockets.Socket.BeginReceive(Byte[] buffer, Int32 offset, Int32 size, SocketFlags socketFlags, AsyncCallback callback, Object state)
   at System.Net.Sockets.NetworkStream.BeginRead(Byte[] buffer, Int32 offset, Int32 size, AsyncCallback callback, Object state)
   --- End of inner exception stack trace ---
... snip ...
```

We see from this exception that a transport connection that was successfully open at one point has been aborted.

There are too many forms of this type of exception to enumerate. In general, it is not safe to retry operations that threw a [`MongoConnectionException`]({{< apiref "T_MongoDB_Driver_MongoConnectionException" >}}) unless the operation was idempotent. Simply getting an exception of this type doesn't give any insight into whether the operations was received by the server or what happened in the server if it was received.


### Write Exceptions

When performing a write, it is possible to receive a [`MongoWriteException`]({{< apiref "T_MongoDB_Driver_MongoWriteException" >}}). This exception has two important properties, [`WriteError`]({{< apiref "P_MongoDB_Driver_MongoWriteException_WriteError" >}}) and [`WriteConcernError`]({{< apiref "P_MongoDB_Driver_MongoWriteException_WriteConcernError" >}}).

#### Write Error

A write error means that there was an error applying the write. The cause could be many different things. The [`WriteError`]({{< apiref "T_MongoDB_Driver_WriteError" >}}) contains a number of properties which may help in the diagnosis of the problem. The [`Code`]({{< apiref "P_MongoDB_Driver_WriteError_Code" >}}) property will indicate specifically what went wrong. For general categories of errors, the driver also provides a helpful [`Category`]({{< apiref "P_MongoDB_Driver_WriteError_Category." >}}) property which classifies certain codes. 

```bash
MongoDB.Driver.MongoWriteException: A write operation resulted in an error. E11000 duplicate key error index: test.people.$_id_ dup key: { : 0 } 
---> MonoDB.Driver.MongoBulkWriteException`1[MongoDB.Bson.BsonDocument]: A bulk write oeration resulted in one or more errors. E11000 duplicate key error index: test.people.$_id_ dup key: { : 0 }
   at MongoDB.Driver.MongoCollectionImpl`1.<BulkWriteAsync>d__11.MoveNext() in :\projects\mongo-csharp-driver\src\MongoDB.Driver\MongoCollectionImpl.cs:line 16
```

We see from this exception that we've attempted to insert a document with a duplicate _id field. In this case, the write error would contain the category [`DuplicateKeyError`]({{< apiref "T_MongoDB_Driver_ServerErrorCategory" >}}). 

#### Write Concern Error

A [write concern]({{< docsref "core/write-concern/" >}}) error indicates that the server was unable to guarantee the write operation to the level specified. See the [server's documentation]({{< docsref "core/write-concern/" >}}) for more information.

### Bulk Write Exceptions

A [`MongoBulkWriteException`]({{< apiref "T_MongoDB_Driver_MongoBulkWriteException_1" >}}) will occur when using the [`InsertManyAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_InsertManyAsync" >}}) or [`BulkWriteAsync`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_BulkWriteAsync" >}}). This exception is just a rollup of a bunch of individual [write errors]({{< relref "#write-error" >}}). It also includes a [write concern error]({{< relref "#write-concern-error" >}}) and a [`Result`]({{< apiref "P_MongoDB_Driver_MongoBulkWriteException_1_Result" >}}) property.

```bash
MongoDB.Driver.MongoBulkWriteException`1[MongoDB.Bson.BsonDocument]: A bulk write operation resulted in one or more errors. 
  E11000 duplicate key error index: test.people.$_id_ dup key: { : 0 }
   at MongoDB.Driver.MongoCollectionImpl`1.<BulkWriteAsync>d__11.MoveNext() in c :\projects\mongo-csharp-driver\src\MongoDB.Driver\MongoCollectionImpl.cs:line 166
```

Above, we see that a duplicate key exception occured. In this case, two writes existed in the batch. Inspected the [`WriteErrors`]({{< apiref "P_MongoDB_Driver_MongoBulkWriteException_WriteErrors" >}}) property would allow the identification of which write failed.

#### Ordered Writes

Bulk writes are allowed to be processed either in order or without order. When processing in order, the first error that occurs will stop the processing of all subsequent writes. In this case, all of the unprocessed writes will be available via the [`UnprocessedRequests`]({{< apiref "P_MongoDB_Driver_MongoBulkWriteException_1_UnprocessedRequests" >}}) property.
