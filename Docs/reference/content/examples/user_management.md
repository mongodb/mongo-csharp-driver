+++
date = "2015-05-29T15:36:56Z"
draft = false
title = "Managing Users"
[menu.main]
  parent = "Examples"
  identifier = "Managing Users"
  weight = 50
  pre = "<i class='fa'></i>"
+++

## How to Manage Users

While MongoDB supports many user management commands, the driver does not have any helpers for them because users are generally managed from the [MongoDB shell]({{< docsref "reference/mongo-shell/" >}}). However, it is still possible to manage users from the driver by using the [`RunCommand`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_RunCommand__1" >}}) or [`RunCommandAsync`]({{< apiref "M_MongoDB_Driver_IMongoDatabase_RunCommandAsync__1" >}}) methods.


## Listing Users

The [`usersInfo`]({{< docsref "reference/command/usersInfo/" >}}) command will provide information about users in a specific database.

```csharp
// get the database
var db = client.GetDatabase("products");

// construct the usersInfo command
var command = new BsonDocument("usersInfo", 1);
```
```csharp
// Run the command. If it fails, an exception will be thrown.
var result = db.RunCommand<BsonDocument>(command);
```
```csharp
// Run the command. If it fails, an exception will be thrown.
var result = await db.RunCommandAsync<BsonDocument>(command);
```

The `result` variable will contain a field called "users" which will contain all the users for the "products" database.


## Creating Users

The following example uses the [`createUser`]({{< docsref "reference/command/createUser/" >}}) command to add a user to a dataase.

```csharp
// get the database
var db = client.GetDatabase("products");

// Construct the write concern
var writeConcern = WriteConcern.WMajority
    .With(wTimeout: TimeSpan.FromMilliseconds(5000);

// Construct the createUser command.
var command = new BsonDocument
{
    { "createUser", "accountAdmin01" },
    { "pwd", "cleartext password" },
    { "customData", new BsonDocument("employeeId", 12345) },
    { "roles", new BsonArray
        {
            new BsonDocument
            {
                { "role", "clusterAdmin" },
                { "db", "admin" }   
            },
            new BsonDocument
            {
                { "role", "readAnyDatabase" },
                { "db", "admin" }   
            },
            "readWrite"
        }},
    { "writeConcern", writeConcern.ToBsonDocument() }
};
```
```csharp
// Run the command. If it fails, an exception will be thrown.
db.RunCommand<BsonDocument>(command);
```
```csharp
// Run the command. If it fails, an exception will be thrown.
await db.RunCommandAsync<BsonDocument>(command);
```

## Updating Users

The following example uses the [`updateUser`]({{< docsref "reference/command/updateUser/" >}}) command to update a user in a database.

```csharp
// get the database
var db = client.GetDatabase("products");

// Construct the updateUser command.
var command = new BsonDocument
{
    { "updateUser", "appClient01" },
    { "customData", new BsonDocument("employeeId", "0x3039") },
    { "roles", new BsonArray
        {
            new BsonDocument
            {
                { "role", "read" },
                { "db", "assets" }   
            },
        }}
};
```
```csharp
// Run the command. If it fails, an exception will be thrown.
db.RunCommand<BsonDocument>(command);
```
```csharp
// Run the command. If it fails, an exception will be thrown.
await db.RunCommandAsync<BsonDocument>(command);
```

## Dropping Users

The following example uses the [`dropUser`]({{< docsref "reference/command/dropUser/" >}}) command to drop a user from a database.

```csharp
// get the database
var db = client.GetDatabase("products");

// Construct the dropUser command.
var command = 
    @"{
        dropUser: ""accountAdmin01"",
        writeConcern: { w: ""majority"", wtimeout: 5000 }
    }";
```
```csharp
// Run the command. If it fails, an exception will be thrown.
db.RunCommand<BsonDocument>(command);
```
```csharp
// Run the command. If it fails, an exception will be thrown.
await db.RunCommandAsync<BsonDocument>(command);
```

{{% note %}}Even though we used a string here for the command, it could have been a BsonDocument like the other examples. Well-formed strings of valid JSON are interchangeable with BsonDocument.{{% /note %}}


## Other User Management Commands

There are a number of [other commands]({{< docsref "reference/command/nav-user-management/" >}}) that exist for managing users and each would be run in a similar fashion to how ones demonstrated above.