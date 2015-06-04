+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "What's New"
[menu.main]
  weight = 20
  identifier = "What's New"
  pre = "<i class='fa fa-star'></i>"
+++

## What's New in the MongoDB .NET 2.0 Driver

The 2.0 driver ships with a host of new features. The most notable are discussed below.


## Async

As has been requested for a while now, the driver now offers a full async stack. Since it uses Tasks, it is fully usable
with async and await. 

While we offer a mostly backwards-compatible sync API, it is calling into the async stack underneath. Until you are ready
to move to async, you should measure against the 1.x versions to ensure performance regressions don't enter your codebase.

All new applications should utilize the New API.


## New API

Because of our async nature, we have rebuilt our entire API. The new API is accessible via MongoClient.GetDatabase. 

- Interfaces are used ([`IMongoClient`]({{< apiref "T_MongoDB_Driver_IMongoClient" >}}), [`IMongoDatabase`]({{< apiref "T_MongoDB_Driver_IMongoDatabase" >}}), [`IMongoCollection<TDocument>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}})) to support easier testing.
- A fluent Find API is available with full support for expression trees including projections.

	``` csharp
	var names = await db.GetCollection<Person>("people")
		.Find(x => x.FirstName == "Jack")
		.SortBy(x => x.Age)
		.Project(x => x.FirstName + " " + x.LastName)
		.ToListAsync();
	```

- A fluent Aggregation API is available with mostly-full support for expression trees.

	``` csharp
	var totalAgeByLastName = await db.GetCollection<Person>("people")
		.Aggregate()
		.Match(x => x.FirstName == "Jack")
		.GroupBy(x => x.LastName, g => new { _id = g.Key, TotalAge = g.Sum(x => x.Age)})
		.ToListAsync();
	```

- Support for dynamic.

	``` csharp
	var person = new ExpandoObject();
	person.FirstName = "Jane";
	person.Age = 12;
	person.PetNames = new List<dynamic> { "Sherlock", "Watson" }
	await db.GetCollection<dynamic>("people").InsertOneAsync(person);
	```


## Experimental Features

We've also include some experimental features which are subject to change. These are both based on the Listener API.


### Logging

It is possible to see what is going on deep down in the driver by listening to core events. We've included a simple text logger as an example:
	
``` csharp
var settings = new MongoClientSettings
{
	ClusterConfigurator = cb =>
	{
		var textWriter = TextWriter.Synchronized(new StreamWriter("mylogfile.txt"));
		cb.AddListener(new LogListener(textWriter));
	}
};
```


### Performance Counters

Windows Performance Counters can be enabled to track statistics like average message size, number of connections in the pool, etc...

``` csharp
var settings = new MongoClientSettings
{
	ClusterConfigurator = cb =>
	{
		cb.UsePeformanceCounters("MyApplicationName");
	}
};
```