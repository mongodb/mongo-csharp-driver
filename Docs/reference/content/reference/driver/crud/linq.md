+++
date = "2015-08-25T15:36:56Z"
draft = false
title = "LINQ"
[menu.main]
  parent = "Reference Reading and Writing"
  identifier = "LINQ"
  weight = 15
  pre = "<i class='fa'></i>"
+++

## LINQ

The driver contains an implementation of [LINQ]({{< msdnref "bb397926" >}}) that targets the [aggregation framework]({{< docsref "aggregation" >}}). The aggregation framework holds a rich query language that maps very easily from a LINQ expression tree making it straightforward to understand the translation from a LINQ statement into an aggregation framework pipeline. To see a more complicated uses of LINQ from the driver, see the [AggregationSample]({{< srcref "MongoDB.Driver.Tests/Samples/AggregationSample.cs" >}}) source code.

For the rest of this page, we'll use the following class:

```csharp
class Person
{
	public string Name { get; set; }

	public int Age { get; set; }

	public IEnumerable<Pet> Pets { get; set; }

	public int[] FavoriteNumbers { get; set; }

	public HashSet<string> FavoriteNames { get; set; }

	public DateTime CreatedAtUtc { get; set; }
}

class Pet
{
	public string Name { get; set; }
}
```

### Queryable

Hooking into the LINQ provider requires getting access to an [`IQueryable`]({{< msdnref "bb351562" >}}) instance. The driver provides an [`AsQueryable`]({{< apiref "M_MongoDB_Driver_IMongoCollectionExtensions_AsQueryable" >}}) extension method on [`IMongoCollection`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}}).

```csharp
var collection = db.GetCollection<Person>("people");
var queryable = collection.AsQueryable();
```

Once you have an [`IQueryable`]({{< msdnref "bb351562" >}}) instance, you can begin to compose a query:

```csharp
var query = from p in collection.AsQueryable()
            where p.Age > 21
            select new { p.Name, p.Age };

// or, using method syntax

var query = collection.AsQueryable()
    .Where(p => p.Age > 21)
    .Select(p => new { p.Name, p.Age });
```

... which maps to the following aggregation framework pipeline:

```json
[
	{ $match: { Age: { $gt: 21 } } },
	{ $project: { Name: 1, Age: 1, _id: 0 } }
]
```


### Stages

We'll walk through the supported stages below:

#### $project

To generate a [`$project`]({{< docsref "reference/operator/aggregation/project/" >}}) stage, use the [`Select`]({{< msdnref "bb548743" >}}) method. To see the list of expressions supported in the [`Select`]({{< msdnref "bb548743" >}}) method, see [Aggregation Projections]({{< relref "reference\driver\expressions.md#aggregation-projections" >}}).

```csharp
var query = from p in collection.AsQueryable()
            select new { p.Name, p.Age };

// or

var query = collection.AsQueryable()
    .Select(p => new { p.Name, p.Age });
```
```json
[
	{ $project: { Name: 1, Age: 1, _id: 0 } }
]
```

{{% note %}}When projecting scalars, the driver will wrap the scalar into a document with a generated field name because MongoDB requires that output from an aggregation pipeline be documents.

```csharp
var query = from p in collection.AsQueryable()
            select p.Name;

var query = collection.AsQueryable()
    .Select(p => p.Name);
```
```json
[
	{ $project: { __fld0: "$Name", _id: 0 } }
]
```

The driver will know how to read the field out and transform the results properly.
{{% /note %}}

{{% note %}}[By default]({{< docsref "reference/operator/aggregation/project/#suppress-the-id-field" >}}), MongoDB will include the `_id` field in the output unless explicitly excluded. The driver will automatically add this exclusion when necessary.{{% /note %}}


#### $match

The [`Where`]({{< msdnref "bb535040" >}}) method is used to generate a [`$match`]({{< docsref "reference/operator/aggregation/match/" >}}) stage. To see the list of expressions supported inside a [`Where`]({{< msdnref "bb535040" >}}), see [Filters]({{< relref "reference\driver\expressions.md#filters" >}}).

```csharp
var query = from p in collection.AsQueryable()
            where p.Age > 21
            select p;

// or

var query = collection.AsQueryable()
    .Where(p => p.Age > 21);
```
```json
[
	{ $match: { Age: { $gt: 21 } } }
]
```


#### $redact

The [`$redact`]({{< docsref "reference/operator/aggregation/redact/" >}}) stage is not currently supported using LINQ.

#### $limit

The [`Take`]({{< msdnref "bb300906" >}}) method is used to generate a [`$limit`]({{< docsref "reference/operator/aggregation/limit/" >}}) stage.

```csharp
var query = collection.AsQueryable().Take(10);
```
```json
[
	{ $limit: 10 }
]
```

#### $skip

The [`Skip`]({{< msdnref "bb357513" >}}) method is used to generate a [`$skip`]({{< docsref "reference/operator/aggregation/skip/" >}}) stage.

```csharp
var query = collection.AsQueryable().Skip(10);
```
```json
[
	{ $skip: 10 }
]
```

#### $unwind

The [`SelectMany`]({{< msdnref "bb548748" >}}) method is used to generate an [`$unwind`]({{< docsref "reference/operator/aggregation/unwind/" >}}) stage. In addition, because of how [`$unwind`]({{< docsref "reference/operator/aggregation/unwind/" >}}) works, a [`$project`]({{< docsref "reference/operator/aggregation/project/" >}}) stage will also be rendered. To see the list of expressions supported in the [`SelectMany`]({{< msdnref "bb548748" >}}) method, see [Aggregation Projections]({{< relref "reference\driver\expressions.md#aggregation-projections" >}}).

```csharp
var query = from p in collection.AsQueryable()
            from pet in p.Pets
            select pet;

// or

var query = collection.AsQueryable()
    .SelectMany(p => p.Pets);
```
```json
[
	{ $unwind: "$Pets" }
	{ $project: { Pets: 1, _id: 0 } }
]
```
---
```csharp
var query = from p in collection.AsQueryable()
            from pet in p.Pets
            select new { Name = pet.Name, Age = p.Age};

// or

var query = collection.AsQueryable()
    .SelectMany(p => p.Pets, (p, pet) => new { Name = pet.Name, Age = p.Age});
```
```json
[
	{ $unwind: "$Pets" }
	{ $project: { Name: "$Pets.Name", Age: "$Age", _id: 0 } }
]
```

#### $group

The [`GroupBy`]({{< msdnref "bb534492" >}}) method is used to generate a [`$group`]({{< docsref "reference/operator/aggregation/group/" >}}) stage. In general, the [`GroupBy`]({{< msdnref "bb534492" >}}) method will be followed by the [`Select`]({{< msdnref "bb548743" >}}) containing the accumulators although that isn't required. To see the list of supported accumulators, see [Accumulators]({{< relref "reference\driver\expressions.md#accumulators" >}}).

```csharp
var query = from p in collection.AsQueryable()
            group p by p.Name into g
            select new { Name = g.Key, Count = g.Count() };

//or

var query = collection.AsQueryable()
    .GroupBy(p => p.Name)
    .Select(g => new { Name = g.Key, Count = g.Count() });
```
```json
[
	{ $group: { _id: "$Name", __agg0: { $sum: 1 } } },
	{ $project: { Name: "$_id", Count: "$__agg0", _id: 0 } }
]
```
---
```csharp
var query = collection.AsQueryable()
    .GroupBy(p => p.Name, (k, s) => new { Name = k, Count = s.Count()});
```
```json
[
	{ $group: { _id: "$Name", Count: { $sum: 1 } } },
]
```

#### $sort

The [`OrderBy`]({{< msdnref "bb549264" >}}) and [`ThenBy`]({{< msdnref "bb535112" >}}) methods are used to generate a [`$sort`]({{< docsref "reference/operator/aggregation/sort/" >}}) stage.

```csharp
var query = from p in collection.AsQueryable()
            orderby p.Name, p.Age descending
            select p;

//or

var query = collection.AsQueryable()
    .OrderBy(p => p.Name)
    .ThenByDescending(p => p.Age);
```
```json
[
	{ $sort: { Name: 1, Age: -1 } },
]
```

#### $geoNear

The [`$geoNear`]({{< docsref "reference/operator/aggregation/geoNear/" >}}) stage is not currently supported using LINQ.

#### $out

The [`$out`]({{< docsref "reference/operator/aggregation/out/" >}}) stage is not currently supported using LINQ.


#### $lookup

The [`GroupJoin`]({{< msdnref "bb549264" >}}) method is used to generate a [`$lookup`]({{< docsref "reference/operator/aggregation/lookup/" >}}) stage.

This operator can take on many forms, many of which are not supported by MongoDB. Therefore, only 2 forms are supported.

First, you may project into most anything as long as it is supported by the `$project` operator and you do not project the original collection variable. Below is an example of a valid query:

```csharp
var query = from p in collection.AsQueryable()
            join o in otherCollection on p.Name equals o.Key into joined
            select new { p.Name, AgeSum: joined.Sum(x => x.Age) };
```
```json
[
    { $lookup: { from: "other_collection", localField: 'Name', foreignField: 'Key', as: 'joined' } }",
    { $project: { Name: "$Name", AgeSum: { $sum: "$joined.Age" }, _id: 0 } }
]
```

Second, you may project into a type with two constructor parameters, the first being the collection variable and the second being the joined variable. Below is an example of this:

```csharp
var query = from p in collection.AsQueryable()
            join o in otherCollection on p.Name equals o.Key into joined
            select new { p, joined };
```
```json
[
    { $lookup: { from: "other_collection", localField: 'Name', foreignField: 'Key', as: 'joined' } }"
]
```

.. note::
   An anonymous type, as above, has a constructor with two parameters as required.

Sometimes, the compiler will also generate this two-parameter anonymous type transparently. Below is an example of this with a custom projection:

```csharp
var query = from p in collection.AsQueryable()
            join o in otherCollection on p.Name equals o.Key into joined
            from sub_o in joined.DefaultIfEmpty()
            select new { p.Name, sub_o.Age };
```
```json
[
    { $lookup: { from: "other_collection", localField: 'Name', foreignField: 'Key', as: 'joined' } }",
    { $unwind: "$joined" },
    { $project: { Name: "$Name", Age: "$joined.Age", _id: 0 }}
]
```


### Supported Methods

The method examples are shown in isolation, but they can be used and combined with all the other methods as well. You can view the tests for each of these methods in the [MongoQueryableTests]({{< srcref "MongoDB.Driver.Tests/Linq/MongoQueryableTests.cs" >}}).


#### Any

All forms of [`Any`]({{< msdnref "system.linq.queryable.any" >}}) are supported.

```csharp
var result = collection.AsQueryable().Any();
```
```json
[
    { $limit: 1 }
]
```
---
```csharp
var result = collection.AsQueryable().Any(p => p.Age > 21);
```
```json
[
    { $match: { Age: { $gt: 21 } },
    { $limit: 1 }
]
```

{{% note %}} `Any` has a boolean return type. Since MongoDB doesn't support this, the driver will pull back at most 1 document. If one document was retrieved, then the result is true. Otherwise, it's false.{{% /note %}}

#### Average

All forms of [`Average`]({{< msdnref "system.linq.queryable.average" >}}) are supported.

```csharp
var result = collection.AsQueryable().Average(p => p.Age);

// or

var result = collection.AsQueryable().Select(p => p.Age).Average();
```
```json
[
    { $group: { _id: 1, __result: { $avg: "$Age" } } }
]
```

#### Count and LongCount

All forms of [`Count`]({{< msdnref "system.linq.queryable.count" >}}) and [`LongCount`]({{< msdnref "system.linq.queryable.longcount" >}}) are supported.

```csharp
var result = collection.AsQueryable().Count();

// or

var result = collection.AsQueryable().LongCount();
```
```json
[
    { $group: { _id: 1, __result: { $sum: 1 } } }
]
```
---
```csharp
var result = collection.AsQueryable().Count(p => p.Age > 21);

// or

var result = collection.AsQueryable().LongCount(p => p.Age > 21);
```
```json
[
    { $match : { Age { $gt: 21 } } },
    { $group: { _id: 1, __result: { $sum: 1 } } }
]
```


#### Distinct

[`Distinct`]({{< msdnref "bb348456" >}}) without an equality comparer is supported.

```csharp
var query = collection.AsQueryable().Distinct();
```
```json
[
    { $group: { _id: "$$ROOT" } }
]
```

Using a distinct in isolation as shown above is non-sensical. Since each document in a collection contains a unique _id field, then there will be as many groups as their are documents. To properly use distinct, it should follow some form of a projection like $project or $group.

```csharp
var query = collection.AsQueryable()
    .Select(p => new { p.Name, p.Age })
    .Distinct();
```
```json
[
    { $group: { _id: { Name: "$Name", Age: "$Age" } } }
]
```


#### First and FirstOrDefault

All forms of [`First`]({{< msdnref "system.linq.queryable.first" >}}) and [`FirstOrDefault`]({{< msdnref "system.linq.queryable.firstordefault" >}}) are supported.

```csharp
var result = collection.AsQueryable().First();

// or

var result = collection.AsQueryable().FirstOrDefault();
```
```json
[
    { $limit: 1 }
]
```
---
```csharp
var result = collection.AsQueryable().First(p => p.Age > 21);

// or

var result = collection.AsQueryable().FirstOrDefault(p => p.Age > 21);
```
```json
[
    { $match : { Age { $gt: 21 } } },
    { $limit: 1 }
]
```


#### GroupBy

See [`$group`]({{< relref "#group" >}}).


#### GroupJoin

See [`$lookup`]({{< relref "#lookup" >}}).


#### Max

All forms of [`Max`]({{< msdnref "system.linq.queryable.max" >}}) are supported.

```csharp
var result = collection.AsQueryable().Max(p => p.Age);

// or

var result = collection.AsQueryable().Select(p => p.Age).Max();
```
```json
[
    { $group: { _id: 1, __result: { $max: "$Age" } } }
]
```


#### Sum

All forms of [`Sum`]({{< msdnref "system.linq.queryable.Sum" >}}) are supported.

```csharp
var result = collection.AsQueryable().Sum(p => p.Age);

// or

var result = collection.AsQueryable().Select(p => p.Age).Sum();
```
```json
[
    { $group: { _id: 1, __result: { $Sum: "$Age" } } }
]
```


#### OfType

All forms of [`OfType`]({{< msdnref "system.linq.queryable.oftype" >}}) are supported.

```csharp
// assuSumg Customer inherits from Person
var result = collection.AsQueryable().OfType<Customer>();
```
```json
[
    { $match: { _t: "Customer" } }
]
```

{{% note %}}Based on configuration, the discriminator name `_t` may be different as well as the value `"Customer"`.{{% /note %}}


#### OrderBy, OrderByDescending, ThenBy, and ThenByDescending

See [`$sort`]({{< relref "#sort" >}}).


#### Select

See [`$project`]({{< relref "#project" >}}).


#### SelectMany

See [`$unwind`]({{< relref "#unwind" >}}).



#### Single and SingleOrDefault

All forms of [`Single`]({{< msdnref "system.linq.queryable.single" >}}) and [`SingleOrDefault`]({{< msdnref "system.linq.queryable.singleordefault" >}}) are supported.

```csharp
var result = collection.AsQueryable().Single();

// or

var result = collection.AsQueryable().SingleOrDefault();
```
```json
[
    { $limit: 2 }
]
```
---
```csharp
var result = collection.AsQueryable().Single(p => p.Age > 21);

// or

var result = collection.AsQueryable().SingleOrDefault(p => p.Age > 21);
```
```json
[
    { $match : { Age { $gt: 21 } } },
    { $limit: 2 }
]
```

{{% note %}}The limit here is 2 because the behavior of `Single` is to throw when there is more than 1 result. Therefore, we pull back at most 2 documents and throw when 2 documents were retrieved. If this is not the behavior you wish, then `First` is the other choice.{{% /note %}}


#### Skip

See [`$skip`]({{< relref "#skip" >}}).


#### Sum

All forms of [`Sum`]({{< msdnref "system.linq.queryable.sum" >}}) are supported.

```csharp
var result = collection.AsQueryable().Sum(p => p.Age);

// or

var result = collection.AsQueryable().Select(p => p.Age).Sum();
```
```json
[
    { $group: { _id: 1, __result: { $sum: "$Age" } } }
]
```


#### Take

See [`$limit`]({{< relref "#limit" >}}).


#### Where

See [`$match`]({{< relref "#match" >}}).