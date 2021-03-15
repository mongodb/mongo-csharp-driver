### latest server compatibility issues

1. 🦖 `GeoSearch` and `GeoHaystack` operations were removed from 4.9.0 server. These operations are from legacy part of the driver and server returns quite comprehensible exception.
See respective ticket: [CSHARP-3249: Skip geoSearch and geoHaystack tests when running against server version that doesn't support these commands](https://jira.mongodb.org/browse/CSHARP-3249)
2. `trunkatedArrays` field is not handled in `ChangeStreamDocument.UpdateDescription` property deserialization, causing the access to the property throw and exception `System.FormatException : Invalid field name: "truncatedArrays".` since server version 4.9.0. Other properties work as intended.
See respective ticket: [CSHARP-3212: Add new field truncatedArrays into cursor.nextBatch.updateDescription](https://jira.mongodb.org/browse/CSHARP-3212)
3. `CreateIndexesOperation` result field `expireAfterSeconds` value is rounded since server version 4.9.0.
See respective ticket: [CSHARP-3294: Fix CreateIndexesOperationTests tests on latests](https://jira.mongodb.org/browse/CSHARP-3294)
4. 🦖 `GridFS.Download` not working on sharded cluster configuration.
See respective ticket: [CSHARP-3423: Fix CSharp269Tests.TestUploadAndDownload GridFs test on sharded cluster](https://jira.mongodb.org/browse/CSHARP-3423)
