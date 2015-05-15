+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Conventions"
[menu.main]
  parent = "Mapping Classes"
  identifier = "Conventions"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## Conventions

When [automapping]({{< relref "reference\bson\mapping\index.md#automap" >}}) a class, there are a lot of decisions that need to be made. For example:

- Which members of the class should be serialized
- Which member of the class is the “Id”
- What element name should be used in the BSON document
- If the class is being used polymorphically, what discriminator values are used
- What should happen if a BSON document has elements we don’t recognize
- Does the member have a default value
- Should the default value be serialized or ignored

Answers to these questions are represented by a set of “conventions”. For each convention, there is a default convention that is the most likely one you will be using, but you can override individual conventions as necessary.

If you want to use your own conventions that differ from the defaults, simply create an instance of [`ConventionPack`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_ConventionPack" >}}), add in the conventions you want to use, and then register that pack. For example:

```csharp
var pack = new ConventionPack();
pack.Add(new CamelCaseElementNameConvention());

ConventionRegistry.Register(
   "My Custom Conventions",
   pack,
   t => t.FullName.StartsWith("MyNamespace."));
```

The third parameter is a filter function that defines when this convention pack should be used. Above, we are saying that any classes whose full names begin with "MyNamespace." should use these conventions.

## Custom Conventions

In addition to pre-packaged conventions, it is possible to write your own. There are 4 classes of conventions which can be created and registered. These 4 classes of conventions correspond with the 4 stages in which they will be run.

1. **Class Stage:** [`IClassMapConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_IClassMapConvention" >}})

	Run against the class map.

1. **Member Stage:** [`IMemberMapConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_IMemberMapConvention" >}})

	Run against each member map discovered during the Class stage.

1. **Creator Stage:** [`ICreatorMapConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_ICreatorMapConvention" >}})

	Run against each CreatorMap discovered during the Class stage.

1. **Post Processing Stage:** [`IPostProcessingConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_IPostProcessingConvention" >}})

	Run against the class map.

Conventions get run in the order they were registered in each stage. The default set of conventions is registered first. This allows any user registered conventions to override the values applied by the default conventions. Hence, it is possible that certain values may get applied and overwritten. It is up to the user to ensure that the order is correct.

{{% note %}}If a custom implementation of an [`IPostProcessingConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_IPostProcessingConvention" >}}) is registered before a customer implementation of an [`IClassMapConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_IClassMapConvention" >}}), the [`IClassMapConvention`]({{< apiref "T_MongoDB_Bson_Serialization_Conventions_IClassMapConvention" >}}) will be run first because the Class Stage is before the Post Processing Stage.{{% /note %}}

### Example

As an example, we will write a custom convention to name all the elements the corresponding lower-case version of the member name. We can implement this convention as follows:

```csharp
public class LowerCaseElementNameConvention : IMemberMapConvention 
{
    public void Apply(BsonMemberMap memberMap) 
    {
        memberMap.SetElementName(memberMap.MemberName.ToLower());
    }
}
```

When you are doing one-off conventions like this, it might be easier to create them with a simple lambda expresion instead. For example:

```csharp
var pack = new ConventionPack();
pack.AddMemberMapConvention(
    "LowerCaseElementName",
    m => m.SetElementName(m.MemberName.ToLower()));
```

For the best examples of writing custom conventions, it is good to consult the source for the pre-packaged conventions.