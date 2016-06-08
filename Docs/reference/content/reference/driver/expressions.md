+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Expressions"
[menu.main]
  parent = "Definitions and Builders"
  identifier = "Expressions"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Expressions

Many methods in the driver accept expressions as an argument. 

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

	public int PermissionFlags { get; set; }
}

class Pet
{
	public string Name { get; set; }
}
```

## Filters

We'll walk through the supported expressions below. The [tests]({{< srcref "MongoDB.Driver.Tests/Linq/Translators/PredicateTranslatorTests.cs" >}}) are also a good reference.

### Comparison

See the [MongoDB documentation]({{< docsref "reference/operator/query/#comparison" >}}) for more information on each operator.

#### $eq

```csharp
Find(p => p.Name == "Jack")
```

becomes:

```json
{ Name: 'Jack' }
```

{{% note %}}This is the short form for equality. Depending on context, this might generate `{ Name: { $eq: 'Jack' } }`.{{% /note %}}

#### $gt

```csharp
Find(p => p.Age > 10);
```
```json
{ Age: { $gt: 10 } }
```

#### $gte

```csharp
Find(p => p.Age >= 10);
```
```json
{ Age: { $gte: 10 } }
```

#### $lt

```csharp
Find(p => p.Age < 10);
```
```json
{ Age: { $lt: 10 } }
```

#### $lte

```csharp
Find(p => p.Age <= 10);
```
```json
{ Age: { $lte: 10 } }
```

#### $ne

```csharp
Find(p => p.Age != 10);
```
```json
{ Age: { $ne: 10 } }
```

#### $in

```csharp
int[] localAges = new [] { 10, 20, 30 };
Find(p => localAges.Contains(p.Age));
```
```json
{ Age: { $in: [10, 20, 30] } }
```

```csharp
int[] localNames = new [] { "Fluffy", "Scruffy" };
Find(p => p.Pets.Any(i => localNames.Contains(i.Name));
```
```json
{ "Pets.Name": { $in: ["Fluffy", "Scruffy"] } }
```

```csharp
int[] localNumbers = new [] { 30, 40 };
Find(p => localNumbers.Any(i => p.FavoriteNumbers.Contains(i));
```
```json
{ FavoriteNumbers: { $in: [30, 40] } } 
```

#### $nin

```csharp
int[] localAges = new [] { 10, 20, 30 };
Find(p => !localAges.Contains(p.Age));
```
```json
{ Age: { $nin: [10, 20, 30] } }
```

#### $bitsAllClear

```csharp
Find(p => (p.PermissionFlags & 7) == 0);
```
```json
{ PermissionFlags: { $bitsAllClear: 7 } }
```

#### $bitsAllSet

```csharp
Find(p => (p.PermissionFlags & 7) == 7);
```
```json
{ PermissionFlags: { $bitsAllSet: 7 } }
```

#### $bitsAnyClear

```csharp
Find(p => (p.PermissionFlags & 7) != 7);
```
```json
{ PermissionFlags: { $bitsAnyClear: 7 } }
```

#### $bitsAnySet

```csharp
Find(p => (p.PermissionFlags & 7) != 0);
```
```json
{ PermissionFlags: { $bitsAnySet: 7 } }
```

### Logical

See the [MongoDB documentation]({{< docsref "reference/operator/query/#logical" >}}) for more information on each operator.


#### $or

```csharp
Find(p => p.Name == "Jack" || p.Age == 10);
```
```json
{ $or: [{ Name: 'Jack' }, { Age: 10 }] }
```

#### $and

```csharp
Find(p => p.Name == "Jack" && p.Age < 40);
```
```json
{ Name: 'Jack', Age: { $lt: 40 } }
```
---
```csharp
Find(p => p.Age > 30 && p.Age < 40);
```
```json
{ Age: { $gt: 30, $lt: 40 }
```
---
```csharp
Find(p => p.Name != "Jack" && p.Name != "Jim");
```
```json
{ $and: [{ Name: { $ne: 'Jack' } }, { Name: { $ne: 'Jim' } }] }
```

#### $not

```csharp
// no example yet
```

#### $nor

```csharp
// no example yet
```


### Element

See the [MongoDB documentation]({{< docsref "reference/operator/query/#element" >}}) for more information on each operator.

#### $exists

```csharp
// no example yet
```

#### $type

```csharp
// no example yet
```

### Evaluation

See the [MongoDB documentation]({{< docsref "reference/operator/query/#evaluation" >}}) for more information on each operator.

#### $mod

```csharp
// no example yet
```

#### $regex

```csharp
// no example yet
```

#### $text

```csharp
// no example yet
```

#### $where

```csharp
// no example yet
```


### Geospatial

See the [MongoDB documentation]({{< docsref "reference/operator/query/#geospatial" >}}) for more information on each operator.

#### $geoWithin

```csharp
// no example yet
```

#### $geoIntersects

```csharp
// no example yet
```

#### $near

```csharp
// no example yet
```

#### $nearSphere

```csharp
// no example yet
```

### Array

See the [MongoDB documentation]({{< docsref "reference/operator/query/#array" >}}) for more information on each operator.

#### $all

```csharp
var local = new [] { 10, 20 };
Find(x => local.All(i => FavoriteNumbers.Contains(i));
```
```json
{ FavoriteNumbers: { $all: [10, 20] } }
```

#### $elemMatch

```csharp
Find(x => x.Pets.Any(p => p.Name == "Fluffy" && p.Age > 21);
```
```json
{ Pets: { $elemMatch: { Name: 'Fluffy', Age: { $gt: 21 } } } }
```
---
```csharp
Find(x => x.FavoriteNumbers.Any(n => n < 42 && n > 21));
```
```json
{ FavoriteNumbers: { $elemMatch: { $lt: 42, $gt: 21 } } }
```

{{% note %}}Depending on the complexity and the operators involved in the Any method call, the driver might eliminate the $elemMatch completely. For instance,

```csharp
Find(x => x.Pets.Any(p => p.Name == "Fluffy"))
```
```json
{ Pets: { Name: "Fluffy" } }
```{{% /note %}}


#### $size

```csharp
Find(x => x.FavoriteNumbers.Length == 3);
```
```json
{ FavoriteNumbers: { $size: 3 } }
```
---
```csharp
Find(x => x.FavoriteNumbers.Length != 3);
```
```json
{ FavoriteNumbers: { $not: { $size: 3 } } }
```
---
```csharp
Find(x => x.FavoriteNumbers.Any());
```
```json
{ FavoriteNumbers: { $ne: null, $not: { $size: 0 } } }
```
---
```csharp
Find(x => x.FavoriteNumbers.Count() == 3);
```
```json
{ FavoriteNumbers: { $size: 3 } }
```

## Aggregation Projections

We'll walk through the supported expressions below. The [tests]({{< srcref "MongoDB.Driver.Tests/Linq/Translators/AggregateProjectTranslatorTests.cs" >}}) are also a good reference.

### Boolean Expressions

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#boolean-expressions" >}}) for more information on each operator.

#### $and

```csharp
p => p.Name == "Jack" && p.Age < 40
```
```json
{ $and: [{ $eq: ['$Name', 'Jack'] }, { $lt: ['$Age', 40] }] }
```

#### $or

```csharp
p => p.Name == "Jack" || p.Age < 40
```
```json
{ $or: [{ $eq: ['$Name', 'Jack'] }, { $lt: ['$Age', 40] }] }
```

#### $not

```csharp
p => !(p.Name == "Jack")
```
```json
{ $not: [{ $eq: ['$Name', 'Jack'] }]] }
```

### Set Expressions

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#set-expressions" >}}) for more information on each operator.

#### $setEquals

```csharp
p => p.FavoriteNames.SetEquals(new [] { "Jack", "Jane" });
```
```json
{ $setEquals: ['$FavoriteNames', [ 'Jack', 'Jane'] ] }
```
---
```csharp
var localSet = new HashSet<int>(new [] { 1, 3, 5 });

p => localSet.SetEquals(p.FavoriteNumbers);
```
```json
{ $setEquals: [ [1, 3, 5], '$FavoriteNumbers' ] }
```

#### $setIntersection

```csharp
p => p.FavoriteNumbers.Intersect(new [] { 1, 3 });
```
```json
{ $setIntersection: [ '$FavoriteNumbers', [ 1, 3 ] ] }
```
---
```csharp
p => new [] { 1, 3 }.Intersect(p.FavoriteNumbers);
```
```json
{ $setIntersection: [ [ 1, 3 ], '$FavoriteNumbers' ] }
```

#### $setUnion

```csharp
p => p.FavoriteNumbers.Union(new [] { 1, 3 });
```
```json
{ $setUnion: [ '$FavoriteNumbers', [ 1, 3 ] ] }
```
---
```csharp
p => new [] { 1, 3 }.Union(p.FavoriteNumbers);
```
```json
{ $setUnion: [ [ 1, 3 ], '$FavoriteNumbers' ] }
```

#### $setDifference

```csharp
p => p.FavoriteNumbers.Except(new [] { 1, 3 });
```
```json
{ $setDifference: [ '$FavoriteNumbers', [ 1, 3 ] ] }
```
---
```csharp
p => new [] { 1, 3 }.Except(p.FavoriteNumbers);
```
```json
{ $setDifference: [ [ 1, 3 ], '$FavoriteNumbers' ] }
```

#### $setIsSubset

```csharp
p => p.FavoriteNames.IsSubsetOf(new [] { "Jack", "Jane" });
```
```json
{ $setIsSubset: ['$FavoriteNames', [ 'Jack', 'Jane'] ] }
```
---
```csharp
var localSet = new HashSet<int>(new [] { 1, 3, 5 });

p => localSet.IsSubsetOf(p.FavoriteNumbers);
```
```json
{ $setIsSubset: [ [1, 3, 5], '$FavoriteNumbers' ] }
```

#### $anyElementTrue

```csharp
p => p.FavoriteNumbers.Any(x => x > 20);
```
```json
{ $anyElementTrue: { $map: { input: '$FavoriteNumbers', as: 'x', in: { $gt: [ '$$x', 20 ] } } } }
```

#### $allElementsTrue

```csharp
p => p.FavoriteNumbers.All(x => x > 20);
```
```json
{ $allElementsTrue: { $map: { input: '$FavoriteNumbers', as: 'x', in: { $gt: [ '$$x', 20 ] } } } }
```

### Comparison Expressions

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#comparison-expressions" >}}) for more information on each operator.

#### $cmp

```csharp
p => p.Name.CompareTo("Jimmy");
```
```json
{ $cmp: [ '$Name', 'Jimmy' ] }
```

#### $eq

```csharp
p => p.Name == "Jimmy";
```
```json
{ $eq: [ '$Name', 'Jimmy' ] }
```

#### $gt

```csharp
p => p.Age > 20;
```
```json
{ $gt: [ '$Age', 20 ] }
```

#### $gte

```csharp
p => p.Age >= 20;
```
```json
{ $gte: [ '$Age', 20 ] }
```

#### $lt

```csharp
p => p.Age < 20;
```
```json
{ $lt: [ '$Age', 20 ] }
```

#### $lte

```csharp
p => p.Age <= 20;
```
```json
{ $lte: [ '$Age', 20 ] }
```

#### $ne

```csharp
p => p.Age != 20;
```
```json
{ $ne: [ '$Age', 20 ] }
```

### Arithmetic Expressions

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#arithmetic-expressions" >}}) for more information on each operator.

#### $abs

```csharp
p => Math.Abs(p.Age);
```
```json
{ $abs: "$Age" }
```

#### $add

```csharp
p => p.Age + 2;
```
```json
{ $add: [ '$Age', 2 ] }
```

#### $ceil

```csharp
p => Math.Ceiling(p.Age);
```
```json
{ $ceil: "$Age" }
```

#### $divide

```csharp
p => p.Age / 2;
```
```json
{ $divide: [ '$Age', 2 ] }
```

#### $exp

```csharp
p => Math.Exp(p.Age);
```
```json
{ $exp: ["$Age"] }
```

#### $floor

```csharp
p => Math.Floor(p.Age);
```
```json
{ $floor: "$Age" }
```

#### $ln

```csharp
p => Math.Log(p.Age);
```
```json
{ $ln: ["$Age"] }
```

#### $log

```csharp
p => Math.Log(p.Age, 10);
```
```json
{ $log: ["$Age", 10] }
```

#### $log10

```csharp
p => Math.Log10(p.Age);
```
```json
{ $log10: ["$Age"] }
```

#### $mod

```csharp
p => p.Age % 2;
```
```json
{ $mod: [ '$Age', 2 ] }
```

#### $multiply

```csharp
p => p.Age * 2;
```
```json
{ $multiply: [ '$Age', 2 ] }
```

#### $pow

```csharp
p => Math.Pow(p.Age, 10);
```
```json
{ $pow: ["$Age", 10] }
```

#### $sqrt

```csharp
p => Math.Sqrt(p.Age);
```
```json
{ $sqrt: ["$Age"] }
```

#### $subtract

```csharp
p => p.Age - 2;
```
```json
{ $subtract: [ '$Age', 2 ] }
```

#### $trunc

```csharp
p => Math.Truncate(p.Age);
```
```json
{ $trunc: "$Age" }
```


### String Expressions

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#string-expressions" >}}) for more information on each operator.

#### $concat

```csharp
p => p.Name + "Awesome";
```
```json
{ $concat: [ '$Name', 'Awesome' ] }
```

#### $substr

```csharp
p => p.Name.Substring(3, 20)
```
```json
{ $substr: [ '$Name', 3, 20 ] }
```

#### $toLower

```csharp
p => p.Name.ToLower()
```
```json
{ $toLower: '$Name' }
```

#### $toUpper

```csharp
p => p.Name.ToUpper()
```
```json
{ $toUpper: '$Name' }
```

#### $strcasecmp

You must use `StringComparison.OrdinalIgnoreCase` for this method.

```csharp
p => p.Name.Equals("balloon", StringComparison.OrdinalIgnoreCase);
```
```json
{ $strcasecmp: ['$Name', 'balloon' ] }
```

### Text Search Expressions

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#text-search-expressions" >}}) for more information on each operator.

#### $meta

```csharp
// no example yet
```

### Array Expressions

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#array-expressions" >}}) for more information on each operator.

#### $arrayElemAt

```csharp
p => p.FavoriteNumbers.First()
```
```json
{ $arrayElemAt: ['$FavoriteNumbers', 0] }
```
```csharp
p => p.FavoriteNumbers.Last()
```
```json
{ $arrayElemAt: ['$FavoriteNumbers', -1] }
```

#### $avg

```csharp
p => p.FavoriteNumbers.Average()
```
```json
{ $avg: '$FavoriteNumbers' }
```

#### $concatArrays

```csharp
p => p.FavoriteNumbers.Concat(new [] { 1, 2, 3 })
```
```json
{ $concatArrays: ['$FavoriteNumbers', [1, 2, 3]] }
```

#### $filter

```csharp
p => p.FavoriteNumbers.Where(x => x > 10)
```
```json
{ $filter: { input: '$FavoriteNumbers', as: 'x', cond: { $gt: ['$$x', 10] } } }
```

#### $map

```csharp
p => p.FavoriteNumbers.Select(x => x + 10)
```
```json
{ $map: { input: '$FavoriteNumbers', as: 'x', in: { $add: ['$$x', 10] } } }
```

#### $max

```csharp
p => p.FavoriteNumbers.Max()
```
```json
{ $max: '$FavoriteNumbers' }
```

#### $min

```csharp
p => p.FavoriteNumbers.Min()
```
```json
{ $min: '$FavoriteNumbers' }
```

#### $size

```csharp
p => p.FavoriteNumbers.Length;
```
```json
{ $size: '$FavoriteNumbers' }
```
---
```csharp
p => p.FavoriteNumbers.Count();
```
```json
{ $size: '$FavoriteNumbers' }
```

#### $slice

```csharp
p => p.FavoriteNumbers.Take(2)
```
```json
{ $slice: ['$FavoriteNumbers', 2] }
```
```csharp
p => p.FavoriteNumbers.Skip(3).Take(2)
```
```json
{ $slice: ['$FavoriteNumbers', 3, 2] }
```

#### $stdDevPop

```csharp
p => p.FavoriteNumbers.StandardDeviationPopulation()
```
```json
{ $stdDevPop: '$FavoriteNumbers' }
```

#### $stdDevSamp

```csharp
p => p.FavoriteNumbers.StandardDeviationSample()
```
```json
{ $stdDevPop: '$FavoriteNumbers' }
```

#### $sum

```csharp
p => p.FavoriteNumbers.Sum()
```
```json
{ $sum: '$FavoriteNumbers' }
```

### Variable Expressions

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#variable-expressions" >}}) for more information on each operator.

#### $map

```csharp
p => p.Pets.Select(x => x.Name + " is awesome");
```
```json
{ $map: { input: '$Name', as: 'x', in: { $concat: [ '$$x', ' is awesome' ] } } }
```

#### $let

```csharp
// no example yet
```

### Literal Expressions

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#literal-expressions" >}}) for more information on each operator.

#### $literal

The only time `$literal` will be used from the driver is if the constant starts with a `$`.

```csharp
p => p.Name == "$1");
```
```json
{ $eq: [ '$Name', { $literal: '$1' } ] }
```

### Date Expressions

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#date-expressions" >}}) for more information on each operator.

#### $dayOfYear

```csharp
p => p.CreatedAtUtc.DayOfYear);
```
```json
{ $dayOfYear: '$CreatedAtUtc' }
```

#### $dayOfMonth

```csharp
p => p.CreatedAtUtc.Day);
```
```json
{ $dayOfMonth: '$CreatedAtUtc' }
```

#### $dayOfWeek

The .NET constant for day of week is 1 less than that in MongoDB. As such, we must subtract 1 from the MongoDB version in order for mapping back to the .NET type to be accurate.

```csharp
p => p.CreatedAtUtc.DayOfWeek);
```
```json
{ $subtract: [ { $dayOfWeek: '$CreatedAtUtc' }, 1 ] }
```

#### $year

```csharp
p => p.CreatedAtUtc.Year);
```
```json
{ $year: '$CreatedAtUtc' }
```

#### $month

```csharp
p => p.CreatedAtUtc.Month);
```
```json
{ $month: '$CreatedAtUtc' }
```

#### $week

```csharp
// no example yet
```

#### $hour

```csharp
p => p.CreatedAtUtc.Hour);
```
```json
{ $hour: '$CreatedAtUtc' }
```

#### $minute

```csharp
p => p.CreatedAtUtc.Minute);
```
```json
{ $minute: '$CreatedAtUtc' }
```

#### $second

```csharp
p => p.CreatedAtUtc.Second);
```
```json
{ $second: '$CreatedAtUtc' }
```

#### $millisecond

```csharp
p => p.CreatedAtUtc.Millisecond);
```
```json
{ $millisecond: '$CreatedAtUtc' }
```

#### $dateToString

```csharp
// no example yet
```

### Conditional Expressions

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#conditional-expressions" >}}) for more information on each operator.

#### $cond

```csharp
p => p.Name == "Jack" ? "a" : "b";
```
```json
{ $cond: [ { $eq: [ '$Name', 'Jack' ] }, 'a', 'b' ] }
```

#### $ifNull

```csharp
p => p.Name ?? "awesome";
```
```json
{ $ifNull: [ '$Name', 'awesome' ] }
```

### Accumulators

See the [MongoDB documentation]({{< docsref "meta/aggregation-quick-reference/#accumulators" >}}) for more information on each operator.

Also, the [tests]({{< srcref "MongoDB.Driver.Tests/Linq/Translators/AggregateGroupTranslatorTests.cs" >}}) are a good reference.

{{% note %}}These are only supported in a grouping expression.{{% /note %}}

In the examples below, it should be assumed that `g` is of type [`IGrouping<TKey, TElement>`]({{< msdnref "bb344977" >}}).

#### $sum

```csharp
g => g.Sum(x => x.Age);
```
```json
{ $sum: '$Age' }
```
---
```csharp
g => g.Count()
```
```json
{ $sum: 1 }
```

#### $avg

```csharp
g => g.Average(x => x.Age);
```
```json
{ $avg: '$Age' }
```

#### $first

```csharp
g => g.First().Age);
```
```json
{ $first: '$Age' }
```

#### $last

```csharp
g => g.Last().Age);
```
```json
{ $last: '$Age' }
```

#### $max

```csharp
g => g.Max(x => x.Age);
```
```json
{ $max: '$Age' }
```

#### $min

```csharp
g => g.Min(x => x.Age);
```
```json
{ $min: '$Age' }
```

#### $push

```csharp
g => g.Select(p => p.Name)
```
```json
{ $push: '$Name' }
```
---
```csharp
g => g.Select(p => p.Name).ToArray()
```
```json
{ $push: '$Name' }
```
---
```csharp
g => g.Select(p => p.Name).ToList()
```
```json
{ $push: '$Name' }
```

#### $addToSet

```csharp
g => new HashSet<string>(g.Select(p => p.Name));
```
```json
{ $addToSet: '$Name' }
```
---
```csharp
g => g.Select(p => p.Name).Distinct();
```
```json
{ $addToSet: '$Name' }
```