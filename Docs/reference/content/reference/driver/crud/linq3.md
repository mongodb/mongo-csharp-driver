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

We are in the process of rewriting our LINQ provider. The new LINQ provider is known as LINQ3. The current LINQ provider is known as LINQ2 (and LINQ1 is the now obsolete LINQ provider in the v1.x releases of the driver).

While we fully transition to the new LINQ provider the two LINQ providers will exist side by side, and LINQ2 will continue to be the default LINQ provider for the time being.

In version 2.14.0 of the driver we are making the new LINQ provider available in beta form. In order to use it you have to configure your client to use the new LINQ provider, as follows:

```csharp
var connectionString = "mongodb://localhost";
var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
clientSettings.LinqProvider = LinqProvider.V3;
var client = new MongoClient(clientSettings);
```

The LINQ provider is only configurable at the client level. All LINQ queries run with a particular client use the same LINQ provider.
