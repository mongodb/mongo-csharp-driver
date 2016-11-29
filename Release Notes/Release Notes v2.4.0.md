# .NET Driver Version 2.4.0

The main new feature of 2.4.0 is support for the new features of the 3.4 version of the server:

* New Decimal128 data type
* New convention for automapping immutable classes for serialization
* New IAggregateFluent methods
  * Bucket and BucketAuto
  * Count
  * Facet
  * GraphLookup
  * ReplaceRoot
  * SortByCount
* New PipelineDefinitionBuilder for building pipelines for CreateView and Facet
* New MaxStaleness property for ReadPreference
* Configurable HeartbeatInterval
* Support for collations
* Driver identifies itself to the server when connecting
* Support for creating read-only views
* Commands that write now support WriteConcern
* LINQ supports new methods: Aggregate, Reverse, Zip

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.4.0.md

The JIRA tickets resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.4%20ORDER%20BY%20key%20ASC

Upgrading

We believe there are only minor breaking changes in classes that normally would not be directly used by applications.
