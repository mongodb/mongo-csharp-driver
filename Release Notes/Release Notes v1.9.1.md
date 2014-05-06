C#/.NET Driver Version 1.9.1 Release Notes
==========================================

This is a minor release which fixes some issues reported since 1.9.0 was released. It is a recommended
upgrade for anyone using 1.9.0.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/v1.x/Release%20Notes/Release%20Notes%20v1.9.1.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?filter=15166

Documentation on the C# driver can be found at:

http://www.mongodb.org/display/DOCS/CSharp+Language+Center
http://api.mongodb.org/csharp/current/

General Changes
===============

The 1.9.x releases will be the last non-bug fix release supporting .NET 3.5.

The issues fixed in 1.9.1 are:

* [LINQ queries against BsonValue properties whose value is C# null](https://jira.mongodb.org/browse/CSHARP-932)
* [$maxDistance must go inside $near when using GeoJson data](https://jira.mongodb.org/browse/CSHARP-950)
* [Allow ParallelScan to go to secondaries](https://jira.mongodb.org/browse/CSHARP-955)
* [Allow Aggreagate to go to secondaries as long as the pipeline doesn't contain $out](https://jira.mongodb.org/browse/CSHARP-956)
* [Support automatically setting the _id even when using an interface as the document type](https://jira.mongodb.org/browse/CSHARP-958)
* [Don't consider any wnote and jnote values to be errors](https://jira.mongodb.org/browse/CSHARP-959)
* [Fixed a stack overflow when deserializing an interface with a missing _t discriminator value](https://jira.mongodb.org/browse/CSHARP-961)
* [Added SetBits to IndexOptions builder](https://jira.mongodb.org/browse/CSHARP-962)
* [JsonReader support for new $date format with ISO8601 strings as used by 2.6 version mongoexport](https://jira.mongodb.org/browse/CSHARP-963)

Compatibility Changes
---------------------

There were no intentional backwards breaking changes.  If you come across any,
please inform us as soon as possible by email dotnetdriver@mongodb.com or by reporting 
an issue at jira.mongodb.com.
