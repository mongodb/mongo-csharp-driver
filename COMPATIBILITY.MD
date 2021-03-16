### 4.2 server compatibility issues

1. 🦖 `ParallelScan` operation was deprecated in server version 4.1.
See respective ticket: [CSHARP-2329: Skip ParallelScan tests when server version is >= 4.1](https://jira.mongodb.org/browse/CSHARP-2329)
2. `CreateIndexesUsingInsertOperation` was deprecated in server version 4.1.
See respective ticket: [CSHARP-2386: Check server in index insertion tests](https://jira.mongodb.org/browse/CSHARP-2386)
3. Storage engine "mmpav1" was deprecated in server version 4.1. This would also cause commands using `CollectionOptions` and `CreateCollectionOptions` with deprecated engine value to fail with exception `"mmapv1 is not a registered storage engine for this server"`.
See respective ticket: [CSHARP-2322: Skip tests that depend on server features removed in 4.1](https://jira.mongodb.org/browse/CSHARP-2322)
4. Inserting duplicate key withing transaction reports `DuplicateKey` not in `CodeName`, but in `WriteError` property of exception.
Server change is: [SERVER-38583: Transactional bulkWrite error missing writeErrors (mongod)](https://jira.mongodb.org/browse/SERVER-38583)
See respective ticket: [CSHARP-2342: Test only the initial command in a transaction includes readConcern](https://jira.mongodb.org/browse/CSHARP-2342)

### 4.4 server compatibility issues

5. 🦖 `MapReduce` operation was rewritten in server version 4.4 and the driver version has several properties inaccessible as a result of using an old operation: `Duration`, `EmitCount`, `OutputCount`, `InputCount`. Additionally result order is no longer guaranteed. Non-legacy part of the driver has an up-to-date API which functions correctly.
See respective ticket: [CSHARP-2738: Deprecate mapparams, out.sharded, and out.nonAtomic arguments to mapReduce](https://jira.mongodb.org/browse/CSHARP-2738)
6. `IndexInfo.Namespace` property throws exception upon accessing, because `'ns'` field was removed from index definition in server version 4.4.
See respective ticket: [CSHARP-2684: Server latest removed the “ns” field from index definitions](https://jira.mongodb.org/browse/CSHARP-2684)
7. `AggregateExplain` operation structure changed. No big deal, since it is intended to be read by humans.
See respective ticket: [CSHARP-2842: The format of Aggregate Explain response has been changed](https://jira.mongodb.org/browse/CSHARP-2842)
8. 🦖 `GridFS.Download` not working on sharded cluster configuration.
See respective ticket: [CSHARP-3423: Fix CSharp269Tests.TestUploadAndDownload GridFs test on sharded cluster](https://jira.mongodb.org/browse/CSHARP-3423)

### latest server compatibility issues

9. 🦖 `GeoSearch` and `GeoHaystack` operations were removed from 4.9.0 server.
See respective ticket: [CSHARP-3249: Skip geoSearch and geoHaystack tests when running against server version that doesn't support these commands](https://jira.mongodb.org/browse/CSHARP-3249)
10. 🦖 `ReIndex` command fails on replica set and sharded cluster configurations, but works on standalone as it used to. This operation is part of legacy API. See respective change in 2.11.0 driver: [CSHARP-3058: Make 'reIndex' a standalone-only command](https://jira.mongodb.org/browse/CSHARP-3058)
11. `CreateIndexesOperation` result field `expireAfterSeconds` value is rounded since server version 4.9.0.
See respective ticket: [CSHARP-3294: Fix CreateIndexesOperationTests tests on latests](https://jira.mongodb.org/browse/CSHARP-3294)
12. `trunkatedArrays` field is not handled in `ChangeStreamDocument.UpdateDescription` property deserialization, causing the access to the property throw and exception `System.FormatException : Invalid field name: "truncatedArrays".` since server version 4.9.0. Other properties work as intended.
See respective ticket: [CSHARP-3212: Add new field truncatedArrays into cursor.nextBatch.updateDescription](https://jira.mongodb.org/browse/CSHARP-3212)
