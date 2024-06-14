# Index Management Tests

______________________________________________________________________

## Test Plan

These prose tests are ported from the legacy enumerate-indexes spec.

### Configurations

- standalone node
- replica set primary node
- replica set secondary node
- mongos node

### Preparation

For each of the configurations:

- Create a (new) database
- Create a collection
- Create a single column index, a compound index, and a unique index
- Insert at least one document containing all the fields that the above indicated indexes act on

### Tests

- Run the driver's method that returns a list of index names, and:
  - verify that *all* index names are represented in the result
  - verify that there are no duplicate index names
  - verify there are no returned indexes that do not exist
- Run the driver's method that returns a list of index information records, and:
  - verify all the indexes are represented in the result
  - verify the "unique" flags show up for the unique index
  - verify there are no duplicates in the returned list
  - if the result consists of statically defined index models that include an `ns` field, verify that its value is
    accurate

### Search Index Management Helpers

These tests are intended to smoke test the search management helpers end-to-end against a live Atlas cluster.

The search index management commands are asynchronous and mongod/mongos returns before the changes to a clusters' search
indexes have completed. When these prose tests specify "waiting for the changes", drivers should repeatedly poll the
cluster with `listSearchIndexes` until the changes are visible. Each test specifies the condition that is considered
"ready". For example, when creating a new search index, waiting until the inserted index has a status `queryable: true`
indicates that the index was successfully created.

The commands tested in these prose tests take a while to successfully complete. Drivers should raise the timeout for
each test to avoid timeout errors if the test timeout is too low. 5 minutes is a sufficiently large timeout that any
timeout that occurs indicates a real failure, but this value is not required and can be tweaked per-driver.

There is a server-side limitation that prevents multiple search indexes from being created with the same name,
definition and collection name. This limitation does not take into account collection uuid. Because these commands are
asynchronous, any cleanup code that may run after a test (cleaning a database or dropping search indexes) may not have
completed by the next iteration of the test (or the next test run, if running locally). To address this issue, each test
uses a randomly generated collection name. Drivers may generate this collection name however they like, but a suggested
implementation is a hex representation of an ObjectId (`new ObjectId().toHexString()` in Node).

#### Setup

These tests must run against an Atlas cluster with a 7.0+ server.
[Scripts are available](https://github.com/mongodb-labs/drivers-evergreen-tools/tree/master/.evergreen/atlas) in
drivers-evergreen-tools which can setup and teardown Atlas clusters. To ensure that the Atlas cluster is cleaned up
after each CI run, drivers should configure evergreen to run these tests as a part of a task group. Be sure that the
cluster gets torn down!

When working locally on these tests, the same Atlas setup and teardown scripts can be used locally to provision a
cluster for development.

#### Case 1: Driver can successfully create and list search indexes

1. Create a collection with the "create" command using a randomly generated name (referred to as `coll0`).

2. Create a new search index on `coll0` with the `createSearchIndex` helper. Use the following definition:

   ```typescript
   {
     name: 'test-search-index',
     definition: {
       mappings: { dynamic: false }
     }
   }
   ```

3. Assert that the command returns the name of the index: `"test-search-index"`.

4. Run `coll0.listSearchIndexes()` repeatedly every 5 seconds until the following condition is satisfied and store the
   value in a variable `index`:

   - An index with the `name` of `test-search-index` is present and the index has a field `queryable` with a value of
     `true`.

5. Assert that `index` has a property `latestDefinition` whose value is `{ 'mappings': { 'dynamic': false } }`

#### Case 2: Driver can successfully create multiple indexes in batch

1. Create a collection with the "create" command using a randomly generated name (referred to as `coll0`).

2. Create two new search indexes on `coll0` with the `createSearchIndexes` helper. Use the following definitions when
   creating the indexes. These definitions are referred to as `indexDefinitions`.

   ```typescript
   {
     name: 'test-search-index-1',
     definition: {
       mappings: { dynamic: false }
     }
   }

   {
     name: 'test-search-index-2',
     definition: {
       mappings: { dynamic: false }
     }
   }
   ```

3. Assert that the command returns an array containing the new indexes' names:
   `["test-search-index-1", "test-search-index-2"]`.

4. Run `coll0.listSearchIndexes()` repeatedly every 5 seconds until the following conditions are satisfied.

   - An index with the `name` of `test-search-index-1` is present and index has a field `queryable` with the value of
     `true`. Store result in `index1`.
   - An index with the `name` of `test-search-index-2` is present and index has a field `queryable` with the value of
     `true`. Store result in `index2`.

5. Assert that `index1` and `index2` have the property `latestDefinition` whose value is
   `{ "mappings" : { "dynamic" : false } }`

#### Case 3: Driver can successfully drop search indexes

1. Create a collection with the "create" command using a randomly generated name (referred to as `coll0`).

2. Create a new search index on `coll0` with the following definition:

   ```typescript
   {
     name: 'test-search-index',
     definition: {
       mappings: { dynamic: false }
     }
   }
   ```

3. Assert that the command returns the name of the index: `"test-search-index"`.

4. Run `coll0.listSearchIndexes()` repeatedly every 5 seconds until the following condition is satisfied:

   - An index with the `name` of `test-search-index` is present and index has a field `queryable` with the value of
     `true`.

5. Run a `dropSearchIndex` on `coll0`, using `test-search-index` for the name.

6. Run `coll0.listSearchIndexes()` repeatedly every 5 seconds until `listSearchIndexes` returns an empty array.

This test fails if it times out waiting for the deletion to succeed.

#### Case 4: Driver can update a search index

1. Create a collection with the "create" command using a randomly generated name (referred to as `coll0`).

2. Create a new search index on `coll0` with the following definition:

   ```typescript
   {
     name: 'test-search-index',
     definition: {
       mappings: { dynamic: false }
     }
   }
   ```

3. Assert that the command returns the name of the index: `"test-search-index"`.

4. Run `coll0.listSearchIndexes()` repeatedly every 5 seconds until the following condition is satisfied:

   - An index with the `name` of `test-search-index` is present and index has a field `queryable` with the value of
     `true`.

5. Run a `updateSearchIndex` on `coll0`, using the following definition.

   ```typescript
   {
     name: 'test-search-index',
     definition: {
       mappings: { dynamic: true }
     }
   }
   ```

6. Assert that the command does not error and the server responds with a success.

7. Run `coll0.listSearchIndexes()` repeatedly every 5 seconds until the following conditions are satisfied:

   - An index with the `name` of `test-search-index` is present. This index is referred to as `index`.
   - The index has a field `queryable` with a value of `true` and has a field `status` with the value of `READY`.

8. Assert that an index is present with the name `test-search-index` and the definition has a property
   `latestDefinition` whose value is `{ 'mappings': { 'dynamic': true } }`.

#### Case 5: `dropSearchIndex` suppresses namespace not found errors

1. Create a driver-side collection object for a randomly generated collection name. Do not create this collection on the
   server.
2. Run a `dropSearchIndex` command and assert that no error is thrown.

#### Case 6: Driver can successfully create and list search indexes with non-default readConcern and writeConcern

1. Create a collection with the "create" command using a randomly generated name (referred to as `coll0`).

2. Apply a write concern `WriteConcern(w=1)` and a read concern with `ReadConcern(level="majority")` to `coll0`.

3. Create a new search index on `coll0` with the `createSearchIndex` helper. Use the following definition:

   ```typescript
   {
     name: 'test-search-index-case6',
     definition: {
       mappings: { dynamic: false }
     }
   }
   ```

4. Assert that the command returns the name of the index: `"test-search-index-case6"`.

5. Run `coll0.listSearchIndexes()` repeatedly every 5 seconds until the following condition is satisfied and store the
   value in a variable `index`:

   - An index with the `name` of `test-search-index-case6` is present and the index has a field `queryable` with a value
     of `true`.

6. Assert that `index` has a property `latestDefinition` whose value is `{ 'mappings': { 'dynamic': false } }`

#### Case 7: Driver can successfully handle search index types when creating indexes

01. Create a collection with the "create" command using a randomly generated name (referred to as `coll0`).

02. Create a new search index on `coll0` with the `createSearchIndex` helper. Use the following definition:

    ```typescript

      {
        name: 'test-search-index-case7-implicit',
        definition: {
          mappings: { dynamic: false }
        }
      }
    ```

03. Assert that the command returns the name of the index: `"test-search-index-case7-implicit"`.

04. Run `coll0.listSearchIndexes('test-search-index-case7-implicit')` repeatedly every 5 seconds until the following
    condition is satisfied and store the value in a variable `index1`:

    - An index with the `name` of `test-search-index-case7-implicit` is present and the index has a field `queryable`
      with a value of `true`.

05. Assert that `index1` has a property `type` whose value is `search`.

06. Create a new search index on `coll0` with the `createSearchIndex` helper. Use the following definition:

    ```typescript

      {
        name: 'test-search-index-case7-explicit',
        type: 'search',
        definition: {
          mappings: { dynamic: false }
        }
      }
    ```

07. Assert that the command returns the name of the index: `"test-search-index-case7-explicit"`.

08. Run `coll0.listSearchIndexes('test-search-index-case7-explicit')` repeatedly every 5 seconds until the following
    condition is satisfied and store the value in a variable `index2`:

    - An index with the `name` of `test-search-index-case7-explicit` is present and the index has a field `queryable`
      with a value of `true`.

09. Assert that `index2` has a property `type` whose value is `search`.

10. Create a new vector search index on `coll0` with the `createSearchIndex` helper. Use the following definition:

    ```typescript

      {
        name: 'test-search-index-case7-vector',
        type: 'vectorSearch',
        definition: {
          fields: [
             {
                 type: 'vector',
                 path: 'plot_embedding',
                 numDimensions: 1536,
                 similarity: 'euclidean',
             },
          ]
        }
      }
    ```

11. Assert that the command returns the name of the index: `"test-search-index-case7-vector"`.

12. Run `coll0.listSearchIndexes('test-search-index-case7-vector')` repeatedly every 5 seconds until the following
    condition is satisfied and store the value in a variable `index3`:

    - An index with the `name` of `test-search-index-case7-vector` is present and the index has a field `queryable` with
      a value of `true`.

13. Assert that `index3` has a property `type` whose value is `vectorSearch`.

#### Case 8: Driver requires explicit type to create a vector search index

1. Create a collection with the "create" command using a randomly generated name (referred to as `coll0`).

2. Create a new vector search index on `coll0` with the `createSearchIndex` helper. Use the following definition:

   ```typescript

     {
       name: 'test-search-index-case8-error',
       definition: {
         fields: [
            {
                type: 'vector',
                path: 'plot_embedding',
                numDimensions: 1536,
                similarity: 'euclidean',
            },
         ]
       }
     }
   ```

3. Assert that the command throws an exception containing the string "Attribute mappings missing" due to the `mappings`
   field missing.
