+++
date = "2020-07-23T00:00:00Z"
draft = false
title = "Guid Serialization"
[menu.main]
  parent = "BSON"
  identifier = "GuidSerialization"
  weight = 35
  pre = "<i class='fa'></i>"
+++

# Guid serialization

We are making changes to how Guids will be serialized in the future. For the time being the driver will
continue to serialize Guids as it has in the past, in order to not break backward compatibility. You
can opt-in to the new way of serializing Guids by setting the GuidRepresentationMode to V3.

The folowing sections contain more information:

* - [Background]({{< relref "reference\bson\guidserialization\background.md" >}})
* - [GuidRepresentationMode]({{< relref "reference\bson\guidserialization\guidrepresentationmode\guidrepresentationmode.md" >}})
* - [Serializer changes]({{< relref "reference\bson\guidserialization\serializerchanges\serializerchanges.md" >}})
