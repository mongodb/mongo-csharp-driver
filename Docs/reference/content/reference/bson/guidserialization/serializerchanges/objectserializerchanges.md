+++
date = "2020-07-23T00:00:00Z"
draft = false
title = "ObjectSerializer changes"
[menu.main]
  parent = "GuidSerializationSerializerChanges"
  identifier = "ObjectSerializerChanges"
  weight = 20
  pre = "<i class='fa'></i>"
+++

# ObjectSerializer changes

Some small changes have been made to the ObjectSerializer to allow the GuidRepresentation it uses to be configurable at the serializer level.

## GuidRepresentation constructor argument and property

A new constructor has been added that allows you to configure the desired GuidRepresentation when instantiating an instance of
the ObjectSerializer.

For example:

```csharp
var objectDiscriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
var objectSerializer = new ObjectSerializer(objectDiscriminatorConvention, GuidRepresentation.Standard);
```

In V3 GuidRepresentationMode, if your application relies on the ObjectSerializer to serialize any Guids you must register
an object serializer that you have configured the way you want. This must be done early in your application and this object
serializer will be globally used whenever an object serializer is needed and has not been otherwise specified.

```csharp
var objectDiscriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
var objectSerializer = new ObjectSerializer(objectDiscriminatorConvention, GuidRepresentation.Standard);
BsonSerializer.RegisterSerializer(objectSerializer);
```
