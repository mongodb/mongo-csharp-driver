+++
date = "2021-10-11T00:00:00Z"
draft = false
title = "LINQ3"
[menu.main]
  parent = "Reference Reading and Writing"
  identifier = "LINQ3"
  weight = 16
  pre = "<i class='fa'></i>"
+++

## LINQ3

We have implemented a new LINQ provider, which is known as LINQ3. The current LINQ provider is known as LINQ2 (and LINQ1 is the now-obsolete LINQ provider in the v1.x releases of the driver).

While we fully transition to the new LINQ provider the two LINQ providers will exist side by side. LINQ2 will continue to be the default LINQ provider for the time being.

LINQ3 is production-ready. It fixes many LINQ2 bugs and offers support for a variety of new aggregation pipeline features present in newer server versions. We encourage all users to switch to LINQ3 and report any issues encountered. The [MongoDB Analyzer](https://www.mongodb.com/docs/mongodb-analyzer/current/) will provide tooltips indicating whether a particular query is supported in LINQ2, LINQ3, or both.

You can opt into the new LINQ3 provider by configuring your `MongoClient` to use the new LINQ provider as follows:

```csharp
var connectionString = "mongodb://localhost";
var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
clientSettings.LinqProvider = LinqProvider.V3;
var client = new MongoClient(clientSettings);
```

The LINQ provider is only configurable at the `MongoClient` level. All LINQ queries run with a particular `MongoClient` use the same LINQ provider.
