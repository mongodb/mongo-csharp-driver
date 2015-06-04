+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Serialization"
[menu.main]
  parent = "BSON"
  identifier = "Serialization"
  weight = 30
  pre = "<i class='fa'></i>"
+++

## Serialization

[Reading and Writing BSON/JSON]({{< relref "reference\bson\bson.md" >}}) demonstrates how to manually read and write BSON and JSON. However, using the serialization classes make this process much easier.

Serialization is the process of mapping an object to and from a BSON document. The architecture is extensible with numerous hooks to allow you to take more control of the process when necessary. 

## Serializer Registry

The serializer registry contains all the [`IBsonSerializers`]({{< apiref "T_MongoDB_Bson_Serialization_IBsonSerializer" >}}) that have been registered. It can be accessed via the [`SerializerRegistry`]({{< apiref "P_MongoDB_Bson_Serialization_BsonSerializer_SerializerRegistry" >}}) property of the static class [`BsonSerializer`]({{< apiref "T_MongoDB_Bson_Serialization_BsonSerializer" >}}). 

{{% note %}}This is a global registry. Currently, you cannot use multiple registries in a single application.{{% /note %}}


## Serialization Provider

The [serializer registry]({{< relref "#serializer-registry" >}}) is powered by a list of [`IBsonSerializationProvider`]({{< apiref "T_MongoDB_Bson_Serialization_IBsonSerializationProvider" >}}). A serialization provider allows you to provide serializers on demand. The provider should be registered as soon as possible to ensure that the serializers provided are used. You can delegate handling of any types your custom provider isn't prepared to handle by returning null from [`GetSerializer`]({{< apiref "M_MongoDB_Bson_Serialization_IBsonSerializationProvider_GetSerializer" >}}).


### Implementation

To implement an [`IBsonSerializationProvider`]({{< apiref "T_MongoDB_Bson_Serialization_IBsonSerializationProvider" >}}), create a class that implements the interface and register it using the [`RegisterSerializationProvider`]({{< apiref "M_MongoDB_Bson_Serialization_BsonSerializer_RegisterSerializationProvider" >}}) method.

```csharp
class MyProvider : IBsonSerializationProvider
{
    public IBsonSerializer GetSerializer(Type type)
    {
        if (type == typeof(int))
        {
            return new MyInt32Serializer();
        }        

        return null;
    }
}
```

Above, we have a custom implemention of a serializer for an [`Int32`]({{< msdnref "system.int32" >}}). If the type being requested is for an [`Int32`]({{< msdnref "system.int32" >}}), we return our serializer. Otherwise, we return null to let the next [`IBsonSerializationProvider`]({{< apiref "T_MongoDB_Bson_Serialization_IBsonSerializationProvider" >}}) in line handle the request.


## Serializers

[`IBsonSerializer`]({{< apiref "T_MongoDB_Bson_Serialization_IBsonSerializer" >}}) is the main interface that is used to handle translating complex types. There are many serializers already built for handling primitive types, collection types, and [custom classes]({{< relref "reference\bson\mapping\index.md" >}}).

For example, to read a file containing the JSON `{ a: 1, b: [{ c: 1 }] }` into a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}), use the [`BsonDocumentSerializer`]({{< apiref "T_MongoDB_Bson_Serialization_Serializers_BsonDocumentSerializer" >}}):

```csharp
var jsonString = "{ a: 1, b: [{ c: 1 }] }";
using (var reader = new JsonReader(jsonString))
{
	var context = BsonDeserializationContext.CreateRoot(reader);
	BsonDocument doc = BsonDocumentSerializer.Instance.Deserialize(context);
}
```

### Implementation

{{% note class="warning" %}}Writing custom serializers to handle both normal cases and edge cases can be very tricky.{{% /note %}}

To implement a custom [`IBsonSerializer`]({{< apiref "T_MongoDB_Bson_Serialization_IBsonSerializer" >}}), it is best to inherit from [`SerializerBase<T>`]({{< apiref "T_MongoDB_Bson_Serialization_Serializers_SerializerBase_1" >}}) and override the [`Deserialize`]({{< apiref "M_MongoDB_Bson_Serialization_Serializers_SerializerBase_1_Deserialize" >}}) and [`Serialize`]({{< apiref "M_MongoDB_Bson_Serialization_Serializers_SerializerBase_1_Serialize" >}}) methods.

For example, it implement a serializer that reads an [`Int32`]({{< msdnref "system.int32" >}}):

```csharp
class MyInt32Serializer : SerializerBase<int>
{
    public override int Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return context.Reader.ReadInt32();
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, int value)
    {
        context.Writer.WriteInt32(value);
    }
}
```

This is overly simplistic. There are other factors that would need to be taken into account such as what happens when the actual BSON type is an [`Int64`]({{< msdnref "system.int64" >}}). In this case, the below implementation is much better:


```csharp
class MyInt32Serializer : SerializerBase<int>
{
    public override int Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.GetCurrentBsonType();
        switch (type)
        {
            case BsonType.Int32:
                return context.Reader.ReadInt32();
            case BsonType.Int64:
                return Convert.ToInt32(context.Reader.ReadInt64());
            case BsonType.Double:
                return Convert.ToInt32(context.Reader.ReadDouble());
            case BsonType.String:
                return int.Parse(context.Reader.ReadString());
            default:
                var message = string.Format("Cannot convert a {0} to an Int32.", type);
                throw new NotSupportedException(message);
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, int value)
    {
        context.Writer.WriteInt32(value);
    }
}
```

Notice that we are testing the current BsonType while reading and making decisions. Since some of these conversions could result in an overflow or truncation, exceptions may still be thrown.

{{% note %}}The built-in [`Int32Serializer`]({{< apiref "T_MongoDB_Bson_Serialization_Serializers_Int32Serializer" >}}) accounts for this as well as other such items.{{% /note %}}

You can register your serializer using the [`RegisterSerializer`]({{< apiref "M_MongoDB_Bson_Serialization_BsonSerializer_RegisterSerializer" >}}) or implement a [serialization provider]({{< relref "#serialization-provider" >}}).


### Opt-in Interfaces

There are some opt-in interfaces that allow the driver to utilize your custom serializer in special ways. You should evaluate these interfaces and decide whether your serializer should implement them.


#### IBsonIdProvider

If your class is used as a root document, you should implement the IBsonIdProvider interface in order for "Inserting" the document to function best, especially if the class your serializer is for uses an Id type other than [`ObjectId`]({{< apiref "T_MongoDB_Bson_ObjectId" >}}).


#### IBsonDocumentSerializer

In order to enable the driver to properly construct type-safe queries using a custom serializer, it needs access to member information. If your custom serializer is for a class, then you should implement [`IBsonDocumentSerializer`]({{< apiref "T_MongoDB_Bson_Serialization_IBsonDocumentSerializer" >}}).

```csharp
class MyClass
{
    public ObjectId Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }
}

class MyClassSerializer : SerializerBase<MyClass>, IBsonDocumentSerializer
{
    // implement Serialize and Deserialize

    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
    {
        switch (memberName)
        {
            case "Id":
                serializationInfo = new BsonSerializationInfo("_id", new ObjectIdSerializer(), typeof(ObjectId));
                return true;
            case "FirstName":
                serializationInfo = new BsonSerializationInfo("fn", new StringSerializer(), typeof(string));
                return true;
            case "LastName":
                serializationInfo = new BsonSerializationInfo("ln", new StringSerializer(), typeof(string));
                return true;
            default:
                serializationInfo = null;
                return false;
        }
    }
}
```

Above, we are providing information about the members of our class based on the member name. This enables the driver to, for instance, translate the below lambda expression into `{ fn: 'Jack' }`.

```csharp
Find(x => x.FirstName == "Jack")
```

#### IBsonArraySerializer

In the same way, if you have written a custom collection serializer, you should implement [`IBsonArraySerializer`]({{< apiref "T_MongoDB_Bson_Serialization_IBsonArraySerializer" >}}).