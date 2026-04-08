# Change Streams

- Status: Accepted
- Minimum Server Version: 3.6

______________________________________________________________________

## Abstract

As of version 3.6 of the MongoDB server a new `$changeStream` pipeline stage is supported in the aggregation framework.
Specifying this stage first in an aggregation pipeline allows users to request that notifications are sent for all
changes to a particular collection. This specification defines the means for creating change streams in drivers, as well
as behavior during failure scenarios.

## Specification

### Definitions

#### META

The keywords "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and
"OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://www.ietf.org/rfc/rfc2119.txt).

#### Terms

##### Resumable Error

An error is considered resumable if it meets any of the criteria listed below. For any criteria with wire version
constraints, the driver MUST use the wire version of the connection used to do the initial `aggregate` or the `getMore`
that resulted in the error. Drivers MUST NOT check the wire version of the server after the command has been executed
when checking these constraints.

- Any error encountered which is not a server error (e.g. a timeout error or network error)

- A server error with code 43 (`CursorNotFound`)

- For servers with wire version 9 or higher (server version 4.4 or higher), any server error with the
    `ResumableChangeStreamError` error label.

- For servers with wire version less than 9, a server error with one of the following codes:

    | Error Name                      | Error Code |
    | ------------------------------- | ---------- |
    | HostUnreachable                 | 6          |
    | HostNotFound                    | 7          |
    | NetworkTimeout                  | 89         |
    | ShutdownInProgress              | 91         |
    | PrimarySteppedDown              | 189        |
    | ExceededTimeLimit               | 262        |
    | SocketException                 | 9001       |
    | NotWritablePrimary              | 10107      |
    | InterruptedAtShutdown           | 11600      |
    | InterruptedDueToReplStateChange | 11602      |
    | NotPrimaryNoSecondaryOk         | 13435      |
    | NotPrimaryOrSecondary           | 13436      |
    | StaleShardVersion               | 63         |
    | StaleEpoch                      | 150        |
    | StaleConfig                     | 13388      |
    | RetryChangeStream               | 234        |
    | FailedToSatisfyReadPreference   | 133        |

An error on an aggregate command is not a resumable error. Only errors on a getMore command may be considered resumable
errors.

### Guidance

For naming and deviation guidance, see the [CRUD specification](../crud/crud.md#naming).

### Server Specification

#### Response Format

**NOTE:** The examples in this section are provided for illustrative purposes, and are subject to change without
warning. Drivers that provide a static type to represent ChangeStreamDocument MAY include additional fields in their
API.

If an aggregate command with a `$changeStream` stage completes successfully, the response contains documents with the
following structure:

```typescript
class ChangeStreamDocument {
  /**
   * The id functions as an opaque token for use when resuming an interrupted
   * change stream.
   */
  _id: Document;

  /**
   * Describes the type of operation represented in this change notification.
   *
   * @note: Whether a change is reported as an event of the operation type
   * `update` or `replace` is a server implementation detail.
   *
   * @note: The server will add new `operationType` values in the future and drivers
   * MUST NOT err when they encounter a new `operationType`. Unknown `operationType`
   * values may be represented by "unknown" or the literal string value.
   */
  operationType: "insert" 
                | "update" 
                | "replace" 
                | "delete" 
                | "invalidate" 
                | "drop" 
                | "dropDatabase" 
                | "rename" 
                | "createIndexes"
                | "dropIndexes" 
                | "modify"
                | "create" 
                | "shardCollection" 
                | "refineCollectionShardKey" 
                | "reshardCollection";

  /**
   * Contains two fields: "db" and "coll" containing the database and
   * collection name in which the change happened.
   *
   * @note: Drivers MUST NOT err when extra fields are encountered in the `ns` document
   * as the server may add new fields in the future such as `viewOn`.
   */
  ns: Document;

  /**
   * Only present for ops of type 'create'.
   * Only present when the `showExpandedEvents` change stream option is enabled.
   * 
   * The type of the newly created object.
   * 
   * @since 8.1.0
   */
  nsType: Optional<"collection" | "timeseries" | "view">;

  /**
   * Only present for ops of type 'rename'.
   *
   * The namespace, in the same format as `ns`, that a collection has been renamed to.
   */
  to: Optional<Document>;

  /**
   * * Only present when the `showExpandedEvents` change stream option is enabled and for the following events:
   *  - 'rename'
   *  - 'create'
   *  - 'modify'
   *  - 'createIndexes'
   *  - 'dropIndexes'
   *  - 'shardCollection'
   *  - 'reshardCollection'
   *  - 'refineCollectionShardKey'
   *
   * A description of the operation.
   * 
   * @since 6.0.0
   */
  operationDescription: Optional<Document>

  /**
   * Only present for ops of type 'insert', 'update', 'replace', and
   * 'delete'.
   *
   * For unsharded collections this contains a single field, _id, with the
   * value of the _id of the document updated.  For sharded collections,
   * this will contain all the components of the shard key in order,
   * followed by the _id if the _id isn't part of the shard key.
   */
  documentKey: Optional<Document>;

  /**
   * Only present for ops of type 'update'.
   */
  updateDescription: Optional<UpdateDescription>;

  /**
   * Always present for operations of type 'insert' and 'replace'. Also
   * present for operations of type 'update' if the user has specified
   * 'updateLookup' for the 'fullDocument' option when creating the change
   * stream.
   *
   * For operations of type 'insert' and 'replace', this key will contain the
   * document being inserted or the new version of the document that is
   * replacing the existing document, respectively.
   *
   * For operations of type 'update', this key will contain a copy of the full
   * version of the document from some point after the update occurred. If the
   * document was deleted since the updated happened, it will be null.
   *
   * Contains the point-in-time post-image of the modified document if the
   * post-image is available and either 'required' or 'whenAvailable' was
   * specified for the 'fullDocument' option when creating the change stream.
   * A post-image is always available for 'insert' and 'replace' events.
   */
  fullDocument: Document | null;

  /**
   * Contains the pre-image of the modified or deleted document if the
   * pre-image is available for the change event and either 'required' or
   * 'whenAvailable' was specified for the 'fullDocumentBeforeChange' option
   * when creating the change stream. If 'whenAvailable' was specified but the
   * pre-image is unavailable, this will be explicitly set to null.
   */
  fullDocumentBeforeChange: Document | null;

  /**
   * The wall time from the mongod that the change event originated from.
   * Populated for server versions 6.0 and above.
   */
  wallTime: Optional<DateTime>;

  /**
   * The `ui` field from the oplog entry corresponding to the change event.
   * 
   * Only present when the `showExpandedEvents` change stream option is enabled and for the following events:
   *  - 'insert'
   *  - 'update'
   *  - 'delete'
   *  - 'createIndexes'
   *  - 'dropIndexes'
   *  - 'modify'
   *  - 'drop'
   *  - 'create'
   *  - 'shardCollection'
   *  - 'reshardCollection'
   *  - 'refineCollectionShardKey'
   *  
   * This field is a value of binary subtype 4 (UUID).
   *  
   * @since 6.0.0
   */
  collectionUUID: Optional<Binary>;

  /**
   * The cluster time at which the change occurred.
   */
  clusterTime: Timestamp;

}

class UpdateDescription {
  /**
   * A document containing key:value pairs of names of the fields that were
   * changed (excluding the fields reported via `truncatedArrays`), and the new value for those fields.
   *
   * Despite array fields reported via `truncatedArrays` being excluded from this field,
   * changes to fields of the elements of the array values may be reported via this field.
   * Example:
   *   original field:
   *     "arrayField": ["foo", {"a": "bar"}, 1, 2, 3]
   *   updated field:
   *     "arrayField": ["foo", {"a": "bar", "b": 3}]
   *   a potential corresponding UpdateDescription:
   *     {
   *       "updatedFields": {
   *         "arrayField.1.b": 3
   *       },
   *       "removedFields": [],
   *       "truncatedArrays": [
   *         {
   *           "field": "arrayField",
   *           "newSize": 2
   *         }
   *       ]
   *     }
   *
   * Modifications to array elements are expressed via the dot notation (https://www.mongodb.com/docs/manual/core/document/#document-dot-notation).
   * Example: an `update` which sets the element with index 0 in the array field named arrayField to 7 is reported as
   *   "updatedFields": {"arrayField.0": 7}
   */
  updatedFields: Document;

  /**
   * An array of field names that were removed from the document.
   */
  removedFields: Array<String>;

  /**
   * Truncations of arrays may be reported using one of the following methods:
   * either via this field or via the 'updatedFields' field. In the latter case the entire array is considered to be replaced.
   *
   * The structure of documents in this field is
   *   {
   *      "field": <string>,
   *      "newSize": <int>
   *   }
   * Example: an `update` which shrinks the array arrayField.0.nestedArrayField from size 8 to 5 may be reported as
   *   "truncatedArrays": [{"field": "arrayField.0.nestedArrayField", "newSize": 5}]
   *
   * @note The method used to report a truncation is a server implementation detail.
   * @since 4.7.0
   */
  truncatedArrays: Array<Document>;

  /**
   * A document containing a map that associates an update path to an array containing the path components used in the update document. This data
   * can be used in combination with the other fields in an `UpdateDescription` to determine the actual path in the document that was updated. This is 
   * necessary in cases where a key contains dot-separated strings (i.e., `{ "a.b": "c" }`) or a document contains a numeric literal string key
   * (i.e., `{ "a": { "0": "a" } }`. Note that in this scenario, the numeric key can't be the top level key, because `{ "0": "a" }` is not ambiguous - 
   * update paths would simply be `'0'` which is unambiguous because BSON documents cannot have arrays at the top level.).
   * 
   * Each entry in the document maps an update path to an array which contains the actual path used when the document was updated.  
   * For example, given a document with the following shape `{ "a": { "0": 0 } }` and an update of `{ $inc: { "a.0": 1 } }`, `disambiguatedPaths` would
   * look like the following:
   *   {
   *      "a.0": ["a", "0"]
   *   }
   * 
   * In each array, all elements will be returned as strings with the exception of array indices, which will be returned as 32 bit integers.
   * 
   * Only present when the `showExpandedEvents` change stream option is enabled.
   * 
   * @since 6.1.0
   */
  disambiguatedPaths: Optional<Document>
}
```

The responses to a change stream aggregate or getMore have the following structures:

```typescript
/**
 * Response to a successful aggregate.
 */
{
    ok: 1,
    cursor: {
       ns: String,
       id: Int64,
       firstBatch: Array<ChangeStreamDocument>,
       /**
        * postBatchResumeToken is returned in MongoDB 4.0.7 and later.
        */
       postBatchResumeToken: Document
    },
    operationTime: Timestamp,
    $clusterTime: Document,
}

/**
 * Response to a successful getMore.
 */
{
    ok: 1,
    cursor: {
       ns: String,
       id: Int64,
       nextBatch: Array<ChangeStreamDocument>
       /**
        * postBatchResumeToken is returned in MongoDB 4.0.7 and later.
        */
       postBatchResumeToken: Document
    },
    operationTime: Timestamp,
    $clusterTime: Document,
}
```

### Driver API

```typescript
interface ChangeStream extends Iterable<Document> {
  /**
   * The cached resume token
   */
  private resumeToken: Document;

  /**
   * The pipeline of stages to append to an initial ``$changeStream`` stage
   */
  private pipeline: Array<Document>;

  /**
   * The options provided to the initial ``$changeStream`` stage
   */
  private options: ChangeStreamOptions;

  /**
   * The read preference for the initial change stream aggregation, used
   * for server selection during an automatic resume.
   */
  private readPreference: ReadPreference;
}

interface Collection {
  /**
   * @returns a change stream on a specific collection.
   */
  watch(pipeline: Document[], options: Optional<ChangeStreamOptions>): ChangeStream;
}

interface Database {
  /**
   * Allows a client to observe all changes in a database.
   * Excludes system collections.
   * @returns a change stream on all collections in a database
   * @since 4.0
   * @see https://www.mongodb.com/docs/manual/reference/system-collections/
   */
  watch(pipeline: Document[], options: Optional<ChangeStreamOptions>): ChangeStream;
}

interface MongoClient {
  /**
   * Allows a client to observe all changes in a cluster.
   * Excludes system collections.
   * Excludes the "config", "local", and "admin" databases.
   * @since 4.0
   * @returns a change stream on all collections in all databases in a cluster
   * @see https://www.mongodb.com/docs/manual/reference/system-collections/
   */
  watch(pipeline: Document[], options: Optional<ChangeStreamOptions>): ChangeStream;
}

class ChangeStreamOptions {
  /**
   * Allowed values: 'default', 'updateLookup', 'whenAvailable', 'required'.
   *
   * The default is to not send a value, which is equivalent to 'default'. By
   * default, the change notification for partial updates will include a delta
   * describing the changes to the document.
   *
   * When set to 'updateLookup', the change notification for partial updates
   * will include both a delta describing the changes to the document as well
   * as a copy of the entire document that was changed from some time after
   * the change occurred.
   *
   * When set to 'whenAvailable', configures the change stream to return the
   * post-image of the modified document for replace and update change events
   * if the post-image for this event is available.
   *
   * When set to 'required', the same behavior as 'whenAvailable' except that
   * an error is raised if the post-image is not available.
   *
   * For forward compatibility, a driver MUST NOT raise an error when a user
   * provides an unknown value. The driver relies on the server to validate
   * this option.
   *
   * @note this is an option of the `$changeStream` pipeline stage.
   */
  fullDocument: Optional<String>;

  /**
   * Allowed values: 'whenAvailable', 'required', 'off'.
   *
   * The default is to not send a value, which is equivalent to 'off'.
   *
   * When set to 'whenAvailable', configures the change stream to return the
   * pre-image of the modified document for replace, update, and delete change
   * events if it is available.
   *
   * When set to 'required', the same behavior as 'whenAvailable' except that
   * an error is raised if the pre-image is not available.
   *
   * For forward compatibility, a driver MUST NOT raise an error when a user
   * provides an unknown value. The driver relies on the server to validate
   * this option.
   *
   * @note this is an option of the `$changeStream` pipeline stage.
   */
  fullDocumentBeforeChange: Optional<String>;

  /**
   * Specifies the logical starting point for the new change stream.
   *
   * @note this is an option of the `$changeStream` pipeline stage.
   */
  resumeAfter: Optional<Document>;

  /**
   * The maximum amount of time for the server to wait on new documents to satisfy
   * a change stream query.
   *
   * This is the same field described in FindOptions in the CRUD spec.
   *
   * @see https://github.com/mongodb/specifications/blob/master/source/crud/crud.md#read
   * @note this option is an alias for `maxTimeMS`, used on `getMore` commands
   * @note this option is not set on the `aggregate` command nor `$changeStream` pipeline stage
   */
  maxAwaitTimeMS: Optional<Int64>;

  /**
   * The number of documents to return per batch.
   *
   * This option is sent only if the caller explicitly provides a value. The
   * default is to not send a value.
   *
   * @see https://www.mongodb.com/docs/manual/reference/command/aggregate
   * @note this is an aggregation command option
   */
  batchSize: Optional<Int32>;

  /**
   * Specifies a collation.
   *
   * This option is sent only if the caller explicitly provides a value. The
   * default is to not send a value.
   *
   * @see https://www.mongodb.com/docs/manual/reference/command/aggregate
   * @note this is an aggregation command option
   */
  collation: Optional<Document>;

  /**
   * The change stream will only provide changes that occurred at or after the
   * specified timestamp. Any command run against the server will return
   * an operation time that can be used here.
   *
   * @since 4.0
   * @see https://www.mongodb.com/docs/manual/reference/method/db.runCommand/
   * @note this is an option of the `$changeStream` pipeline stage.
   */
  startAtOperationTime: Optional<Timestamp>;

  /**
   * Similar to `resumeAfter`, this option takes a resume token and starts a
   * new change stream returning the first notification after the token.
   * This will allow users to watch collections that have been dropped and recreated
   * or newly renamed collections without missing any notifications.
   *
   * The server will report an error if `startAfter` and `resumeAfter` are both specified.
   *
   * @since 4.1.1
   * @see https://www.mongodb.com/docs/manual/changeStreams/#change-stream-start-after
   * @note this is an option of the `$changeStream` pipeline stage.
   */
   startAfter: Optional<Document>;

  /**
   * Enables users to specify an arbitrary comment to help trace the operation through
   * the database profiler, currentOp and logs. The default is to not send a value.
   *
   * The comment can be any valid BSON type for server versions 4.4 and above.
   * Server versions prior to 4.4 only support string as comment,
   * and providing a non-string type will result in a server-side error.
   *
   * If a comment is provided, drivers MUST attach this comment to all
   * subsequent getMore commands run on the same cursor for server
   * versions 4.4 and above. For server versions below 4.4 drivers MUST NOT
   * attach a comment to getMore commands.
   *
   * @see https://www.mongodb.com/docs/manual/reference/command/aggregate
   * @note this is an aggregation command option
   */
  comment: Optional<any>

  /**
   * Enables the server to send the 'expanded' list of change stream events.
   * The list of additional events included with this flag set are
   * - createIndexes
   * - dropIndexes
   * - modify
   * - create
   * - shardCollection
   * - reshardCollection
   * - refineCollectionShardKey
   * 
   * This flag is available in server versions greater than 6.0.0. `reshardCollection` and
   * `refineCollectionShardKey` events are not available until server version 6.1.0.
   * 
   * @note this is an option of the change stream pipeline stage
   */
  showExpandedEvents: Optional<Boolean>
}
```

**NOTE:** The set of `ChangeStreamOptions` may grow over time.

#### Helper Method

The driver API consists of a `ChangeStream` type, as well as three helper methods. All helpers MUST return a
`ChangeStream` instance. Implementers MUST document that helper methods are preferred to running a raw aggregation with
a `$changeStream` stage, for the purpose of supporting resumability.

The helper methods must construct an aggregation command with a REQUIRED initial `$changeStream` stage. A driver MUST
NOT throw a custom exception if multiple `$changeStream` stages are present (e.g. if a user also passed `$changeStream`
in the pipeline supplied to the helper), as the server will return an error.

The helper methods MUST determine a read concern for the operation in accordance with the
[Read and Write Concern specification](../read-write-concern/read-write-concern.md#via-code). The initial implementation
of change streams on the server requires a 'majority' read concern or no read concern. Drivers MUST document this
requirement. Drivers SHALL NOT throw an exception if any other read concern is specified, but instead should depend on
the server to return an error.

The stage has the following shape:

```typescript
{ $changeStream: ChangeStreamOptions }
```

The first parameter of the helpers specifies an array of aggregation pipeline stages which MUST be appended to the
initial stage. Drivers MUST support an empty pipeline. Languages which support default parameters MAY specify an empty
array as the default value for this parameter. Drivers SHOULD otherwise make specification of a pipeline as similar as
possible to the [aggregate](../crud/crud.md#read) CRUD method.

Additionally, implementers MAY provide a form of these methods which require no parameters, assuming no options and no
additional stages beyond the initial `$changeStream` stage:

```python
for change in db.collection.watch():
    print(change)
```

Presently change streams support only a subset of available aggregation stages:

- `$match`
- `$project`
- `$addFields`
- `$replaceRoot`
- `$redact`

A driver MUST NOT throw an exception if any unsupported stage is provided, but instead depend on the server to return an
error.

A driver MUST NOT throw an exception if a user adds, removes, or modifies fields using `$project`. The server will
produce an error if `_id` is projected out, but a user should otherwise be able to modify the shape of the change stream
event as desired. This may require the result to be deserialized to a `BsonDocument` or custom-defined type rather than
a `ChangeStreamDocument`. It is the responsibility of the user to ensure that the deserialized type is compatible with
the specified `$project` stage.

The aggregate helper methods MUST have no new logic related to the `$changeStream` stage. Drivers MUST be capable of
handling [TAILABLE_AWAIT](../crud/crud.md#read) cursors from the aggregate command in the same way they handle such
cursors from find.

##### `Collection.watch` helper

Returns a `ChangeStream` on a specific collection

Command syntax:

```typescript
{
  aggregate: 'collectionName'
  pipeline: [{$changeStream: {...}}, ...],
  ...
}
```

##### `Database.watch` helper

- Since: 4.0

Returns a `ChangeStream` on all collections in a database.

Command syntax:

```typescript
{
  aggregate: 1
  pipeline: [{$changeStream: {...}}, ...],
  ...
}
```

Drivers MUST use the `ns` returned in the `aggregate` command to set the `collection` option in subsequent `getMore`
commands.

##### `MongoClient.watch` helper

- Since: 4.0

Returns a `ChangeStream` on all collections in all databases in a cluster

Command syntax:

```typescript
{
  aggregate: 1
  pipeline: [{$changeStream: {allChangesForCluster: true, ...}}, ...],
  ...
}
```

The helper MUST run the command against the `admin` database

Drivers MUST use the `ns` returned in the `aggregate` command to set the `collection` option in subsequent `getMore`
commands.

#### ChangeStream

A `ChangeStream` is an abstraction of a [TAILABLE_AWAIT](../crud/crud.md#read) cursor, with support for resumability.
Implementers MAY choose to implement a `ChangeStream` as an extension of an existing tailable cursor implementation. If
the `ChangeStream` is implemented as a type which owns a tailable cursor, then the implementer MUST provide a manner of
closing the change stream, as well as satisfy the requirements of extending `Iterable<Document>`. If your language has
an idiomatic way of disposing of resources you MAY choose to implement that in addition to, or instead of, an explicit
close method.

A change stream MUST track the last resume token, per
[Updating the Cached Resume Token](#updating-the-cached-resume-token).

Drivers MUST raise an error on the first document received without a resume token (e.g. the user has removed `_id` with
a pipeline stage), and close the change stream. The error message SHOULD resemble "Cannot provide resume functionality
when the resume token is missing".

A change stream MUST attempt to resume a single time if it encounters any resumable error per
[Resumable Error](#resumable-error). A change stream MUST NOT attempt to resume on any other type of error.

In addition to tracking a resume token, change streams MUST also track the read preference specified when the change
stream was created. In the event of a resumable error, a change stream MUST perform server selection with the original
read preference using the
[rules for server selection](../server-selection/server-selection.md#rules-for-server-selection) before attempting to
resume.

##### Single Server Topologies

Presently, change streams cannot be initiated on single server topologies as they do not have an oplog. Drivers MUST NOT
throw an exception in this scenario, but instead rely on an error returned from the server. This allows for the server
to seamlessly introduce support for this in the future, without need to make changes in driver code.

##### startAtOperationTime

- Since: 4.0

`startAtOperationTime` specifies that a change stream will only return changes that occurred at or after the specified
`Timestamp`.

The server expects `startAtOperationTime` as a BSON Timestamp. Drivers MUST allow users to specify a
`startAtOperationTime` option in the `watch` helpers. They MUST allow users to specify this value as a raw `Timestamp`.

`startAtOperationTime`, `resumeAfter`, and `startAfter` are all mutually exclusive; if any two are set, the server will
return an error. Drivers MUST NOT throw a custom error, and MUST defer to the server error.

The `ChangeStream` MUST save the `operationTime` from the initial `aggregate` response when the following criteria are
met:

- None of `startAtOperationTime`, `resumeAfter`, `startAfter` were specified in the `ChangeStreamOptions`.
- The max wire version is >= `7`.
- The initial `aggregate` response had no results.
- The initial `aggregate` response did not include a `postBatchResumeToken`.

##### resumeAfter

`resumeAfter` is used to resume a `ChangeStream` that has been stopped to ensure that only changes starting with the log
entry immediately *after* the provided token will be returned. If the resume token specified does not exist, the server
will return an error.

##### Resume Process

Once a `ChangeStream` has encountered a resumable error, it MUST attempt to resume one time. The process for resuming
MUST follow these steps:

- Perform server selection.
- Connect to selected server.
- If there is a cached `resumeToken`:
    - If the `ChangeStream` was started with `startAfter` and has yet to return a result document:
        - The driver MUST set `startAfter` to the cached `resumeToken`.
        - The driver MUST NOT set `resumeAfter`.
        - The driver MUST NOT set `startAtOperationTime`. If `startAtOperationTime` was in the original aggregation command,
            the driver MUST remove it.
    - Else:
        - The driver MUST set `resumeAfter` to the cached `resumeToken`.
        - The driver MUST NOT set `startAfter`. If `startAfter` was in the original aggregation command, the driver MUST
            remove it.
        - The driver MUST NOT set `startAtOperationTime`. If `startAtOperationTime` was in the original aggregation command,
            the driver MUST remove it.
- Else if there is no cached `resumeToken` and the `ChangeStream` has a saved operation time (either from an originally
    specified `startAtOperationTime` or saved from the original aggregation) and the max wire version is >= `7`:
    - The driver MUST NOT set `resumeAfter`.
    - The driver MUST NOT set `startAfter`.
    - The driver MUST set `startAtOperationTime` to the value of the originally used `startAtOperationTime` or the one
        saved from the original aggregation.
- Else:
    - The driver MUST NOT set `resumeAfter`, `startAfter`, or `startAtOperationTime`.
    - The driver MUST use the original aggregation command to resume.

When `resumeAfter` is specified the `ChangeStream` will return notifications starting with the oplog entry immediately
*after* the provided token.

If the server supports sessions, the resume attempt MUST use the same session as the previous attempt's command.

A driver MUST only attempt to resume once from a resumable error. However, if the `aggregate` for that resume succeeds,
a driver MUST ensure that following resume attempts can succeed, even in the absence of any changes received by the
cursor between resume attempts. For example:

1. `aggregate` (succeeds)
2. `getMore` (fails with resumable error)
3. `aggregate` (succeeds)
4. `getMore` (fails with resumable error)
5. `aggregate` (succeeds)
6. `getMore` (succeeds)
7. change stream document received

A driver SHOULD attempt to kill the cursor on the server on which the cursor is opened during the resume process, and
MUST NOT attempt to kill the cursor on any other server. Any exceptions or errors that occur during the process of
killing the cursor should be suppressed, including both errors returned by the `killCursor` command and exceptions
thrown by opening, writing to, or reading from the socket.

##### Exposing All Resume Tokens

- Since: 4.0.7

Users can inspect the \_id on each `ChangeDocument` to use as a resume token. But since MongoDB 4.0.7, aggregate and
getMore responses also include a `postBatchResumeToken`. Drivers use one or the other when automatically resuming, as
described in [Resume Process](#resume-process).

Drivers MUST expose a mechanism to retrieve the same resume token that would be used to automatically resume. It MUST be
possible to use this mechanism after iterating every document. It MUST be possible for users to use this mechanism
periodically even when no documents are getting returned (i.e. `getMore` has returned empty batches). Drivers have two
options to implement this.

###### Option 1: ChangeStream::getResumeToken()

```typescript
interface ChangeStream extends Iterable<Document> {
  /**
   * Returns the cached resume token that will be used to resume
   * after the most recently returned change.
   */
  public getResumeToken() Optional<Document>;
}
```

This MUST be implemented in synchronous drivers. This MAY be implemented in asynchronous drivers.

###### Option 2: Event Emitted for Resume Token

Allow users to set a callback to listen for new resume tokens. The exact interface is up to the driver, but it MUST meet
the following criteria:

- The callback is set in the same manner as a callback used for receiving change documents.
- The callback accepts a resume token as an argument.
- The callback (or event) MAY include an optional ChangeDocument, which is unset when called with resume tokens sourced
    from `postBatchResumeToken`.

A possible interface for this callback MAY look like:

```typescript
interface ChangeStream extends Iterable<Document> {
  /**
   * Returns a resume token that should be used to resume after the most
   * recently returned change.
   */
  public onResumeTokenChanged(ResumeTokenCallback:(Document resumeToken) => void);
}
```

This MUST NOT be implemented in synchronous drivers. This MAY be implemented in asynchronous drivers.

###### Updating the Cached Resume Token

The following rules describe how to update the cached `resumeToken`:

- When the `ChangeStream` is started:
    - If `startAfter` is set, cache it.
    - Else if `resumeAfter` is set, cache it.
    - Else, `resumeToken` remains unset.
- When `aggregate` or `getMore` returns:
    - If an empty batch was returned and a `postBatchResumeToken` was included, cache it.
- When returning a document to the user:
    - If it's the last document in the batch and a `postBatchResumeToken` is included, cache it.
    - Else, cache the `_id` of the document.

###### Not Blocking on Iteration

Synchronous drivers MUST provide a way to iterate a change stream without blocking until a change document is returned.
This MUST give the user an opportunity to get the most up-to-date resume token, even when the change stream continues to
receive empty batches in getMore responses. This allows users to call `ChangeStream::getResumeToken()` after iterating
every document and periodically when no documents are getting returned.

Although the implementation of tailable awaitData cursors is not specified, this MAY be implemented with a `tryNext`
method on the change stream cursor.

All drivers MUST document how users can iterate a change stream and receive *all* resume token updates.
[Why do we allow access to the resume token to users](#why-do-we-allow-access-to-the-resume-token-to-users) shows an
example. The documentation MUST state that users intending to store the resume token should use this method to get the
most up to date resume token.

##### Timeouts

Drivers MUST apply timeouts to change stream establishment, iteration, and resume attempts per
[Client Side Operations Timeout: Change Streams](../client-side-operations-timeout/client-side-operations-timeout.md#change-streams).

##### Notes and Restrictions

**1. `fullDocument: updateLookup` can result in change documents larger than 16 MiB**

There is a risk that if there is a large change to a large document, the full document and delta might result in a
document larger than the 16 MiB limitation on BSON documents. If that happens the cursor will be closed, and a server
error will be returned.

**2. Users can remove the resume token with aggregation stages**

It is possible for a user to specify the following stage:

```javascript
{ $project: { _id: 0 } }
```

Similar removal of the resume token is possible with the `$redact` and `$replaceRoot` stages. While this is not
technically illegal, it makes it impossible for drivers to support resumability. Users may explicitly opt out of
resumability by issuing a raw aggregation with a `$changeStream` stage.

## Rationale

### Why Helper Methods?

Change streams are a first class concept similar to CRUD or aggregation; the fact that they are initiated via an
aggregation pipeline stage is merely an implementation detail. By requiring drivers to support top-level helper methods
for this feature we not only signal this intent, but also solve a number of other potential problems:

Disambiguation of the result type of this special-case aggregation pipeline (`ChangeStream`), and an ability to control
the behaviors of the resultant cursor

More accurate support for the concept of a maximum time the user is willing to wait for subsequent queries to complete
on the resultant cursor (`maxAwaitTimeMs`)

Finer control over the options pertaining specifically to this type of operation, without polluting the already
well-defined `AggregateOptions`

Flexibility for future potentially breaking changes for this feature on the server

### Why are ChangeStreams required to retry once on a resumable error?

User experience is of the utmost importance. Errors not originating from the server are generally network errors, and
network errors can be transient. Attempting to resume an interrupted change stream after the initial error allows for a
seamless experience for the user, while subsequent network errors are likely to be an outage which can then be exposed
to the user with greater confidence.

### Why do we allow access to the resume token to users

Imagine a scenario in which a user wants to process each change to a collection **at least once**, but the application
crashes during processing. In order to overcome this failure, a user might use the following approach:

```python
localChange = getChangeFromLocalStorage()
resumeToken = getResumeTokenFromLocalStorage()

if localChange:
  processChange(localChange)

try:
    change_stream = db.collection.watch([...], resumeAfter=resumeToken)
    while True:
        change = change_stream.try_next()
        persistResumeTokenToLocalStorage(change_stream.get_resume_token())
        if change:
          persistChangeToLocalStorage(change)
          processChange(change)
except Exception:
    log.error("...")
```

In this case the current change is always persisted locally, including the resume token, such that on restart the
application can still process the change while ensuring that the change stream continues from the right logical time in
the oplog. It is the application's responsibility to ensure that `processChange` is idempotent, this design merely makes
a reasonable effort to process each change **at least** once.

### Why is there no example of the desired user experience?

The specification used to include this overspecified example of the "desired user experience":

```python
try:
    for change in db.collection.watch(...):
        print(change)
except Exception:
    # We know for sure it's unrecoverable:
    log.error("...")
```

It was decided to remove this example from the specification for the following reasons:

- Tailable + awaitData cursors behave differently in existing supported drivers.
- There are considerations to be made for languages that do not permit interruptible I/O (such as Java), where a change
    stream which blocks forever in a separate thread would necessitate killing the thread.
- There is something to be said for an API that allows cooperation by default. The model in which a call to next only
    blocks until any response is returned (even an empty batch), allows for interruption and cooperation (e.g.
    interaction with other event loops).

### Why is an allow list of error codes preferable to a deny list?

Change streams originally used a deny list of error codes to determine which errors were not resumable. However, this
allowed for the possibility of infinite resume loops if an error was not correctly deny listed. Due to the fact that all
errors aside from transient issues such as failovers are not resumable, the resume behavior was changed to use an allow
list. Part of this change was to introduce the `ResumableChangeStreamError` label so the server can add new error codes
to the allow list without requiring changes to drivers.

### Why is `CursorNotFound` special-cased when determining resumability?

With the exception of `CursorNotFound`, a server error on version 4.4 or higher is considered resumable if and only if
it contains the `ResumableChangeStreamError` label. However, this label is only added by the server if the cursor being
created or iterated is a change stream. `CursorNotFound` is returned when a `getMore` is done with a cursor ID that the
server isn't aware of and therefore can't determine if the cursor is a change stream. Marking all `CursorNotFound`
errors resumable in the server regardless of cursor type could be confusing as a user could see the
`ResumableChangeStreamError` label when iterating a non-change stream cursor. To workaround this, drivers always treat
this error as resumable despite it not having the proper error label.

### Why do we need to send a default `startAtOperationTime` when resuming a `ChangeStream`?

`startAtOperationTime` allows a user to create a resumable change stream even when a result (and corresponding
resumeToken) is not available until a later point in time.

For example:

- A client creates a `ChangeStream`, and calls `watch`
- The `ChangeStream` sends out the initial `aggregate` call, and receives a response with no initial values. Because
    there are no initial values, there is no latest resumeToken.
- The client's network is partitioned from the server, causing the client's `getMore` to time out
- Changes occur on the server.
- The network is unpartitioned
- The client attempts to resume the `ChangeStream`

In the above example, not sending `startAtOperationTime` will result in the change stream missing the changes that
occurred while the server and client are partitioned. By sending `startAtOperationTime`, the server will know to include
changes from that previous point in time.

### Why do we need to expose the postBatchResumeToken?

Resume tokens refer to an oplog entry. The resume token from the `_id` of a document corresponds the oplog entry of the
change. The `postBatchResumeToken` represents the oplog entry the change stream has scanned up to on the server (not
necessarily a matching change). This can be a much more recent oplog entry, and should be used to resume when possible.

Attempting to resume with an old resume token may degrade server performance since the server needs to scan through more
oplog entries. Worse, if the resume token is older than the last oplog entry stored on the server, then resuming is
impossible.

Imagine the change stream matches a very small percentage of events. On a `getMore` the server scans the oplog for the
duration of `maxAwaitTimeMS` but finds no matching entries and returns an empty response (still containing a
`postBatchResumeToken`). There may be a long sequence of empty responses. Then due to a network error, the change stream
tries resuming. If we tried resuming with the most recent `_id`, this throws out the oplog scanning the server had done
for the long sequence of getMores with empty responses. But resuming with the last `postBatchResumeToken` skips the
unnecessary scanning of unmatched oplog entries.

## Test Plan

See [tests/README.md](tests/README.md)

## Backwards Compatibility

There should be no backwards compatibility concerns.

## Reference Implementations

- NODE (NODE-1055)
- PYTHON (PYTHON-1338)
- RUBY (RUBY-1228)

## Changelog

- 2026-03-18: Revert expanded field visibility change.

- 2025-09-08: Clarify resume behavior.

- 2025-03-31: Update for expanded field visibility in server 8.2+

- 2025-02-24: Make `nsType` `Optional` to match other optional fields in the change stream spec.

- 2025-01-29: Add `nsType` to `ChangeStreamDocument`.

- 2024-02-09: Migrated from reStructuredText to Markdown.

- 2023-08-11: Update server versions for `$changeStreamSplitLargeEvent` test.

- 2023-05-22: Add spec test for `$changeStreamSplitLargeEvent`.

- 2022-10-20: Reformat changelog.

- 2022-10-05: Remove spec front matter.

- 2022-08-22: Add `clusterTime` to `ChangeStreamDocument`.

- 2022-08-17: Support `disambiguatedPaths` in `UpdateDescription`.

- 2022-05-19: Support new change stream events with `showExpandedEvents`.

- 2022-05-17: Add `wallTime` to `ChangeStreamDocument`.

- 2022-04-13: Support returning point-in-time pre and post-images with `fullDocumentBeforeChange` and `fullDocument`.

- 2022-03-25: Do not error when parsing change stream event documents.

- 2022-02-28: Add `to` to `ChangeStreamDocument`.

- 2022-02-10: Specify that `getMore` command must explicitly send inherited `comment`.

- 2022-02-01: Add `comment` to `ChangeStreamOptions`.

- 2022-01-19: Require that timeouts be applied per the client-side operations timeout specification.

- 2021-09-01: Clarify that server selection during resumption should respect normal server selection rules.

- 2021-04-29: Add `load-balanced` to test topology requirements.

- 2021-04-23: Update to use modern terminology.

- 2021-02-08: Add the `UpdateDescription.truncatedArrays` field.

- 2020-02-10: Change error handling approach to use an allow list.

- 2019-07-15: Clarify resume process for change streams started with the `startAfter` option.

- 2019-07-09: Change `fullDocument` to be an optional string.

- 2019-07-02: Fix server version for `startAfter`.

- 2019-07-01: Clarify that close may be implemented with more idiomatic patterns instead of a method.

- 2019-06-20: Fix server version for addition of `postBatchResumeToken`.

- 2019-04-12: Clarify caching process for resume token.

- 2019-04-03: Update the lowest server version that supports `postBatchResumeToken`.

- 2019-01-10: Clarify error handling for killing the cursor.

- 2018-11-06: Add handling of `postBatchResumeToken`.

- 2018-12-14: Add `startAfter` to change stream options.

- 2018-09-09: Add `dropDatabase` to change stream `operationType`.

- 2018-07-30: Remove redundant error message checks for resumable errors.

- 2018-07-27: Add drop to change stream `operationType`.

- 2018-06-14: Clarify how to calculate `startAtOperationTime`.

- 2018-05-24: Change `startAtClusterTime` to `startAtOperationTime`.

- 2018-04-18: Add helpers for Database and MongoClient, and add `startAtClusterTime` option.

- 2018-04-17: Clarify that the initial aggregate should not be retried.

- 2017-12-13: Default read concern is also accepted, not just "majority".

- 2017-11-06: Defer to Read and Write concern spec for determining a read concern for the helper method.

- 2017-09-26: Clarify that change stream options may be added later.

- 2017-09-21: Clarify that we need to close the cursor on missing token.

- 2017-09-06: Remove `desired user experience` example.

- 2017-08-22: Clarify killing cursors during resume process.

- 2017-08-16: Fix formatting of resume process.

- 2017-08-16: Add clarification regarding Resumable errors.

- 2017-08-07: Fix typo in command format.

- 2017-08-03: Initial commit.
