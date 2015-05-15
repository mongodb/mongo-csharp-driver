+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Mapping Classes"
[menu.main]
  parent = "BSON"
  identifier = "Mapping Classes"
  weight = 40
  pre = "<i class='fa'></i>"
+++

## Mapping Classes

Using a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) will work when a schema is fluid and dynamic. However, most applications are built with a schema modeled in the application itself rather than the database. In these cases, it is likely that the application uses classes.

The .NET BSON library supports mapping these classes to and from BSON/JSON using a [`BsonClassMap`]({{< apiref "T_MongoDB_Bson_Serialization_BsonClassMap_1" >}}).


## Creating a Class Map

In a majority of cases, the driver will be able to automatically map your class for you. This will happen if you begin to use a class for which no [serializer]({{< relref "reference\bson\serialization.md#serializers" >}}) has yet been registered in the [serializer registry]({{< relref "reference\bson\serialization.md#serializer-registry" >}}).

You can choose to register the class map using the [`RegisterClassMap`]({{< apiref "M_MongoDB_Bson_Serialization_BsonClassMap_RegisterClassMap__1_1" >}}) method:

```csharp
BsonClassMap.RegisterClassMap<MyClass>();
```

{{% note %}}It is very important that the registration of class maps occur prior to them being needed. The best place to register them is at app startup prior to initializing a connection with MongoDB.{{% /note %}}

If you want to control the creation of the class map, you can provide your own initialization code in the form of a lambda expression:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.MapMember(c => c.SomeProperty);
    cm.MapMember(c => c.AnotherProperty);
});
```

When your lambda expression is executed, the `cm` (short for class map) parameter is passed an empty class map for you to fill in. In this example, two properties are added to the class map by calling the [`MapMember`]({{< apiref "M_MongoDB_Bson_Serialization_BsonClassMap_1_MapMember__1" >}}) method. The arguments to the method are themselves lambda expressions which identify the member of the class. The advantage of using a lambda expression instead of just a string parameter with the name of the property is that Intellisense and compile time checking ensure that you can’t misspell the name of the property.

## AutoMap

It is also possible to use automapping and then override some of the results using the [`AutoMap`]({{< apiref "M_MongoDB_Bson_Serialization_BsonClassMap_AutoMap" >}}) method. This method should be called first in the lambda expression.

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
	cm.AutoMap();
    cm.MapMember(c => c.SomeProperty);
    cm.MapMember(c => c.AnotherProperty);
});
```

[`AutoMap`]({{< apiref "M_MongoDB_Bson_Serialization_BsonClassMap_AutoMap" >}}) uses conventions to map the class and its members. See the [convention documentation]({{< relref "reference\bson\mapping\conventions.md" >}}) for more information.


## Class Customization

There are several serialization options that are related to the class itself instead of to any particular field or property. You can set these class level options either by decorating the class with serialization related attributes or by writing initialization code. 


### Ignoring Extra Elements

When a BSON document is deserialized, the name of each element is used to look up a matching member in the class map. Normally, if no matching member is found, an exception will be thrown. If you want to ignore extra elements during deserialization, use a [`BsonIgnoreExtraElementsAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonIgnoreExtraElementsAttribute" >}}):

```csharp
[BsonIgnoreExtraElements]
public MyClass 
{
    // fields and properties
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.SetIgnoreExtraElements(true);
});
```

{{% note class="important" %}}When you ignore extra elements, if the class is rendered back to BSON, those extra elements will not exist and may be lost forever.{{% /note %}}


### Supporting Extra Elements

You can design your class to be capable of handling any extra elements that might be found in a BSON document during deserialization. To do so, you must have a property of type [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}) (or [`IDictionary<string, object>`]({{< msdnref "s4ys34ea" >}})) and you must identify that property as the one that should hold any extra elements that are found. By [convention]({{< relref "reference\bson\mapping\conventions.md" >}}), the member may be named `ExtraElements`. For example:

```csharp
public MyClass 
{
    // fields and properties
    [BsonExtraElements]
    public BsonDocument CatchAll { get; set; }
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.MapExtraElementsMember(c => c.CatchAll);
});
```

When a BSON document is deserialized, any extra elements found will be stored in the extra elements property. When the class is serialized, the extra elements will be serialized also. One thing to note though is that the serialized class will probably not have the elements in exactly the same order as the original document. All extra elements will be serialized together when the extra elements member is serialized.


### Discriminators

See the [polymorphism]({{< relref "reference\bson\mapping\polymorphism.md" >}}) section for documentation on discriminators and polymorphism.

To specify a discriminator, use a [`BsonDiscriminatorAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonDiscriminatorAttribute" >}}):

```csharp
[BsonDiscriminator("myclass")]
public MyClass 
{
    // fields and properties
}
```

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.SetDiscriminator("myclass");
});
```


### ISupportInitialize

If your class implements [`ISupportInitialize`]({{< msdnref "system.componentmodel.isupportinitialize" >}}), the driver will call the [`BeginInit`]({{< msdnref "system.componentmodel.isupportinitialize.begininit" >}}) method before deserialization and the [`EndInit`]({{< msdnref "system.componentmodel.isupportinitialize.endinit" >}}) method upon completion. It is useful for running operations before or after deserialization such as handling schema changes are pre-calculating some expensive operations.


## Creation Customization

By default, classes must contain a no-argument constructor that will be used to instantiate the class to rehydrate. However, it is possible to configure a constructor whose arguments are correlated with mapped properties or fields. There are a couple of ways to do this.

Using an expression, you can instruct the driver to use a creator map as follows:

```csharp
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

Parsing the expression tree correlates the first constructor argument with the FirstName property and the second constructor argument with the LastName property. There are other, more complicated ways of handling this which can be explored on your own should the need arise.

Using attributes instead:

```csharp
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

By default, a convention runs on every CreatorMap with no mapped arguments and attempts to correlate the names of the constructor arguments with the names of mapped members. If your names differ in more than just case, there are overloads of BsonConstructor which can be used to explicity tell the driver which members to use.

When more than one constructor is found, we will use the constructor that has the most matching parameters. For example:

```csharp
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

If the document in the database has a BirthDate element, we will choose to use the constructor with three parameters because it is more specific.

## Member Customization

You can also control serialization at the individual class or field or property level using code to configure the class and member maps or using attributes to decorate the class and members. For each aspect of serialization you can control, we will be showing both ways.


### Opt-In

A majority of classes will have their members [mapped automatically]({{< relref "#automap" >}}). There are some circumstances where this does not happen. For instance, if your property is read-only, it will not get included in the automapping of a class by default. In order to include the member, you can use the [`BsonElementAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonElementAttribute" >}}).

```csharp
class MyClass
{
	private readonly string _someProperty;

	[BsonElement]
	public string SomeProperty
	{
		get { return _someProperty; }
	}
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
	cm.AutoMap();
    cm.MapProperty(c => c.SomeProperty);
});
```

{{% note %}}When a readonly property is serialized, it value is persisted to the database, but never read back out. This is useful for storing “computed” properties.{{% /note %}}


### Element Name

To specify an element name using attributes, write:

```csharp
public class MyClass 
{
    [BsonElement("sp")]
    public string SomeProperty { get; set; }
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.MapMember(c => c.SomeProperty).SetElementName("sp");
});
```


### Element Order

If you want precise control over the order of the elements in the BSON document, you can use the Order named parameter to the BsonElement attribute:

```csharp
public class MyClass 
{
    [BsonElement("sp", Order = 1)]
    public string SomeProperty { get; set; }
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.MapMember(c => c.SomeProperty).SetElementName("sp").SetOrder(1);
});
```

Any fields or properties that do not have an explicit Order will occur after those that do have an Order.


### The Id Member

By [convention]({{< relref "reference\bson\mapping\conventions.md" >}}), a public member called `Id`, `id`, or `_id` will be used as the identifier. You can be specific about this using the [`BsonIdAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonIdAttribute" >}}).

```csharp
public class MyClass 
{
    [BsonId]
    public string SomeProperty { get; set; }
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.MapIdMember(c => c.SomeProperty);
});
```

#### Id Generators

When you Insert a document, the driver checks to see if the `Id` member has been assigned a value and, if not, generates a new unique value for it. Since the `Id` member can be of any type, the driver requires the help of an [`IIdGenerator`]({{< apiref "T_MongoDB_Bson_Serialization_IIdGenerator" >}}) to check whether the `Id` has a value assigned to it and to generate a new value if necessary. The driver has the following Id generators built-in:

- [`ObjectIdGenerator`]({{< apiref "T_MongoDB_Bson_Serialization_IdGenerators_ObjectIdGenerator" >}})
- [`StringObjectIdGenerator`]({{< apiref "T_MongoDB_Bson_Serialization_IdGenerators_StringObjectIdGenerator" >}})
- [`GuidGenerator`]({{< apiref "T_MongoDB_Bson_Serialization_IdGenerators_GuidGenerator" >}})
- [`CombGuidGenerator`]({{< apiref "T_MongoDB_Bson_Serialization_IdGenerators_CombGuidGenerator" >}})
- [`NullIdChecker`]({{< apiref "T_MongoDB_Bson_Serialization_IdGenerators_NullIdChecker" >}})
- [`ZeroIdChecker<T>`]({{< apiref "T_MongoDB_Bson_Serialization_IdGenerators_ZeroIdChecker_1" >}})
- [`BsonObjectIdGenerator`]({{< apiref "T_MongoDB_Bson_Serialization_IdGenerators_BsonObjectIdGenerator" >}})

Some of these Id generators are used automatically for commonly used `Id` types:

- [`GuidGenerator`]({{< apiref "T_MongoDB_Bson_Serialization_IdGenerators_GuidGenerator" >}}) is used for a [`Guid`]({{< msdnref "system.guid" >}})
- [`ObjectIdGenerator`]({{< apiref "T_MongoDB_Bson_Serialization_IdGenerators_ObjectIdGenerator" >}}) is used for an [`ObjectId`]({{< apiref "T_MongoDB_Bson_ObjectId" >}})
- [`StringObjectIdGenerator`]({{< apiref "T_MongoDB_Bson_Serialization_IdGenerators_StringObjectIdGenerator" >}}) is used for a [`string`]({{< msdnref "system.string" >}}) represented externally as [`ObjectId`]({{< apiref "T_MongoDB_Bson_ObjectId" >}})

To specify the Id generator via an attribute:

```csharp
public class MyClass 
{
    [BsonId(IdGenerator = typeof(CombGuidGenerator))]
    public Guid Id { get; set; }
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.MapIdMember(c => c.Id).SetIdGenerator(CombGuidGenerator.Instance);
});
```

You could also say that you want to use the [`CombGuidGenerator`]({{< apiref "T_MongoDB_Bson_Serialization_IdGenerators_CombGuidGenerator" >}}) for all Guids.

```csharp
BsonSerializer.RegisterIdGenerator(
    typeof(Guid),
    CombGuidGenerator.Instance
);
```


### Ignoring a Member

When constructing a class map manually, you can ignore a field or property simply by not adding it to the class map. When using AutoMap, you need a way to specify that a field or property should be ignored. Use the [`BsonIgnoreAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonIgnoreAttribute" >}}):

```csharp
public class MyClass 
{
    [BsonIgnore]
    public string SomeProperty { get; set; }
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.UnmapMember(c => c.SomeProperty);
});
```

When using code, [`AutoMap`]({{< apiref "M_MongoDB_Bson_Serialization_BsonClassMap_AutoMap" >}}) will have initially added the property to the class map automatically. [`UnmapMember`]({{< apiref "M_MongoDB_Bson_Serialization_BsonClassMap_1_UnmapMember__1" >}}) will remove it.


### Ignoring Default Values

By default, default values are serialized to the BSON document. An alternative is to serialize nothing to the BSON document when the member has a default value. For reference types, this value is `null` and for value types, the default is whatever the default is for the value type. Use a [`BsonIgnoreIfDefaultAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonIgnoreIfDefaultAttribute" >}}):

```csharp
public class MyClass 
{
    [BsonIgnoreIfDefault]
    public string SomeProperty { get; set; }
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.MapMember(c => c.SomeProperty).SetIgnoreIfDefault(true);
});
```

### Specifying the Default Value

You can specify a default value for a member using a [`BsonDefaultValueAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonDefaultValueAttribute" >}}):

```csharp
public class MyClass 
{
    [BsonDefaultValue("abc")]
    public string SomeProperty { get; set; }
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.MapMember(c => c.SomeProperty).SetDefaultValue("abc");
});
```

The default value and the ignoring of a default value work together. The following will serialize a `null` value, but not `abc`:

```csharp
public class MyClass 
{
	[BsonIgnoreIfDefault]
    [BsonDefaultValue("abc")]
    public string SomeProperty { get; set; }
}
```

### Ignoring a Member at Runtime

Sometimes the decision whether to serialize a member or not is more complicated than just whether the value is `null` or equal to the default value. In these cases, you can write a method that determines whether a value should be serialized. Usually the method for member Xyz is named ShouldSerializeXyz. If you follow this naming convention then [`AutoMap`]({{< apiref "M_MongoDB_Bson_Serialization_BsonClassMap_AutoMap" >}}) will automatically detect the method and use it. For example:

```csharp
public class Employee 
{
    public ObjectId Id { get; set; }

    [BsonDateTimeOptions(DateOnly = true)]
    public DateTime DateOfBirth { get; set; }

    public bool ShouldSerializeDateOfBirth() 
    {
        return DateOfBirth > new DateTime(1900, 1, 1);
    }
}
```

When using code, it can be specified as a lambda expression:

```csharp
BsonClassMap.RegisterClassMap<Employee>(cm => 
{
    cm.AutoMap();
    cm.MapMember(c => c.DateOfBirth).SetShouldSerializeMethod(
        obj => ((Employee) obj).DateOfBirth > new DateTime(1900, 1, 1)
    );
});
```

### Specifying the Serializer

There are times when a specific serializer needs to be used rather than letting the BSON library choose. This can be done using a [`BsonSerializerAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonSerializerAttribute" >}}):

```csharp
public class MyClass 
{
    public ObjectId Id { get; set; }

    [BsonSerializer(typeof(MyCustomStringSerializer))]
    public string X { get; set; }
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.MapMember(c => c.X).SetSerializer(new MyCustomStringSerializer());
});
```

### Serialization Options

Serialization of some classes can be more finely controlled using serialization options. Whether a class uses serialization options or not, and which ones, depends on the particular class involved. The following sections describe the available serialization option classes and the classes that use them.


#### DateTime Serialization Options

Using a [`BsonDateTimeOptionsAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonDateTimeOptionsAttribute" >}}):

```csharp
public class MyClass 
{
    [BsonDateTimeOptions(DateOnly = true)]
    public DateTime DateOfBirth { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime AppointmentTime { get; set; }
}
```

When done via code, a [`DateTimeSerializer`]({{< apiref "T_MongoDB_Bson_Serialization_Serializers_DateTimeSerializer" >}}) should be set:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.MapMember(c => c.DateOfBirth).SetSerializer(new DateTimeSerializer(dateOnly: true));
    cm.MapMember(c => c.AppointmentTime).SetSerializer(new DateTimeSerializer(DateTimeKind.Local));
});
```

Here we are specifying that the `DateOfBirth` value holds a date only (so the TimeOfDay component will be zero). Additionally, because this is a date only, no timezone conversions at all will be performed. The `AppointmentTime` value is in local time and will be converted to UTC when it is serialized and converted back to local time when it is deserialized.

{{% note %}}DateTime values in MongoDB are always saved as UTC.{{% /note %}}


#### Dictionary Serialization Options

When serializing dictionaries, there are several alternative ways that the contents of the dictionary can be represented. The [`DictionaryRepresentation`]({{< apiref "T_MongoDB_Bson_Serialization_Options_DictionaryRepresentation" >}}) enum indicates the supported methods. Using a [`BsonDictionaryOptionsAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_Attributes_BsonDictionaryOptionsAttribute" >}}):

```csharp
public class C 
{
    public ObjectId Id;
   
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
    public Dictionary<string, int> Values;
}
```

When done via code, a [`DictionaryInterfaceImplementerSerializer`]({{< apiref "T_MongoDB_Bson_Serialization_Serializers_DictionaryInterfaceImplementerSerializer_1" >}}) should be set:

```csharp
BsonClassMap.RegisterClassMap<C>(cm => 
{
    cm.AutoMap();
    cm.MapMember(c => c.Values).SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<string, int>>(DictionaryRepresentation.ArrayOfDocuments));
});
```

The 3 options in the [`DictionaryRepresentation`]({{< apiref "T_MongoDB_Bson_Serialization_Options_DictionaryRepresentation" >}}) enum are as follows:

- `Document`: A dictionary represented as a Document will be stored as a BsonDocument, and each entry in the dictionary will be represented by a BsonElement with the name equal to the key of the dictionary entry and the value equal to the value of the dictionary entry. This representation can only be used when all the keys in a dictionary are strings that are valid element names.

- `ArrayOfArrays`: A dictionary represented as an ArrayOfArrays will be stored as a BsonArray of key/value pairs, where each key/value pair is stored as a nested two-element BsonArray where the two elements are the key and the value of the dictionary entry. This representation can be used even when the keys of the dictionary are not strings. This representation is very general and compact, and is the default representation when Document does not apply. One problem with this representation is that it is difficult to write queries against it, which motivated the introduction in the 1.2 version of the driver of the ArrayOfDocuments representation.

- `ArrayOfDocuments`: A dictionary represented as an ArrayOfDocuments will be stored as a BsonArray of key/value pairs, where each key/value pair is stored as a nested two-element BsonDocument of the form { k : key, v : value }. This representation is just as general as the ArrayOfArrays representation, but because the keys and values are tagged with element names it is much easier to write queries against it. For backward compatibility reasons this is not the default representation.


### Representation

For some .NET primitive types you can control what BSON type you want used to represent the value. For example, you can specify whether a char value should be represented as a BSON Int32 or as a one-character BSON String:

```csharp
public class MyClass 
{
    [BsonRepresentation(BsonType.Int32)]
    public char RepresentAsInt32 { get; set; }

    [BsonRepresentation(BsonType.String)]
    public char RepresentAsString { get; set; }
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<MyClass>(cm => 
{
    cm.AutoMap();
    cm.MapMember(c => c.RepresentAsInt32).SetSerializer(new CharSerializer(BsonType.Int32));
    cm.MapMember(c => c.RepresentAsString).SetSerializer(new CharSerializer(BsonType.String));
});
```

#### ObjectIds

One case that deserves special mention is representing a string externally as an ObjectId. For example:

```csharp
public class Employee 
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
}
```

In this case, the serializer will convert the [`ObjectId`]({{< apiref "T_MongoDB_Bson_ObjectId" >}}) to a `string` when reading data from the database and will convert the `string` back to an [`ObjectId`]({{< apiref "T_MongoDB_Bson_ObjectId" >}}) when writing data to the database (the `string` value must be a valid [`ObjectId`]({{< apiref "T_MongoDB_Bson_ObjectId" >}})). Typically this is done when you want to keep your domain classes free of any dependencies on the driver. To keep your domain classes free of dependencies on the C# driver you also won’t want to use attributes, so you can accomplish the same thing using initialization code instead of attributes:

```csharp
BsonClassMap.RegisterClassMap<Employee>(cm => 
{
    cm.AutoMap();
    cm.IdMemberMap.SetRepresentation(BsonType.ObjectId);
});
```

#### Enums

Another case that deserves mention is enums. Enums are, by default, represented as their underlying value. In other words, a plain enum will be represented as an integer value. However, it is possible to instruct the driver to represent an enum as a string.

```csharp
public enum Color
{
    Blue,
    Other
}

public class Person 
{
    [BsonRepresentation(BsonType.String)]
    public Color FavoriteColor { get; set; }
}
```

Or via code:

```csharp
BsonClassMap.RegisterClassMap<Person>(cm => 
{
    cm.AutoMap();
    cm.MapMember(c => c.FavoriteColor).SetSerializer(new EnumSerializer<Color>(BsonType.String));
});
```

## Custom Attributes

It is possible to implement custom attributes to contribute to the serialization infrastructure. There are 3 interfaces you might want to implement:

- [`IBsonClassMapAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_IBsonClassMapAttribute" >}}) is used to contribute to a class map.
- [`IBsonMemberMapAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_IBsonMemberMapAttribute" >}}) is used to contribute to a member map.
- [`IBsonCreatorMapAttribute`]({{< apiref "T_MongoDB_Bson_Serialization_IBsonCreatorMapAttribute" >}}) is used to contribute to a creator map.

All the provided attributes implement one or more of these interfaces, so they are good examples of how these interfaces function.