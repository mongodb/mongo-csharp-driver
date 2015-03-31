+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Definitions and Builders"
[menu.main]
  parent = "Driver"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## Definitions and Builders

The driver has introduced a number of types related to the specification of filters, updates, projections, sorts, etc... These types are used throughout the [API]({{< relref "reference\driver\crud\index.md" >}}). In addition, most of them have builders as well to aid in their creation.


## Fields

[`FieldDefinition<TDocument>`]({{< apiref "T_MongoDB_Driver_FieldDefinition_1" >}}) and [`FieldDefinition<TDocument, TField>`]({{< apiref "T_MongoDB_Driver_FieldDefinition_2" >}}) define how to get a field name. They are implicitly convertible from a string, so that you can simply pass the field name you'd like.

For instance, with a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}), the following will be true:

```csharp
// method definition
string GetFieldName<TDocument>(FieldDefinition<TDocument> field);

// invocation
var fieldName = GetFieldName<BsonDocument>("fun");

fieldName.Should().Be("fun");
```

However, if you are working with a [mapped class]({{< relref "reference\bson\mapping\index.md" >}}), then 2 further options exist.

First, most locations that accept a [`FieldDefinition<TDocument>`]({{< apiref "T_MongoDB_Driver_FieldDefinition_1" >}}) or [`FieldDefinition<TDocument, TField>`]({{< apiref "T_MongoDB_Driver_FieldDefinition_2" >}}) will also have an overload (via extension method) that accepts a lambda expression. Given the following class definition marked with the [`BsonElementAttribute`]({{< apiref "T_MongoDB_Bson_Serializers_Attributes_BsonElementAttribute" >}}) instructing the use of a different field name:

```csharp
class Person
{
	[BsonElement("fn")]
	public string FirstName { get; set; }

	[BsonElement("fn")]
	public string LastName { get; set; }
}
```

Then the following will be true.

```csharp
// method definition
string GetFieldName<TDocument>(Expression<Func<TDocument, object>> field);

// invocation
var fieldName = GetFieldName<Person>(x => x.FirstName);

fieldName.Should().Be("fn");
```

In the same way, since we know the type of document, `Person` in this case, we can also translate fields names provided as a string.

```csharp
// method definition
string GetFieldName<TDocument>(FieldDefinition<TDocument> field);

// invocation
var fieldName = GetFieldName<Person>("FirstName");

fieldName.Should().Be("fn");
```

Finally, we don't validate that the provided string exists as a mapped field, so it is still possible to provide a field that hasn't been mapped:

```csharp
// method definition
string GetFieldName<TDocument>(FieldDefinition<TDocument> field);

// invocation
var fieldName = GetFieldName<Person>("fn");

fieldName.Should().Be("fn");
```


## Filters

[`FilterDefinition<TDocument>`]({{< apiref "T_MongoDB_Driver_FilterDefinition_1" >}}) defines a filter. It is implicity convertible from both a JSON string as well as a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}). Given the following method, all the following are valid:

```csharp
// method definition
BsonDocument Filter(FilterDefinition<BsonDocument> filter);

// invocation
var doc = Filter("{ x: 1 }");
doc.ToJson().Should().Be("{ x: 1 }");

var doc = Filter(new BsonDocument("x", 1));
doc.ToJson().Should().Be("{ x: 1 }");
```

### Filter Definition Builder 

_See the [tests]({{< srcref "MongoDB.Driver.Tests/FilterDefinitionBuilderTests.cs" >}}) for examples._

The [`FilterDefinitionBuilder<TDocument>`]({{< apiref "T_MongoDB_Driver_FilterDefinitionBuilder_1" >}}) provides a nice API for building up both simple and complex [MongoDB queries]({{< docsref "reference/operator/query/" >}}).

For example, to build up the filter `{ x: 10, y: { $lt: 20 } }`, the following calls are all comparable.

```csharp
var builder = Builders<BsonDocument>.Filter;
var filter = builder.Eq("x", 10) & builder.Lt("y", 20);
```

{{% note %}}Note that the `&` operator is overloaded. Other overloaded operators include the `|` operator for "or" and the `!` operator for "not".{{% /note %}}

Given the following class:

```csharp
class Widget
{
	[BsonElement("x")]
	public int X { get; set; }

	[BsonElement("y")]
	public int Y { get; set; }
}
```

We can achieve the same result in the typed variant:

```csharp
var builder = Builders<Widget>.Filter;
var filter = builder.Eq(x => x.X, 10) & builder.Lt(x => x.Y, 20);

// or

var filter = builder.Eq("X", 10) & builder.Lt("Y", 20);

// or

var filter = builder.Eq("x", 10) & builder.Lt("y", 20);
```

Or via a lambda expression:

```csharp
var filter = Builders<Widget>.Filter.Where(x => x.X == 10 && x.Y < 20);
```

For more information on valid lambda expressions, see the [expressions documentation]({{< relref "reference\driver\expressions.md" >}}).

#### Array Operators 

When using entities with properties or fields that serialize to arrays, you can use the methods prefixed with "Any" to compare the entire array against a single item.

Given the following class:

```csharp
public class Post
{
	public IEnumerable<string> Tags { get; set; }
}
```

To see if any of the tags equals "mongodb":

```csharp
var filter = Builders<Post>.Filter.AnyEq(x => x.Tags, "mongodb");

// This will NOT compile:
// var filter = Builders<Post>.Filter.Eq(x => x.Tags, "mongodb");
```

## Pipelines

A pipeline definition defines an entire aggregation pipeline. It is implicitly convertible from a [`List<BsonDocument>`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}), a [`BsonDocument[]`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}), a [`List<IPipelineStageDefinition>`]({{< apiref "T_MongoDB_Driver_IPipelineStageDefinition" >}}) , and a [`IPipelineStageDefinition[]`]({{< apiref "T_MongoDB_Driver_IPipelineStageDefinition" >}}).

For example:

```csharp
PipelineDefinition pipeline = new BsonDocument[] 
{
	new BsonDocument { { "$match", new BsonDocument("x", 1) } },
	new BsonDocument { { "$sort", new BsonDocument("y", 1) } }
};
```

{{% note %}}There is no builder for a PipelineDefinition. In most cases, the `IAggregateFluent<TDocument>` interface would be used which is returned from the `IMongoCollection<TDocument>.Aggregate` method.{{% /note %}}


## Projections

There are two forms of a projection definition, one where the type of the projection is known ([`ProjectionDefinition<TDocument, TProjection>`]({{< apiref "T_MongoDB_Driver_ProjectionDefinition_2" >}})) and one where the type of the projection is not yet known ([`ProjectionDefinition<TDocument>`]({{< apiref "T_MongoDB_Driver_ProjectionDefinition_1" >}})). The latter, while implicitly convertible to the first, is merely used as a building block. The [high-level APIs]({{< relref "reference\driver\crud\index.md" >}}) that take a projection will always take the former. This is because, when determining how to handle a projection client-side, it is not enough to know what fields and transformations will take place. It also requires that we know how to interpret the projected shape as a .NET type. Since the driver allows you to work with custom classes, it is imperative that any projection also include the "interpretation instructions" for projecting into a custom class.

### Projection Definition Builder 

_See the [tests]({{< srcref "MongoDB.Driver.Tests/ProjectionDefinitionBuilderTests.cs" >}}) for examples._

The [`ProjectionDefinitionBuilder<TDocument>`]({{< apiref "T_MongoDB_Driver_ProjectionDefinitionBuilder_1" >}}) exists to make it easier to build up projections in MongoDB's syntax. To render the projection `{ x: 1, y: 1, _id: 0 }`, the following should be done:

```csharp
var projection = Builders<BsonDocument>.Projection.Include("x").Include("y").Exclude("_id");
```

Using the `Widget` class:

```csharp
class Widget
{
	public ObjectId Id { get; set; }

	[BsonElement("x")]
	public int X { get; set; }

	[BsonElement("y")]
	public int Y { get; set; }
}
```

We can render the same projection in a couple of ways:

```csharp
var projection = Builders<Widget>.Projection.Include("X").Include("Y").Exclude("Id");

// or

var projection = Builders<Widget>.Projection.Include("x").Include("y").Exclude("_id");

// or

var projection = Builders<Widget>.Projection.Include(x => x.X).Include(x => x.Y).Exclude(x => x.Id);

// or

var projection = Builders<Widget>.Projection.Expression(x => new { X = x.X, Y = x.Y });
```

This last projection where we've used the [`Expression`]({{< apiref "M_MongoDB_Driver_ProjectionDefinitionBuilder_1_Expression" >}}) method is subtly different as is explained below, and its return type is a ([`ProjectionDefinition<TDocument, TProjection>`]({{< apiref "T_MongoDB_Driver_ProjectionDefinition_2" >}})) as opposed to the others which return a ([`ProjectionDefinition<TDocument>`]({{< apiref "T_MongoDB_Driver_ProjectionDefinition_1" >}})).


#### Lambda Expressions

The driver supports using expression trees to render projections. The same expression tree will sometimes render differently when used in a Find operation versus when used in an Aggregate operation. Inherently, a lambda expression contains all the information necessary to form both the projection on the server as well as the client-side result and requires no further information.


##### Find 

_See the [tests]({{< srcref "MongoDB.Driver.Tests/Linq/Translators/FindProjectionTranslatorTests.cs" >}}) for examples._

When a Find projection is defined using a lambda expression, only the [projection operators]({{< docsref "reference/operator/query/#projection-operators" >}}) are supported.

{{% note %}}$slice and $meta are not currently supported via lambda expressions.{{% /note %}}

Given the following class:

```csharp
class Widget
{
	public ObjectId Id { get; set; }

	[BsonElement("x")]
	public int X { get; set; }

	[BsonElement("y")]
	public int Y { get; set; }
}
```

The following lambda expressions will all result in the projection `{ x: 1, y: 1, _id: 0 }`. This is because we inspect the expression tree to discover all the fields that are used and tell the server to include them. We then run the lambda expression client-side. As such, Find projections support virtually the entire breadth of the C# language.

```csharp
var projection = Builders<Widget>.Projection.Expression(x => new { X = x.X, Y = x.Y });

var projection = Builders<Widget>.Projection.Expression(x => new { Sum = x.X + x.Y });

var projection = Builders<Widget>.Projection.Expression(x => new { Avg = (x.X + x.Y) / 2 });

var projection = Builders<Widget>.Projection.Expression(x => (x.X + x.Y) / 2);
```

The `_id` field is excluded automatically when we know for certain that it isn't necessary, as is the case in all the above examples.


##### Aggregate 

_See the [tests]({{< srcref "MongoDB.Driver.Tests/Linq/Translators/AggregateProjectionTranslatorTests_Project.cs" >}}) for examples._

When an aggregate projection is defined using a lambda expression, a majority of the [aggregation expression operators]({{< docsref "reference/operator/aggregation/#expression-operators" >}}) are supported and translated. Unlike a project for Find, no part of the lambda expression is run client-side. This means that all expressions in a projection for the [Aggregation Framework]({{< docsref "core/aggregation-pipeline/" >}}) must be expressible on the server.


##### Grouping 

_See the [tests]({{< srcref "MongoDB.Driver.Tests/Linq/Translators/AggregateProjectionTranslatorTests_Group.cs" >}}) for examples._

A projection is also used when performing grouping in the [Aggregation Framework]({{< docsref "core/aggregation-pipeline/" >}}). In addition to the expression operators used in an aggregate projection, the [aggregation accumulator operators]({{< docsref "reference/operator/aggregation/group/#accumulator-operator" >}}) are also supported. 


## Sorts

[`SortDefinition<TDocument>`]({{< apiref "T_MongoDB_Driver_SortDefinition_1" >}}) defines how to render a valid sort document. It is implicity convertible from both a JSON string as well as a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}). Given the following method, all the following are valid:

```csharp
// method definition
BsonDocument Sort(SortDefinition<BsonDocument> filter);

// invocation
var doc = Sort("{ x: 1 }");
doc.ToJson().Should().Be("{x : 1 }");

var doc = Sort(new BsonDocument("x", 1));
doc.ToJson().Should().Be("{ x: 1 }");
```

### Sort Definition Builder 

_See the [tests]({{< srcref "MongoDB.Driver.Tests/SortDefinitionBuilderTests.cs" >}}) for examples._

The [`SortDefinitionBuilder<TDocument>`]({{< apiref "T_MongoDB_Driver_SortDefinitionBuilder_1" >}}) provides a nice API for building up [MongoDB sort syntax]({{< docsref "reference/method/cursor.sort/" >}}).

For example, to build up the sort `{ x: 1, y: -1 }`, do the following:

```csharp
var builder = Builders<BsonDocument>.Sort;
var sort = builder.Ascending("x").Descending("y");
```

Given the following class:

```csharp
class Widget
{
	[BsonElement("x")]
	public int X { get; set; }

	[BsonElement("y")]
	public int Y { get; set; }
}
```

We can achieve the same result in the typed variant:

```csharp
var builder = Builders<Widget>.Sort;
var sort = builder.Ascending(x => x.X).Descending(x => x.Y);

// or

var sort = builder.Ascending("X").Descending("Y");

// or

var sort = builder.Ascending("x").Descending("y");
```


## Updates

[`UpdateDefinition<TDocument>`]({{< apiref "T_MongoDB_Driver_UpdateDefinition_1" >}}) defines how to render a valid update document. It is implicity convertible from both a JSON string as well as a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}). Given the following method, all the following are valid:

```csharp
// method definition
BsonDocument Update(UpdateDefinition<BsonDocument> filter);

// invocation
var doc = Update("{ $set: { x: 1 } }");
doc.ToJson().Should().Be("{ $set: { x: 1 } }");

var doc = Sort(new BsonDocument("$set", new BsonDocument("x", 1)));
doc.ToJson().Should().Be("{ $set: { x: 1 } }");
```

### Update Definition Builder 

_See the [tests]({{< srcref "MongoDB.Driver.Tests/UpdateDefinitionBuilderTests.cs" >}}) for examples._

The [`UpdateDefinitionBuilder<TDocument>`]({{< apiref "T_MongoDB_Driver_UpdateDefinitionBuilder_1" >}}) provides a nice API for building the [MongoDB update specification]({{< docsref "reference/operator/update/" >}}).

For example, to build up the update `{ $set: { x: 1, y: 3 }, $inc: { z: 1 } }`, do the following:

```csharp
var builder = Builders<BsonDocument>.Update;
var update = builder.Set("x", 1).Set("y", 3).Inc("z", 1);
```

Given the following class:

```csharp
class Widget
{
    [BsonElement("x")]
    public int X { get; set; }

    [BsonElement("y")]
    public int Y { get; set; }

    [BsonElement("z")]
    public int Z { get; set; }
}
```

We can achieve the same result in a typed variant:

```csharp
var builder = Builders<Widget>.Update;
var update = builder.Set(x => x.X, 1).Set(x => x.Y, 3).Inc(x => x.Z, 1);

// or

var update = builder.Set("X", 1).Set("Y", 3).Inc("Z", 1);

// or

var update = builder.Set("x", 1).Set("y", 3).Inc("z", 1);
```