+++
date = "2020-07-23T00:00:00Z"
draft = false
title = "GuidRepresentationMode"
[menu.main]
  parent = "GuidSerialization"
  identifier = "GuidRepresentationMode"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## GuidRepresentationMode

If we just abruptly changed the way the driver serialized Guids that would be a breaking change. In order to help applications
migrate in an orderly fashion to the new way of handling Guids we have introduced a configurable `GuidRepresentationMode`.
In V2 mode the driver will handle Guids the same way that the v2.x versions have in the past. In V3 mode the driver
will handle Guids in the new way. An application can opt-in to V3 mode to transition to the new way Guids are handled.
In the v2.x versions of the driver V2 is the default mode but V3 mode is supported. In future v3.x versions of the driver
V3 will be the default mode (and support for V2 mode will be removed).

### GuidRepresentationMode == V2 (Deprecated)

In V2 mode the central principle is that all Guids in a collection must be represented the same way. In order to enforce
this the representation of Guids is not controlled at the individual serializer level, but rather at the reader/writer
level since the same reader/writer is used to read/write an entire document.

Read more about V2 mode [here]({{< relref "reference\bson\guidserialization\guidrepresentationmode\v2mode.md" >}}).

### GuidRepresentationMode == V3

In V3 mode the central principle is that the representation of Guids is controlled at the level of each individual
property by configuring the serializer for that property. The recommendation is that all Guids in a
collection be represented uniformly using the standard BsonBinaryData subtype 4, but when working with historical
data it is acceptable for different Guid fields in the same document to be represented differently.

Read more about V3 mode [here]({{< relref "reference\bson\guidserialization\guidrepresentationmode\v3mode.md" >}}).

### Opting in to V3 GuidRepresentationMode

An application must choose to use either the original V2 GuidRepresentationMode or the new V3 GuidRepresentationMode. It is
not possible to mix use of both modes in the same application.

If you want to use V2 mode you don't need to do anything because V2 is still the default.

If you want to use V3 mode execute the following line of code as early as possible in your application:

```csharp
BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
```
