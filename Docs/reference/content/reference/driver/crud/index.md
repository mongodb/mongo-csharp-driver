+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Reading and Writing"
[menu.main]
  parent = "Driver"
  weight = 50
  identifier = "Reference Reading and Writing"
  pre = "<i class='fa'></i>"
+++

## Reading and Writing

All the create, read, update, and delete (CRUD) operations take a similar form and are defined on the [`IMongoCollection<TDocument>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}}) interface. All the required fields take the form of a positional parameter and, if any options exists, they are passed in as an instance of an options class. All methods are available in both synchronous and asynchronous versions. For example, the following method signatures exists:

```csharp
long Count(FilterDefinition<TDocument> filter, CountOptions options = null);
```
```csharp
Task<long> CountAsync(FilterDefinition<TDocument> filter, CountOptions options = null);
```

As described, the `filter` is required and the `options` can be omitted.

A majority of the methods in the CRUD API take some form of a definition class. Many of these classes contain implicit conversions to make it easy to pass in common types of values like a JSON string or a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}). In addition, most of the definition classes contain a definition builder that can make it easy to build MongoDB specific syntax. See the [Definitions and Builders]({{< relref "definitions.md" >}}) section for more information.

### Polymorphism

The driver supports polymorphic class hierarchies fully. To understand how discriminators are handled for entities, see the reference on [polymorphism]({{< relref "reference\bson\mapping\polymorphism.md" >}}).

The requirement to use polymorphic hierarchies during CRUD operations is that the generic parameter on your collection instance must be the base class. For example, with a class hierarchy of Animals:

```
Animal
  - Cat
    - Lion
    - Tiger
  - Dog
```

The collection instance must be an [`IMongoCollection<Animal>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}}).


#### Working with Subclasses

The [`OfType`]({{< apiref "M_MongoDB_Driver_IMongoCollection_1_OfType_1">}}) method applies a discriminating filter to all methods of an [`IMongoCollection<T>`]({{< apiref "T_MongoDB_Driver_IMongoCollection_1" >}}). For example, to work only with `Cats` from the "animals" collection:

```csharp
IMongoCollection<Animal> animals = db.GetCollection<Animal>("animals");

IMongoCollection<Cat> cats = animals.OfType<Cat>();
```

{{% note class="warning" %}}It is imperative that the collection retrieved from the database instance be the root of the hierarchy. `db.GetCollection<Cat>("animals")` will NOT include the discriminator.{{% /note %}} 

