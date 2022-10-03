====================================
Server Wire version and Feature List
====================================

.. list-table::
   :header-rows: 1

   * - Server version
     - Wire version
     - Feature List

   * - 2.6
     - 1
     - | Aggregation cursor
       | Auth commands

   * - 2.6
     - 2
     - | Write commands (insert/update/delete)
       | Aggregation $out pipeline operator

   * - 3.0
     - 3
     - | listCollections
       | listIndexes
       | SCRAM-SHA-1
       | explain command

   * - 3.2
     - 4
     - | (find/getMore/killCursors) commands
       | currentOp command
       | fsyncUnlock command
       | findAndModify take write concern
       | Commands take read concern
       | Document-level validation
       | explain command supports distinct and findAndModify

   * - 3.4
     - 5
     - | Commands take write concern
       | Commands take collation

   * - 3.6
     - 6
     - | Supports OP_MSG
       | Collection-level ChangeStream support
       | Retryable Writes
       | Causally Consistent Reads
       | Logical Sessions
       | update "arrayFilters" option

   * - 4.0
     - 7
     - | ReplicaSet transactions
       | Database and cluster-level change streams and startAtOperationTime option

   * - 4.2
     - 8
     - | Sharded transactions
       | Aggregation $merge pipeline operator

   * - 5.0
     - 12
     - | Consistent $collStats count behavior on sharded and non-sharded topologies

For more information see MongoDB Server repo: https://github.com/mongodb/mongo/blob/master/src/mongo/db/wire_version.h
