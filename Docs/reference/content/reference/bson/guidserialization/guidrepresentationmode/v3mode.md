+++
date = "2020-07-23T00:00:00Z"
draft = false
title = "V3 mode"
[menu.main]
  parent = "GuidRepresentationMode"
  identifier = "GuidRepresentationModeV3"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## V3 GuidRepresentationMode

In V3 mode the central principle is that the representation of Guids is controlled at the level of each individual
property of a document by configuring the serializer for that property. The recommendation is that all Guids in a
collection be represented uniformly using the standard BsonBinaryData subtype 4, but when working with historical
data it is acceptable for different Guid fields in the same document to be represented differently.

The following existing methods behave differently in V3 mode:

* BsonBinaryReader.ReadBinaryData method ignores readerSettings.GuidRepresentation
* BsonBinaryWriter.WriteBinaryData method ignores writerSettings.GuidRepresentation
* JsonReader ReadBinaryData method ignores readerSettings.GuidRepresentation
* JsonWriter ignores writerSettings.GuidRepresentation
* BsonBinaryData ToGuid without GuidRepresentation argument is only valid for sub type 4


