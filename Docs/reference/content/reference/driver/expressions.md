+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Expressions"
[menu.main]
  parent = "Definitions and Builders"
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

#### $nin

```csharp
int[] localAges = new [] { 10, 20, 30 };
Find(p => !localAges.Contains(p.Age));
```
```json
{ Age: { $nin: [10, 20, 30] } }
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

### Geospatial

See the [MongoDB documentation]({{< docsref "reference/operator/query/#array" >}}) for more information on each operator.

#### $all

```csharp
// no example yet
```

#### $elemMatch

```csharp
Find(x => x.Pets.Any(p => p.Name == "Fluffy");
```
```json
{ Pets: { $elemMatch: { Name: 'Fluffy' } } }
```
---
```csharp
Find(x => x.FavoriteNumbers.Any(n => n > 21));
```
```json
{ FavoriteNumbers: { $elemMatch: { { $gt: 21 } } } }
```

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