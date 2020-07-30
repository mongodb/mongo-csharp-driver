+++
date = "2020-07-23T00:00:00Z"
draft = false
title = "Background"
[menu.main]
  parent = "GuidSerialization"
  identifier = "GuidSerializationBackground"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Background information

Guids were originally represented in BSON as BsonBinaryData values of subtype 3. Unfortunately, different drivers
inadvertently used different byte orders when converting a Guid to a 16 byte binary value. To standardize on a
single canonical representation BsonBinaryData subtype 4 was created with a well defined byte order.

The C# driver's support for Guids was originally based on the premise that all Guids in a single collection must
be represented the same way (i.e. using the same BsonBinaryData sub type and byte order). In order to accomplish this
the representation of Guids is enforced at the BSON reader and writer levels (because a single reader or writer is
used to read or write an entire document from or to the collection).

However, this original premise has not stood the test of time.

The first issue we ran into was that the server
started returning UUIDs (i.e. Guids) in metadata using standard subtype 4. If a collection was configured to use
subtype 3 (which it usually was since that is the default) the driver could not deserialize the Guids in the metadata
without throwing an exception. We worked around this by temporarily reconfiguring the BSON reader while reading the metadata.

The second issue is that the original premise was too strict. There are valid reasons why a single collection might
have a mix of Guid representations, and we need to allow that.

