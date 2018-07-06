+++
date = "2018-06-29T13:45:00Z"
draft = false
title = "Sessions and Transactions"
[menu.main]
  parent = "Reference Reading and Writing"
  weight = 30
  pre = "<i class='fa'></i>"
+++

## Sessions

A session is used to group together a series of operations that are related to each other and should be executed with the same session options. Sessions are also used for transactions.

New overloaded methods that take a session parameter have been added for all operation methods in the driver. You execute multiple operations in the same session by passing the same session value to each operation.

When you call an older operation method that does not take a session parameter, the driver will start an implicit session to execute that one operation and then immediately end the implicit session.

### StartSession and StartSessionAsync

```csharp
IClientSessionHandle StartSession(ClientSessionOptions options = null, CancellationToken cancellationToken = default(CancellationToken));
Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions options = null, CancellationToken cancellationToken = default(CancellationToken));
```

A session is started by calling the StartSession or StartSessionAsync methods of IMongoClient:

You must end a session when you no longer need it. You end a session by calling Dispose, which will happen automatically if you put the session inside a using statement. 

The recommended way of using a session is:

```csharp
var sessionOptions = new ClientSessionOptions { ... };
using (var session = client.StartSession(sessionOptions, cancellationToken))
{
    // execute some operations passing the session as an argument to each operation
}
```

or

```csharp
var sessionOptions = new ClientSessionOptions { ... };
using (var session = await client.StartSessionAsync(sessionOptions, cancellationToken))
{
    // execute some operations passing the session as an argument to each operation
}
```

A session is typically short lived. You start a session, execute some operations, and end the session.

### ClientSessionOptions

The ClientSessionOptions class is used to specify any desired options when calling StartSession.

```csharp
public class ClientSessionOptions
{
    public bool? CausalConsistency { get; set; }
    public TransactionOptions DefaultTransactionOptions { get; set; }
}
```

#### CausalConsistency

Set to true if you want all operations in a session to be causally consistent.

#### DefaultTransactionOptions

You can provide default transaction options to be used for any options that are not provided when StartTransaction is called.

### IClientSession properties and methods

The IClientSession interface defines the properties and methods available on a session.

```csharp
public interface IClientSession : IDisposable
{
    IMongoClient Client { get; }
    BsonDocument ClusterTime { get; }
    bool IsInTransaction { get; }
    BsonTimestamp OperationTime { get; }
    ClientSessionOptions Options { get; }

    void AdvanceClusterTime(BsonDocument newClusterTime);
    void AdvanceOperationTime(BsonTimestamp newOperationTime);

    // see also transaction related methods documented below
}
```
Note: a few members of IClientSession have been deliberately omitted from this documentation, either because they are rarely used or because they are for internal use only.

#### Client

The Client property returns a reference to the IMongoClient instance that was used to start this session.

#### ClusterTime

The ClusterTime property returns the highest cluster time that has been seen by this client. The value is an opaque BsonDocument containing a cluster time that has been returned by the server. While an application might never inspect the actual value, it might set this value (by calling AdvanceClusterTime) when initializing a causally consistent session.

### IsInTransaction

Specifies whether the session is currently in a transaction. A session is in a transaction after StartTransaction has been called and until either AbortTransaction or CommitTransaction has been called.

### OperationTime

The operation time returned by the server for the most recent operation in this session. Operation times are used by the driver to ensure causal consistency when the ClientSessionOptions specify that causal consistency is desired. While an application might never use the actual value, it might set this value (by calling AdvanceOperationTime) when initializing a causally consistent session.

### Options

Returns the options that were passed to StartSession.

### AdvanceClusterTime and AdvanceOperationTime

```csharp
void AdvanceClusterTime(BsonDocument newClusterTime);
void AdvanceOperationTime(BsonTimestamp newOperationTime);
```

Call these methods to advance the cluster and operation times when you want subsequent operations in this session to be causally consistent with operations that have executed outside of this session. Typically the values you pass to AdvanceClusterTime and AdvanceOperation time will come from the ClusterTime and OperationTime properties of some other session.

### Transaction methods

The methods to start, abort or commit a transaction are documented in the next section.

## Transactions

Transactions are started, committed or aborted using methods of IClientSession. A session can only execute one transaction at a time, but a session can execute more than one transaction as long as each transaction is committed or aborted before the next one is started.

### StartTransaction

```csharp
void StartTransaction(TransactionOptions transactionOptions = null);
```

Start a transaction by calling StartTransaction, optionally specifying options for the transaction.

Each transaction option can be specified at any of the following levels:

1. StartTransaction in the options parameter
2. StartSession in the defaultTransactionOptions parameter
3. Defaulted

You can specify different options at different levels. An option specified in StartTransaction overrides the same option specified in the defaultTransactionOptions passed to StartSession, which in turn overrides the default value.

#### TransactionOptions

```csharp
public class TransactionOptions
{
    public ReadConcern ReadConcern { get; };
    public ReadPreference ReadPreference { get; };
    public WriteConcern WriteConcern { get; };

    public TransactionOptions(
        Optional<ReadConcern> readConcern = default(Optional<ReadConcern>),
        Optional<ReadPreference> readPreference = default(Optional<ReadPreference>),
        Optional<WriteConcern> writeConcern = default(Optional<WriteConcern>));

    public TransactionOptions With(
        Optional<ReadConcern> readConcern = default(Optional<ReadConcern>),
        Optional<ReadPreference> readPreference = default(Optional<ReadPreference>),
        Optional<WriteConcern> writeConcern = default(Optional<WriteConcern>))
}
```

Create an instance of TransactionOptions either by calling the constructor with any desired optional arguments, or by calling the With method on an existing instance of TransactionOptions to create a new instance with some values changed.

##### ReadConcern

The ReadConcern used while in the transaction. All operations in the transaction use the ReadConcern specified when StartTransaction is called.

##### ReadPreference

The ReadPreference used while in the transaction. Currently, the ReadPreference for a transaction must be Primary.

##### WriteConcern

The WriteConcern used for this transaction. The WriteConcern only applies when committing the transaction, not to the individual operations executed while in the transaction.

### CommitTransaction and CommitTransactionAsync

```csharp
void CommitTransaction(CancellationToken cancellationToken = default(CancellationToken));
Task CommitTransactionAsync(CancellationToken cancellationToken = default(CancellationToken));
```

You must call CommitTransaction or CommitTransactionAsync in order for a transaction to be committed. If a session is ended while a transaction is in progress the transaction will be automatically aborted.

The commit is executed with the WriteConcern specified by the transaction options.

### AbortTransaction and AbortTransactionAsync

```csharp
void AbortTransaction(CancellationToken cancellationToken = default(CancellationToken));
Task AbortTransactionAsync(CancellationToken cancellationToken = default(CancellationToken));
```

Call AbortTransaction or AbortTransactionAsync to abort a transaction. Since any transaction in progress is automatically aborted when a session is ended, you can also implicitly abort an uncommitted transaction by simply ending the session.

In this example we rely on the implied transaction abort:

```csharp
using (var session = client.StartSession())
{
    session.StartTransaction();
    // execute operations using the session
    session.CommitTransaction(); // if an exception is thrown before reaching here the transaction will be implicitly aborted
}
```

When writing an async program you may want to avoid using the implied abort transaction that occurs when Dispose is called on a session with a transaction in progress, because Dispose is a blocking operation. To be fully async, even in the case where the transaction needs to be aborted, you might instead write:

```csharp
using (var session = await client.StartSessionAsync())
{
    try
    {
        // execute async operations using the session
    }
    catch
    {
        await session.AbortTransactionAsync(); // now Dispose on the session has nothing to do and won't block
        throw;
    }
    await session.CommitTransactionAsync();
}
```
