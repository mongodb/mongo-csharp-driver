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

We have implemented a new LINQ provider, which is known as LINQ3. The previous LINQ provider is known as LINQ2 (and LINQ1 is the now-obsolete LINQ provider in the v1.x releases of the driver).

While we fully transition to the new LINQ provider the two LINQ providers will exist side by side. Starting with 2.19.0, LINQ3 is the default LINQ provider.

LINQ3 is production-ready. It fixes many LINQ2 bugs and offers support for a variety of new aggregation pipeline features present in newer server versions. The [MongoDB Analyzer](https://www.mongodb.com/docs/mongodb-analyzer/current/) will provide tooltips indicating whether a particular query is supported in LINQ2, LINQ3, or both.

If you encounter a problem with LINQ3, you can switch back the LINQ2 provider by configuring your `MongoClient` to use the previous LINQ provider as follows:

```csharp
var connectionString = "mongodb://localhost";
var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
clientSettings.LinqProvider = LinqProvider.V2;
var client = new MongoClient(clientSettings);
```

The LINQ provider is only configurable at the `MongoClient` level. All LINQ queries run with a particular `MongoClient` use the same LINQ provider.

If you do encounter a query that works in LINQ2 but fails in LINQ3, please file a [CSHARP ticket](https://jira.mongodb.org/browse/CSHARP) with a self-contained reproduction of the problem so that we can investigate and fix the issue.
