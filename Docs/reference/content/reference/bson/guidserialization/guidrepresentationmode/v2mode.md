+++
date = "2020-07-23T00:00:00Z"
draft = false
title = "V2 mode"
[menu.main]
  parent = "GuidRepresentationMode"
  identifier = "GuidRepresentationModeV2"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## V2 GuidRepresentationMode (Deprecated)

In V2 mode the central principle is that all Guids in a collection must be represented the same way. In order to enforce
this the representation of Guids is not controlled at the individual serializer level, but rather at the reader/writer
level since the same reader/writer is used to read/write an entire document.

All of the following properties and methods are only relevant to V2 mode and are now deprecated:

* BsonDefaults GuidRepresentation property
* BsonBinaryData implicit conversion to or from Guid
* BsonBinaryData constructor taking a Guid (without a GuidRepresentation)
* BsonBinaryData constructor taking (byte[], BsonBinarySubType, GuidRepresentation)
* BsonBinaryData GuidRepresentation property
* BsonValue implicit conversion from Guid or Guid? (Nullable\<Guid>)
* BsonDocumentReaderSettings constructor taking a GuidRepresentation
* BsonDocumentWriterSettings constructor taking a GuidRepresentation
* BsonReaderSettings GuidRepresentation property
* BsonWriterSettings GuidRepresentation property
* IBsonReaderExtentions ReadBinaryDataWithGuidRepresentationUnspecified extension method
* MongoClientSettings GuidRepresentation property
* MongoCollectionSettings GuidRepresentation property
* MongoDatabaseSettings GuidRepresentation property
* MongoDefaults GuidRepresentation property
* MongoUrl GuidRepresentation property
* MongoUrlBuilder GuidRepresentation property
* MongoGridFSSettings GuidRepresentation property

Note: the BsonDefaults GuidRepresentationMode property is itself deprecated even though it is new because it is only
intended to be use during the transition period and will be removed when support for V2 mode is removed.
