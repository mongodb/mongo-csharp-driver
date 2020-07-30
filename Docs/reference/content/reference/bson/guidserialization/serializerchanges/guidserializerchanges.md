+++
date = "2020-07-23T00:00:00Z"
draft = false
title = "GuidSerializer changes"
[menu.main]
  parent = "GuidSerializationSerializerChanges"
  identifier = "GuidSerializerChanges"
  weight = 20
  pre = "<i class='fa'></i>"
+++

# GuidSerializer changes

Some small changes have been made to the GuidSerializer to allow the GuidRepresentation it uses to be configurable at the serializer level.

## GuidRepresentation constructor argument and property

A new constructor has been added that allows you to configure the desired GuidRepresentation when instantiating an instance of
the GuidSerializer. Calling the constructor that takes a GuidRepresentation property implies a BsonType representation of Binary.

For example:

```csharp
var guidSerializer = new GuidSerializer(GuidRepresentation.Standard);
```

If you want to use the Standard GuidRepresentation globally you can register a properly configured GuidSerializer early in your code:

```csharp
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
```

## BsonGuidRepresentation attribute

In V3 GuidRepresentationMode you must explicitly specify the GuidRepresentation you want used for every Guid property. If you are
relying on the driver's auto mapping to map C# classes to document schemas you may use the new BsonGuidRepresentation attribute to specify the desired representation.

For example:

```csharp
public class C
{
    public int Id { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid G { get; set; }
}
```

If most of your Guids use the same representation and only a few use a different representation, you could alternatively register
a global GuidSerializer (as shown above) for the most commonly used representation, and only use the BsonGuidRepresentation attribute
to mark the ones that use a different representation.