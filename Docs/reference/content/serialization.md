+++
date = "2015-03-18T16:56:14Z"
draft = false
title = "Serialization Tutorial"
[menu.main]
  weight = 30
  pre = "<i class='fa'></i>"
+++

Serialization Tutorial
=======================================

Introduction
------------

This section of the C\# Driver Tutorial discusses serialization (and
deserialization) of instances of C\# classes to and from BSON documents.
Serialization is the process of mapping an object to a BSON document
that can be saved in MongoDB, and deserialization is the reverse process
of reconstructing an object from a BSON document. For that reason the
serialization process is also often referred to as "Object Mapping."

Serialization is handled by the BSON Library. The BSON Library has an
extensible serialization architecture, so if you need to take control of
serialization you can. The BSON Library provides a default serializer
which should meet most of your needs, and you can supplement the default
serializer in various ways to handle your particular needs.

The main way the default serializer handles serialization is through
"class maps". A class map is a structure that defines the mapping
between a class and a BSON document. It contains a list of the fields
and properties of the class that participate in serialization and for
each one defines the required serialization parameters (e.g., the name
of the BSON element, representation options, etc...).

The default serializer also has built in support for many .NET data
types (primitive values, arrays, lists, dictionaries, etc...) for which
class maps are not used.

Before an instance of a class can be serialized a class map must exist.
You can either create this class map yourself or simply allow the class
map to be created automatically when first needed (called
"automapping"). You can exert some control over the automapping process
either by decorating your classes with serialization related attributes
or by using initialization code (attributes are very convenient to use
but for those who prefer to keep serialization details out of their
domain classes be assured that anything that can be done with attributes
can also be done without them).

Creating a Class Map
--------------------

To create a class map in your initialization code write:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>();
```

which results in `MyClass` being automapped and registered. In this case
you could just as well have allowed the class to be automapped by the
serializer (when first serialized or deserialized). The one case where
you must call `RegisterClassMap` yourself (even without arguments) is
when you are using a polymorphic class hierarchy: in this case you must
register all the known subclasses to guarantee that the discriminators
get registered.

If you want to control the creation of the class map you can provide
your own initialization code in the form of a lambda expression:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.MapProperty(c => c.SomeProperty);
    cm.MapProperty(c => c.AnotherProperty);
});
```

When your lambda expression is executed the `cm` (short for class map)
parameter is passed an empty class map for you to fill in. In this
example two properties are added to the class map by calling the
`MapProperty` method. The arguments to `MapProperty` are themselves
lambda expressions which identify the property of the class. The
advantage of using a lambda expression instead of just a string
parameter with the name of the property is that `Intellisense` and
compile time checking ensure that you can't misspell the name of the
property.

It is also possible to use automapping and then override some of the
results. We will see examples of that later on.

Note that a class map must only be registered once (an exception will be
thrown if you try to register the same class map more than once).
Usually you call `RegisterClassMap` from some code path that is known to
execute only once (the `Main` method, the `Application_Start` event
handler, etc...). If you must call `RegisterClassMap` from a code path
that executes more than once, you can use `IsClassMapRegistered` to
check whether a class map has already been registered for a class:

``` {.sourceCode .csharp}
if (!BsonClassMap.IsClassMapRegistered(typeof(MyClass))) {
   // register class map for MyClass
}
```

### Creator Maps

By default, classes must contain a zero-argument constructor that will
be used to instantiate an instance to hydrate. However, it is possible
to configure a constructor whose arguments are correlated with mapped
properties or fields. There are a couple of ways to do this.

Using an expression, you can instruct the driver to use a creator map as
follows:

``` {.sourceCode .csharp}
public class Person
{
  public string FirstName { get; private set; }
  public string LastName { get; private set; }

  public Person(string firstName, string lastName)
  {
    FirstName = firstName;
    LastName = lastName;
  }
}

// snip...

BsonClassMap.RegisterClassMap<Person>(cm =>
{
    cm.AutoMap();
    cm.MapCreator(p => new Person(p.FirstName, p.LastName));
});
```

Parsing the expression tree correlates the first constructor argument
with the `FirstName` property and the second constructor argument with
the `LastName` property. There are other, more complicated ways of
handling this which can be explored on your own should the need arise.

Using attributes instead:

``` {.sourceCode .csharp}
public class Person
{
  public string FirstName { get; set; }
  public string LastName { get; set; }

  [BsonConstructor]
  public Person(string firstName, string lastName)
  {
    FirstName = firstName;
    LastName = lastName;
  }
}
```

By default, a convention runs on every CreatorMap with no mapped
arguments and attempts to correlate the names of the constructor
arguments with the names of mapped members. If your names differ in more
than just case, there are overloads of BsonConstructor which can be used
to explicity tell the driver which members to use.

When more than 1 constructor is found, we will use the most parameters
fulfilled strategy to identify the best match. For example:

``` {.sourceCode .csharp}
public class Person
{
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public DateTime? BirthDate { get; set; }

  [BsonConstructor]
  public Person(string firstName, string lastName)
  {
    // snip...
  }

  [BsonConstructor]
  public Person(string firstName, string lastName, DateTime birthDate)
  {
    // snip...
  }
}
```

If the document in the database has a BirthDate element, we will choose
to use the constructor with 3 parameters because it is more specific.

In addition to the above code and attribute forms, mapping a creator can
be handled via conventions.

Conventions
-----------

When automapping a class there are a lot of decisions that need to be
made. For example:

-   Which fields or properties of the class should be serialized
-   Which field or property of the class is the "Id"
-   What element name should be used in the BSON document
-   If the class is being used polymorphically what discriminator values
    are used
-   What should happen if a BSON document has elements we don't
    recognize
-   Does the field or property have a default value
-   Should the default value be serialized or ignored
-   Should `null` values be serialized or ignored

Answers to these questions are represented by a set of "conventions".
For each convention there is a default convention that is the most
likely one you will be using, but you can override individual
conventions (and even write your own) as necessary.

If you want to use your own conventions that differ from the defaults
simply create an instance of `ConventionPack` and add in the conventions
you want to use and then register that pack (in other words, tell the
default serializer when your special conventions should be used). For
example:

``` {.sourceCode .csharp}
var pack = new ConventionPack();
pack.Add(new CamelCaseElementNameConvention());

ConventionRegistry.Register(
   "My Custom Conventions",
   pack,
   t => t.FullName.StartsWith("MyNamespace."));
```

The third parameter is a filter function that defines when this
convention pack should be used. In this case we are saying that any
classes whose full names begin with `"MyNamespace."` should use
`myConventions`.

In addition to pre-packaged conventions, it is possible to write your
own. There are 4 classes of conventions which can be created and
registered. These 4 classes of conventions correspond with the 4 stages
in which they will be run.

1.  

    `Class Stage`: `IClassMapConvention`

    :   Run against the class map.

2.  

    `Member Stage`: `IMemberMapConvention`

    :   Run against each member map discovered during the
        `IClassMapConvention` stage.

3.  

    `Creator Stage`: `ICreatorMapConvention`

    :   Run against each CreatorMap discovered during the
        `IClassMapConvention` stage.

4.  

    `Post Processing Stage`: `IPostProcessingConvention`

    :   Run against the class map.

Conventions get run in the order they were registered in each stage. The
default set of conventions is registered first. This allows any user
registered conventions to override the values applied by the default
conventions. Hence, it is possible that certain values may get applied
and overwritten. It is up to the user to ensure that the order is
correct.

> **note**
>
> If a custom implementation of an `IPostProcessingConvention` is
> registered before a customer implementation of an
> `IClassMapConvention`, the `IClassMapConvention` will be run first
> because the `Class Stage` is before the `Post Processing Stage`.

Field or Property Level Serialization Options
---------------------------------------------

There are many ways you can control serialization. The previous section
discussed conventions, which are a convenient way to control
serialization decisions for many classes at once. You can also control
serialization at the individual class or field or property level using
code to configure the class and member maps or using attributes to
decorate the class and members. For each aspect of serialization you can
control, we will be showing both ways.

### Opt-In

A majority of classes will have their properties mapped automatically.
There are some circumstances where this does not happen. For instance,
if your property is read-only, it will not get included in the
automapping of a class by default. In order to include the member, you
can use the `BsonElementAttribute`.

``` {.sourceCode .csharp}
public class MyClass {
    private readonly string _someProperty;

    [BsonElement]
    public string SomeProperty
    {
        get { return _someProperty; }
    }
}
```

The same result can be achieved without using attributes with the
following initialization code:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.MapProperty(c => c.SomeProperty);
});
```

> **note**
>
> When a readonly property is serialized, it value is persisted to the
> database, but never read back out. This is useful for storing
> "computed" properties

### Element name

To specify an element name using attributes, write:

``` {.sourceCode .csharp}
public class MyClass {
    [BsonElement("sp")]
    public string SomeProperty { get; set; }
}
```

The same result can be achieved without using attributes with the
following initialization code:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.GetMemberMap(c => c.SomeProperty).SetElementName("sp");
});
```

Note that we are first automapping the class and then overriding one
particular piece of the class map. If you didn't call `AutoMap` first
then GetMemberMap would throw an exception because there would be no
member maps.

### Element Order

If you want precise control over the order of the elements in the BSON
document you can use the `Order` named parameter to the `BsonElement`
attribute:

``` {.sourceCode .csharp}
public class MyClass {
    [BsonElement("sp", Order = 1)]
    public string SomeProperty { get; set; }
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.GetMemberMap(c => c.SomeProperty).SetElementName("sp").SetOrder(1);
});
```

Any fields or properties that do not have an explicit `Order` will occur
after those that do have an `Order`.

### Identifying the Id Field or Property

To identify which field or property of a class is the Id you can write:

``` {.sourceCode .csharp}
public class MyClass {
    [BsonId]
    public string SomeProperty { get; set; }
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.SetIdMember(cm.GetMemberMap(c => c.SomeProperty));
});
```

When not using `AutoMap`, you can also map a field or property and
identify it as the Id in one step as follows:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.MapIdProperty(c => c.SomeProperty);
    // mappings for other fields and properties
});
```

> **note**
>
> The default conventions will discover a public property or field with
> the name "Id", "id", or "\_id". It is generally unnecessary to
> decorate this field with an attribute or map it explicitly.

### Selecting an IdGenerator to Use for an Id Field or Property

When you Insert a document the C\# driver checks to see if the `Id`
member has been assigned a value, and if not, generates a new unique
value for it. Since the `Id` member can be of any type, the driver
requires the help of a matching `IdGenerator` to check whether the `Id`
has a value assigned to it and to generate a new value if necessary. The
driver has the following `IdGenerators` built-in:

-   `BsonObjectIdGenerator`
-   `CombGuidGenerator`
-   `GuidGenerator`
-   `NullIdChecker`
-   `ObjectIdGenerator`
-   `StringObjectIdGenerator`
-   `ZeroIdChecker<T>`

Some of these `IdGenerators` are used automatically for commonly used
`Id` types:

-   `BsonObjectIdGenerator` is used for `BsonObjectId`
-   `GuidGenerator` is used for `Guid`
-   `ObjectIdGenerator` is used for `ObjectId`
-   `StringObjectIdGenerator` is used for strings represented externally
    as `ObjectId`

To select an `IdGenerator` to use for your `Id` field or property write:

``` {.sourceCode .csharp}
public class MyClass {
    [BsonId(IdGenerator = typeof(CombGuidGenerator))]
    public Guid Id { get; set; }
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.IdMemberMap.SetIdGenerator(CombGuidGenerator.Instance);
});
```

You could also say that you want to use the `CombGuidGenerator` for all
`Guids`. In this case you would write:

``` {.sourceCode .csharp}
BsonSerializer.RegisterIdGenerator(
    typeof(Guid),
    CombGuidGenerator.Instance
);
```

The `NullIdChecker` and `ZeroIdChecker<T>` IdGenerators can be used when
you don't have an `IdGenerator` for an `Id` type but you want to enforce
that the `Id` is not null or zero. These pseudo-IdGenerators throw an
exception if their `GenerateId` method is called. You can select it for
an individual member just like a `CombGuidGenerator` was selected in the
previous example, or you can turn on one or both of these `IdGenerators`
for all types as follows:

``` {.sourceCode .csharp}
BsonSerializer.UseNullIdChecker = true; // used for reference types
BsonSerializer.UseZeroIdChecker = true; // used for value types
```

> **note**
>
> In version 1.0 of the C\# Driver `NullIdChecker` and
> `ZeroIdChecker<T>` were always used, but it was decided that their use
> should be optional, since null and zero are valid values for an `Id`
> as far as the server is concerned, so they should only be considered
> an error if the developer has specifically said they should be.

### Ignoring a Field or Property

When constructing a class map manually you can ignore a field or
property simply by not adding it to the class map. When using `AutoMap`
you need a way to specify that a field or property should be ignored. To
do so using attributes write:

``` {.sourceCode .csharp}
public class MyClass {
    [BsonIgnore]
    public string SomeProperty { get; set; }
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.UnmapProperty(c => c.SomeProperty);
});
```

In this case `AutoMap` will have initially added the property to the
class map automatically but then `UnmapProperty` will remove it.

### Ignoring `null` Values

By default `null` values are serialized to the BSON document as a BSON
`Null`. An alternative is to serialize nothing to the BSON document when
the field or property has a `null` value. To specify this using
attributes write:

``` {.sourceCode .csharp}
public class MyClass {
    [BsonIgnoreIfNull]
    public string SomeProperty { get; set; }
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.GetMemberMap(c => c.SomeProperty).SetIgnoreIfNull(true);
});
```

### Default Values

You can specify a default value for a field or property as follows:

``` {.sourceCode .csharp}
public class MyClass {
    [BsonDefaultValue("abc")]
    public string SomeProperty { get; set; }
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.GetMemberMap(c => c.SomeProperty).SetDefaultValue("abc");
});
```

You can also control whether default values are serialized or not (the
default is yes). To not serialize default values using attributes write:

``` {.sourceCode .csharp}
public class MyClass {
    [BsonDefaultValue("abc")]
    [BsonIgnoreIfDefault]
    public string SomeProperty { get; set; }
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.GetMemberMap(c => c.SomeProperty)
        .SetDefaultValue("abc")
        .SetIgnoreIfDefault(true);
});
```

### Ignoring a Member Based on a *ShouldSerializeXyz* Method

Sometimes the decision whether to serialize a member or not is more
complicated than just whether the value is `null` or equal to the
default value. You can write a method that determines whether a value
should be serialized. Usually the method for member *Xyz* is named
*ShouldSerializeXyz*. If you follow this naming convention then
`AutoMap` will automatically detect the method and use it. For example:

``` {.sourceCode .csharp}
public class Employee {
    public ObjectId Id { get; set; }
    [BsonDateTimeOptions(DateOnly = true)]
    public DateTime DateOfBirth { get; set; }

    public bool ShouldSerializeDateOfBirth() {
        return DateOfBirth > new DateTime(1900, 1, 1);
    }
}
```

Or using initialization code instead of naming conventions:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<Employee>(cm => {
    cm.AutoMap();
    cm.GetMemberMap(c => c.DateOfBirth).SetShouldSerializeMethod(
        obj => ((Employee) obj).DateOfBirth > new DateTime(1900, 1, 1)
    );
});
```

### Identifying Required Fields

Normally, the deserializer doesn't care if the document being
deserialized doesn't have a matching element for every field or property
of the class. The members that don't have a matching element simply get
assigned their default value.

If you want to make an element in the document be required, you can mark
an individual field or property like this:

``` {.sourceCode .csharp}
public class MyClass {
    public ObjectId Id { get; set; }
    [BsonRequired]
    public string X { get; set; }
}
```

Or using initialization code instead attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.GetMemberMap(c => c.X).SetIsRequired(true);
});
```

> **warning**
>
> This will throw an exception during deserialization. The required
> state of a member map does not apply to serialization.

### Specifying the Serializer

There are times when a specific serializer needs to be used rather than
letting the Bson library choose. This can be done in a couple of ways:

``` {.sourceCode .csharp}
public class MyClass {
    public ObjectId Id { get; set; }
    [BsonSerializer(typeof(MyCustomStringSerializer))]
    public string X { get; set; }
}
```

Or using initialization code instead attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.GetMemberMap(c => c.X).SetSerializer(new MyCustomStringSerializer());
});
```

### Serialization Options

Serialization of some classes can be more finely controlled using
serialization options (which are represented using classes that
implement the `IBsonSerializationOptions` interface). Whether a class
uses serialization options or not, and which ones, depends on the
particular class involved. The following sections describe the available
serialization option classes and the classes that use them.

#### DateTimeSerializationOptions

These serialization options control how a `DateTime` is serialized. For
example:

``` {.sourceCode .csharp}
public class MyClass {
    [BsonDateTimeOptions(DateOnly = true)]
    public DateTime DateOfBirth { get; set; }
    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime AppointmentTime { get; set; }
}
```

Here we are specifying that the `DateOfBirth` value holds a date only
(so the `TimeOfDay` component must be zero). Additionally, because this
is a date only, no timezone conversions at all will be performed. The
`AppointmentTime` value is in local time and will be converted to UTC
when it is serialized and converted back to local time when it is
deserialized.

You can specify the same options using initialization code instead of
attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.GetMemberMap(c => c.DateOfBirth)
        .SetSerializationOptions(
            new DateTimeSerializationOptions { DateOnly = true });
    cm.GetMemberMap(c => c.AppointmentTime)
        .SetSerializationOptions(
            new DateTimeSerializationOptions { Kind = DateTimeKind.Local });
});
```

`DateTimeSerializationOptions` are supported by the serializers for the
following classes: `BsonDateTime` and `DateTime`.

#### DictionarySerializationOptions

When serializing dictionaries there are several alternative ways that
the contents of the dictionary can be represented. The different ways
are represented by the `DictionaryRepresentation` enumeration:

``` {.sourceCode .csharp}
public enum DictionaryRepresentation {
    Dynamic,
    Document,
    ArrayOfArrays,
    ArrayOfDocuments
}
```

A dictionary represented as a `Document` will be stored as a
`BsonDocument`, and each entry in the dictionary will be represented by
a `BsonElement` with the name equal to the key of the dictionary entry
and the value equal to the value of the dictionary entry. This
representation can only be used when all the keys in a dictionary are
strings that are valid element names.

A dictionary represented as an `ArrayOfArrays` will be stored as a
`BsonArray` of key/value pairs, where each key/value pair is stored as a
nested two-element `BsonArray` where the two elements are the key and
the value of the dictionary entry. This representation can be used even
when the keys of the dictionary are not strings. This representation is
very general and compact, and is the default representation when
`Document` does not apply. One problem with this representation is that
it is difficult to write queries against it, which motivated the
introduction in the 1.2 version of the driver of the `ArrayOfDocuments`
representation.

A dictionary represented as an `ArrayOfDocuments` will be stored as a
`BsonArray` of key/value pairs, where each key/value pair is stored as a
nested two-element `BsonDocument` of the form `{ k : key, v : value }`.
This representation is just as general as the `ArrayOfArrays`
representation, but because the keys and values are tagged with element
names it is much easier to write queries against it. For backward
compatibility reasons this is not the default representation.

If the `Dynamic` representation is specified, the dictionary key values
are inspected before serialization, and if all the keys are strings
which are also valid element names, then the `Document` representation
will be used, otherwise the `ArrayOfArrays` representation will be used.

If no other representation for a dictionary is specified, then `Dynamic`
is assumed.

You can specify a `DictionarySerializationOption` as follows:

``` {.sourceCode .csharp}
public class C {
    public ObjectId Id;
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
    public Dictionary<string, int> Values;
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<C>(cm => {
    cm.AutoMap();
    cm.GetMemberMap(c => c.Values)
        .SetSerializationOptions(DictionarySerializationOptions.ArrayOfDocuments);
});
```

`DictionarySerializationOptions` are supported by the serializers for
the following classes: the generic classes and interfaces `Dictionary`,
`IDictionary`, `SortedDictionary` and `SortedList`, and the non-generic
classes and interfaces `Hashtable`, `IDictionary`, `ListDictionary`,
`OrderedDictionary` and `SortedList`.

#### RepresentationSerializationOptions

For some .NET primitive types you can control what BSON type you want
used to represent the value in the BSON document. For example, you can
specify whether a char value should be represented as a BSON Int32 or as
a one-character BSON String:

``` {.sourceCode .csharp}
public class MyClass {
    [BsonRepresentation(BsonType.Int32)]
    public char RepresentAsInt32 { get; set; }
    [BsonRepresentation(BsonType.String)]
    public char RepresentAsString { get; set; }
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.GetMemberMap(c => c.RepresentAsInt32)
        .SetRepresentation(BsonType.Int32);
    cm.GetMemberMap(c => c.RepresentAsString)
        .SetRepresentation(BsonType.String);
});
```

One case that deserves special mention is representing a string
externally as an `ObjectId`. For example:

``` {.sourceCode .csharp}
public class Employee {
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    // other properties
}
```

In this case the serializer will convert the `ObjectId` to a string when
reading data from the database and will convert the string back to an
`ObjectId` when writing data to the database (the string value must be a
valid `ObjectId`). Typically this is done when you want to keep your
domain classes free of any dependencies on the C\# driver, so you don't
want to declare the `Id` as an `ObjectId`. String serves as a neutral
representation that is at the same time easily readable for debugging
purposes. To keep your domain classes free of dependencies on the C\#
driver you also won't want to use attributes, so you can accomplish the
same thing using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<Employee>(cm => {
    cm.AutoMap();
    cm.IdMemberMap.SetRepresentation(BsonType.ObjectId);
});
```

Class Level Serialization Options
---------------------------------

There are several serialization options that are related to the class
itself instead of to any particular field or property. You can set these
class level options either by decorating the class with serialization
related attributes or by writing initialization code. As usual, we will
show both ways in the examples.

### Ignoring Extra Elements

When a BSON document is deserialized the name of each element is used to
look up a matching field or property in the class map. Normally, if no
matching field or property is found, an exception will be thrown. If you
want to ignore extra elements during deserialization, use the following
attribute:

``` {.sourceCode .csharp}
[BsonIgnoreExtraElements]
public MyClass {
    // fields and properties
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.SetIgnoreExtraElements(true);
});
```

### Supporting Extra Elements

You can design your class to be capable of handling any extra elements
that might be found in a BSON document during deserialization. To do so,
you must have a property of type `BsonDocument` and you must identify
that property as the one that should hold any extra elements that are
found (or you can name the property "ExtraElements" so that the default
`ExtraElementsMemberConvention` will find it automatically). For
example:

``` {.sourceCode .csharp}
public MyClass {
    // fields and properties
    [BsonExtraElements]
    public BsonDocument CatchAll { get; set; }
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.SetExtraElementsMember(cm.GetMemberMap(c => c.CatchAll));
});
```

When a BSON document is deserialized any extra elements found will be
stored in the extra elements `BsonDocument` property. When the class is
serialized the extra elements will be serialized also. One thing to note
though is that the serialized class will probably not have the elements
in exactly the same order as the original document. All extra elements
will be serialized together when the extra elements member is
serialized.

### Polymorphic Classes and Discriminators

When you have a class hierarchy and will be serializing instances of
varying classes to the same collection you need a way to distinguish one
from another. The normal way to do so is to write some kind of special
value (called a "discriminator") in the document along with the rest of
the elements that you can later look at to tell them apart. Since there
are potentially many ways you could discriminate between actual types,
the default serializer uses conventions for discriminators. The default
serializer provides two standard discriminators:
`ScalarDiscriminatorConvention` and
`HierarchicalDiscriminatorConvention`. The default is the
`HierarchicalDiscriminatorConvention`, but it behaves just like the
`ScalarDiscriminatorConvention` until certain options are set to trigger
its hierarchical behavior (more on this later).

The default discriminator conventions both use an element named `_t` to
store the discriminator value in the BSON document. This element will
normally be the second element in the BSON document (right after the
`_id`). In the case of the `ScalarDiscriminatorConvention` the value of
`_t` will be a single string. In the case of the
`HierarchicalDiscriminatorConvention` the value of `_t` will be an array
of discriminator values, one for each level of the class inheritance
tree (again, more on this later).

While you will normally be just fine with the default discriminator
convention, you might have to write a custom discriminator convention if
you must inter-operate with data written by another driver or object
mapper that uses a different convention for its discriminators.

### Setting the Discriminator Value

The default value for the discriminator is the name of the class
(without the namespace part). You can specify a different value using
attributes:

``` {.sourceCode .csharp}
[BsonDiscriminator("myclass")]
public MyClass {
    // fields and properties
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<MyClass>(cm => {
    cm.AutoMap();
    cm.SetDiscriminator("myclass");
});
```

### Specifying Known Types

When deserializing polymorphic classes it is important that the
serializer know about all the classes in the hierarchy before
deserialization begins. If you ever see an error message about an
"Unknown discriminator" it is because the deserializer can't figure out
the class for that discriminator. If you are mapping your classes
programmatically simply make sure that all classes in the hierarchy have
been mapped before beginning deserialization. When using attributes and
automapping you will need to inform the serializer about known types
(i.e. subclasses) it should create class maps for. Here is an example of
how to do this:

``` {.sourceCode .csharp}
[BsonKnownTypes(typeof(Cat), typeof(Dog)]
public class Animal {
}

[BsonKnownTypes(typeof(Lion), typeof(Tiger)]
public class Cat : Animal {
}

public class Dog : Animal {
}

public class Lion : Cat {
}

public class Tiger : Cat {
}
```

The `BsonKnownTypes` attribute lets the serializer know what subclasses
it might encounter during deserialization, so when `Animal` is
automapped the serializer will also automap `Cat` and `Dog` (and
recursively, `Lion` and `Tiger` as well).

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<Animal>();
BsonClassMap.RegisterClassMap<Cat>();
BsonClassMap.RegisterClassMap<Dog>();
BsonClassMap.RegisterClassMap<Lion>();
BsonClassMap.RegisterClassMap<Tiger>();
```

### Scalar and Hierarchical Discriminators

Normally a discriminator is simply the name of the class (although it
could be different if you are using a custom discriminator convention or
have explicitly specified a discriminator for a class). So a collection
containing a mix of different type of `Animal` documents might look
like:

``` {.sourceCode .csharp}
{ _t : "Animal", ... }
{ _t : "Cat", ... }
{ _t : "Dog", ... }
{ _t : "Lion", ... }
{ _t : "Tiger", ... }
```

Sometimes it can be helpful to record a hierarchy of discriminator
values, one for each level of the hierarchy. To do this, you must first
mark a base class as being the root of a hierarchy, and then the default
`HierarchicalDiscriminatorConvention` will automatically record
discriminators as array values instead.

To identify `Animal` as the root of a hierarchy use the
`BsonDiscriminator` attribute with the `RootClass` named parameter:

``` {.sourceCode .csharp}
[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(Cat), typeof(Dog)]
public class Animal {
}

// the rest of the hierarchy as before
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<Animal>(cm => {
    cm.AutoMap();
    cm.SetIsRootClass(true);
});
BsonClassMap.RegisterClassMap<Cat>();
BsonClassMap.RegisterClassMap<Dog>();
BsonClassMap.RegisterClassMap<Lion>();
BsonClassMap.RegisterClassMap<Tiger>();
```

Now that you have identified `Animal` as a root class, the discriminator
values will look a little bit different:

``` {.sourceCode .csharp}
{ _t : "Animal", ... }
{ _t : ["Animal", "Cat"], ... }
{ _t : ["Animal", "Dog"], ... }
{ _t : ["Animal", "Cat", "Lion"], ... }
{ _t : ["Animal", "Cat", "Tiger"], ... }
```

The main reason you might choose to use hierarchical discriminators is
because it makes it possibly to query for all instances of any class in
the hierarchy. For example, to read all the `Cat` documents we can
write:

``` {.sourceCode .csharp}
var query = Query.EQ("_t", "Cat");
var cursor = collection.FindAs<Animal>(query);
foreach (var cat in cursor) {
    // process cat
}
```

This works because of the way MongoDB handles queries against array
values.

Customizing Serialization
-------------------------

There are several ways you can customize serialization:

1.  Implement `ISupportInitialize`.
2.  Make a class responsible for its own serialization.
3.  Supplementing the default serializer.
4.  Write a custom serializer.
5.  Write a custom attribute.
6.  Write a custom Id generator.
7.  Write a custom convention.

### Implementing `ISupportInitialize`

The driver respects an entity implementing `ISupportInitialize` which
contains 2 methods, `BeginInit` and `EndInit`. These method are called
before deserialization begins and after it is complete. It is useful for
running operations before or after deserialization such as handling
schema changes are pre-calculating some expensive operations.

### Supplementing the Default Serializer Provider

You can register your own serialization provider to supplement the
default serializer. Register it like this:

``` {.sourceCode .csharp}
IBsonSerializationProvider myProvider;
BsonSerializer.RegisterSerializationProvider(myProvider);
```

You should register your provider as early as possible. Your provider
will be called first before the default serializer. You can delegate
handling of any types your custom provider isn't prepared to handle to
the default serializer by returning null from `GetSerializer`.

### Write a Custom Serializer

A custom serializer can handle serialization of your classes without
requiring any changes to those classes. This is a big advantage when you
either don't want to modify those classes or can't (perhaps because you
don't have control over them). You must register your custom serializer
so that the BSON Library knows of its existence and can call it when
appropriate.

If you write a custom serializer you will have to become familiar with
the `BsonReader` and `BsonWriter` abstract classes, which are not
documented here, but are relatively straightforward to use. Look at the
existing serializers in the driver for examples of how `BsonReader` and
`BsonWriter` are used.

To implement and register a custom serializer you would:

``` {.sourceCode .csharp}
// MyClass is the class for which you are writing a custom serializer
public MyClass {
}

// MyClassSerializer is the custom serializer for MyClass
public MyClassSerializer : IBsonSerializer {
    // implement Deserialize
    // implement GetDefaultSerializationOptions
    // implement Serialize
}

// register your custom serializer
BsonSerializer.RegisterSerializer(
    typeof(MyClass),
    new MyClassSerializer()
);
```

You can also decorate the target class with a `BsonSerializer` attribute
instead of using the `BsonSerializer.RegisterSerializer` method:

``` {.sourceCode .csharp}
[BsonSerializer(typeof(MyClassSerializer))]
public MyClass {
}
```

The `IBsonSerializer` interface is all that is necessary for
serialization. However, there are some extension interfaces that will
enable further use in other parts of the api such as saving a class or
LINQ.

If your class is used as a root document, you will need to implement the
`IBsonIdProvider` interface in order for "Saving" the document to
function. `MongoCollection.Save` requires a document identity in order
to know if it should generate an insert or update statement. Below is
the extension to the above `MyClassSerializer`.

``` {.sourceCode .csharp}
public MyClassSerializer : IBsonSerializer, IBsonIdProvider {
    // ...

    // implement GetDocumentId
    // implement SetDocumentId
}
```

In order to enable LINQ to properly construct type-safe queries using a
custom serializer, it needs access to member information or array
information. If your custom serializer is for a class, as
`MyClassSerializer` is above, then you should implement
`IBsonDocumentSerializer`.

``` {.sourceCode .csharp}
public MyClassSerializer : IBsonSerializer, IBsonDocumentSerializer {
    // ...

    // implement GetMemberSerializationInfo
}
```

If, however, your class is a collection that should be serialized as an
array, it should implement `IBsonArraySerializer`.

``` {.sourceCode .csharp}
public MyClassSerializer : IBsonSerializer, IBsonArraySerializer {
    // ...

    // implement GetItemSerializationInfo
}
```

To debug a custom serializer you can either Insert a document containing
a value serialized by your custom serializer into some collection and
then use the mongo shell to examine what the resulting document looks
like. Alternatively you can use the `ToJson` method to see the result of
the serializer without having to Insert anything into a collection as
follows:

``` {.sourceCode .csharp}
// assume a custom serializer has been registered for class C
var c = new C();
var json = c.ToJson();
// inspect the json string variable to see how c was serialized
```

### Write a Custom Attribute

The auto mapping ability of BSON library utilizes attributes that
implement `IBsonClassMapAttribute`, `IBsonMemberMapAttribute`, or
`IBsonCreatorMapAttribute` for class level attributes, member level
attributes, or creator level attributes respectively. Each of these
interfaces has a single method called Apply that is passed a
`BsonClassMap`, a `BsonMemberMap`, or a `BsonCreatorMap` which it can
modify using public properties and methods. One example of this would be
to create an attribute called `BsonEncryptionAttribute` that is used to
encrypt a string before sending it to the database and decrypt it when
reading it back out.

View the existing attributes for examples of how these interfaces
function.

### Write a custom Id generator

You can write your own `IdGenerator`. For example, suppose you wanted to
generate integer Employee Ids:

``` {.sourceCode .csharp}
public class EmployeeIdGenerator : IIdGenerator {
    // implement GenerateId
    // implement IsEmpty
}
```

You can specify that this generator be used for Employee Ids using
attributes:

``` {.sourceCode .csharp}
public class Employee {
    [BsonId(IdGenerator = typeof(EmployeeIdGenerator)]
    public int Id { get; set; }
    // other fields or properties
}
```

Or using initialization code instead of attributes:

``` {.sourceCode .csharp}
BsonClassMap.RegisterClassMap<Employee >(cm => {
    cm.AutoMap();
    cm.IdMember.SetIdGenerator(new EmployeeIdGenerator());
});
```

Alternatively, you can get by without an Id generator at all by just
assigning a value to the `Id` property before calling `Insert` or
`Save`.

### Write a Custom Convention

Earlier in this tutorial we discussed replacing one or more of the
default conventions. You can either replace them with one of the
provided alternatives or you can write your own convention. Writing your
own convention varies slightly from convention to convention.

As an example, we will write a custom convention to name all the
elements the corresponding lower-case version of the member name. We can
implement this convention as follows:

``` {.sourceCode .csharp}
public class LowerCaseElementNameConvention : IMemberMapConvention {
    public void Apply(BsonMemberMap memberMap) {
        memberMap.SetElementName(memberMap.MemberName.ToLower());
    }
}
```

When you are doing one-off conventions like this, it might be easier to
create them with a simple lambda expresion instead. For example:

``` {.sourceCode .csharp}
var pack = new ConventionPack();
pack.AddMemberMapConvention(
    "LowerCaseElementName",
    m => m.SetElementName(m.MemberName.ToLower()));
```

For the best examples of writing custom conventions, it is good to
consult the source for the pre-packaged conventions.

Handling Schema Changes
-----------------------

Just because MongoDB is schema-less does not mean that your code can
handle a schema-less document. Most likely, if you are using a
statically typed language like C\# or VB.NET, then your code is
not-flexible and needs to be mapped to a known schema.

There are a number of different ways that a schema can change from one
version of your application to the next.

1.  A new member is added.
2.  A member is deleted.
3.  A member is renamed.
4.  The type of a member is changed.
5.  The representation of a member is changed.

How you handle these is up to you. There primary two different
strategies.

1.  Write an upgrade script.
2.  Incrementally update your documents as they are used.

The easiest and most bullet-proof of the strategies is to write an
upgrade script. There is effectively no difference to this method
between a relational database (SQL Server, Oracle) and MongoDB. Identify
the documents that need to be changed and update them.

Alternatively, and not supportable in most relational databases, is the
incremental upgrade. The idea is that your documents get updated as they
are used. Documents that are never used never get updated. Because of
this, there are some definite pitfalls you will need to be aware of.

First, queries against a schema where half the documents are version 1
and half the documents are version 2 could go awry. For instance, if you
rename an element, then your query will need to test both the old
element name and the new element name to get all the results.

Second, any incremental upgrade code must stay in the code-base until
all the documents have been upgraded. For instance, if there have been 3
versions of a document, [1, 2, and 3] and we remove the upgrade code
from version 1 to version 2, any documents that still exist as version 1
are un-upgradeable.

So, with that being said, let's talk about handling the schema change
variations.

### A Member Has Been Added

When a new member is added to an entity, there is nothing that needs to
be done other than restarting the application if you are using the auto
mapping features. If not, then you will manually need to map the member
in the same way all the other members are getting mapped.

Existing documents will not have this element and it will show up in
your class with its default value. You can, of course, specify a default
value.

### A Member Has Been Removed

When a member has been removed from am entity, it will continue to exist
in the documents. The serializer will throw an exception when this
element is seen because it doesn't know what to do with it. The 2
previously discussed items that can be used to combat this are the
`BsonIgnoreExtraElements` class-level attribute and the `ExtraElements`
members.

### A Member Is Renamed

When a member has been renamed, it will exist in old documents with the
old name and in new documents with the new name. The way to handle
incremental upgrades for this rename would be to implement an
`ExtraElements` member in conjunction with `ISupportInitialize`. For
example, let's say that a class used to have a `Name` property which has
now been split into a `FirstName` and a `LastName` property.

``` {.sourceCode .csharp}
public class MyClass : ISupportInitialize {
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [BsonExtraElements]
    public IDictionary<string, object> ExtraElements { get; set; }

    void ISupportInitialize.BeginInit() {
        // nothing to do at begin
    }

    void ISupportInitialize.EndInit() {
        object nameValue;
        if(!ExtraElements.TryGetValue("Name", out nameValue)) {
            return;
        }

        var name = (string)nameValue;

        // remove the Name element so that it doesn't get persisted back to the database
        ExtraElements.Remove("Name");

        // assuming all names are "First Last"
        var nameParts = name.Split(' ');

        FirstName = nameParts[0];
        LastName = nameParts[1];
    }
}
```

### The Type of a Member Is Changed

If the .NET type is compatible with the old type (an integer is changed
to a double), then everything will continue to work. Otherwise, a custom
serializer or a migration script will be required.

### The Representation of a Member Is Changed

If the representation of a member is changed and the representations are
compatible, then everything will continue to work. Otherwise, a custom
serializer or a migration script will be required.
