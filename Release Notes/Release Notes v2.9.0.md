# .NET Driver Version 2.9.0 Release Notes

The main new features in 2.9.0 are:

* Distributed transactions on sharded clusters
* The sessions API supports the `IClientSession.WithTransaction()` method to conveniently run a transaction with automatic retries and at-most-once semantics.
* Support for message compression
* SRV polling for `mongodb+srv` connection scheme:  DNS SRV records are periodically polled in order to update the mongos proxy list without having to change client configuration or even restart the client application. This feature is particularly useful when used with a sharded cluster on MongoDB Atlas, which dynamically updates SRV records whenever you resize your Atlas sharded cluster.
* Retryable reads: The diver can automatically retry any read operation that has not yet received any results (due to a transient network error, a "not master" error after a replica set failover, etc.). This feature is enabled by default.
* Retryable writes are now enabled by default.
* Update specification using an aggregation framework pipeline
* SCRAM-SHA authentication caching
* Connections to the replica set primary are no longer closed after a step-down, allowing in progress read operations to complete.
* New aggregate helper methods support running database-level aggregations.
* Aggregate helper methods now support the `$merge` pipeline stage, and builder methods support creation of the new pipeline stage.
* Change stream helpers now support the `startAfter` option.
* Index creation helpers now support wildcard indexes.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.9.0.md

The full list of JIRA issues that are currently scheduled to be resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.9.0%20ORDER%20BY%20key%20ASC


Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/

## Upgrading

Applications with custom retry logic should note that retryable reads and writes default to `true`. Any applications that rely on the driver's old behavior of not automatically retrying reads and writes should update their connection strings to turn off retryable reads/writes as needed. Otherwise, the new default may cause unexpected behavior.

For example, if an application has custom logic that retries reads `n` times, then after upgrading to 2.9.0, the application could end up retrying reads up to `2n` times because the driver defaults to retrying reads.
