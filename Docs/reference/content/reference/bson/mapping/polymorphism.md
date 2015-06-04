+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Polymorphism"
[menu.main]
  parent = "Mapping Classes"
  identifier = "Polymorphism"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Polymorphism

When you have a class hierarchy and will be serializing instances of varying classes to the same collection you need a way to distinguish one from another. The normal way to do so is to write some kind of special value (called a “discriminator”) in the document along with the rest of the elements that you can later look at to tell them apart. Since there are potentially many ways you could discriminate between actual types, the default serializer uses conventions for discriminators. The default serializer provides two standard discriminators: [`ScalarDiscriminatorConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_ScalarDiscriminatorConvention" >}}) and [`HierarchicalDiscriminatorConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_HierarchicalDiscriminatorConvention" >}}). The default is the [`HierarchicalDiscriminatorConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_HierarchicalDiscriminatorConvention" >}}), but it behaves just like the [`ScalarDiscriminatorConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_ScalarDiscriminatorConvention" >}}) until certain options are set to trigger its hierarchical behavior.

The default discriminator conventions both use an element named _t to store the discriminator value in the BSON document. This element will normally be the second element in the BSON document (right after the _id). In the case of the [`ScalarDiscriminatorConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_ScalarDiscriminatorConvention" >}}), the value of _t will be a single string. In the case of the [`HierarchicalDiscriminatorConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_HierarchicalDiscriminatorConvention" >}}) the value of _t will be an array of discriminator values, one for each level of the class inheritance tree.

While you will normally be just fine with the default discriminator convention, you might have to write a custom discriminator convention if you must work with data written by another driver or object mapper that uses a different convention for its discriminators.


## Setting the Discriminator Value

The default value for the discriminator is the name of the class (without the namespace part). You can specify a different value using attributes:

```csharp
[BsonDiscriminator("myclass")]
public MyClass {
    // fields and properties
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.SetDiscriminator("myclass");
});
```

## Specifying Known Types

When deserializing polymorphic classes, it is important that the serializer know about all the classes in the hierarchy before deserialization begins. If you ever see an error message about an “Unknown discriminator”, it is because the deserializer can’t figure out the class for that discriminator. If you are mapping your classes programmatically simply make sure that all classes in the hierarchy have been mapped before beginning deserialization. When using attributes and [automapping]({{< relref "reference\bson\mapping\index.md#automap" >}}), you will need to inform the serializer about known types (i.e. subclasses) it should create class maps for. Here is an example of how to do this:

```csharp
[BsonKnownTypes(typeof(Cat), typeof(Dog)]
public class Animal 
{
}

[BsonKnownTypes(typeof(Lion), typeof(Tiger)]
public class Cat : Animal 
{
}

public class Dog : Animal 
{
}

public class Lion : Cat 
{
}

public class Tiger : Cat 
{
}
```

The [`BsonKnownTypesAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonKnownTypesAttribute" >}}) attribute lets the serializer know what subclasses it might encounter during deserialization, so when `Animal` is [automapped]({{< relref "reference\bson\mapping\index.md#automap" >}}), the serializer will also [automap]({{< relref "reference\bson\mapping\index.md#automap" >}}) `Cat` and `Dog`, and recursively, `Lion` and `Tiger` as well.

Or via code:

```csharp
BsonClassMap.RegisterClassMap<Animal>();
BsonClassMap.RegisterClassMap<Cat>();
BsonClassMap.RegisterClassMap<Dog>();
BsonClassMap.RegisterClassMap<Lion>();
BsonClassMap.RegisterClassMap<Tiger>();
```


## Scalar and Hierarchical Discriminators

Normally a discriminator is simply the name of the class (although it could be different if you are using a custom discriminator convention or have explicitly specified a discriminator for a class). So a collection containing a mix of different type of Animal documents might look like:

```json
{ _id: ..., _t: "Animal", ... }
{ _id: ..., _t: "Cat", ... }
{ _id: ..., _t: "Dog", ... }
{ _id: ..., _t: "Lion", ... }
{ _id: ..., _t: "Tiger", ... }
```

Sometimes it can be helpful to record a hierarchy of discriminator values, one for each level of the hierarchy. To do this, you must first mark a base class as being the root of a hierarchy, and then the default [`HierarchicalDiscriminatorConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_HierarchicalDiscriminatorConvention" >}}) will automatically record discriminators as array values instead.

To identify `Animal` as the root of a hierarchy use the [`BsonDiscriminatorAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonDiscriminatorAttribute" >}}) attribute with the [`RootClass`]({{< apiref "P_MongoDB_Bson_Serialization_Attributes_BsonDiscriminatorAttribute_RootClass" >}}) named parameter:

```csharp
[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(Cat), typeof(Dog)]
public class Animal 
{
}

// the rest of the hierarchy as before
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<Animal>(cm => {
    cm.AutoMap();
    cm.SetIsRootClass(true);
});
BsonClassMap.RegisterClassMap<Cat>();
BsonClassMap.RegisterClassMap<Dog>();
BsonClassMap.RegisterClassMap<Lion>();
BsonClassMap.RegisterClassMap<Tiger>();
```

Now that you have identified Animal as a root class, the discriminator values will look a little bit different:

```json
{ _id: ..., _t: "Animal", ... }
{ _id: ..., _t: ["Animal", "Cat"], ... }
{ _id: ..., _t: ["Animal", "Dog"], ... }
{ _id: ..., _t: ["Animal", "Cat", "Lion"], ... }
{ _id: ..., _t: ["Animal", "Cat", "Tiger"], ... }
```

The main reason you might choose to use hierarchical discriminators is because it makes it possibly to query for all instances of any class in the hierarchy. For example, to read all the `Cat` documents we can use the following filter.

```csharp
var filter = new BsonDocument("_t", "Cat");
```