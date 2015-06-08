# .NET Driver Version 2.0.1 Release Notes

This is a patch release which fixes some issues reported since 2.0 was released. It is a recommended
upgrade for anyone using 2.0.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/v2.0.x/Release%20Notes/Release%20Notes%20v2.0.1.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?filter=17849

Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/

## General Changes

The key issues fixed in 2.0.1 are:

- [CSHARP-1264](https://jira.mongodb.org/browse/CSHARP-1264) WaitQueueSize now properly configures the server selection wait queue.
- [CSHARP-1280](https://jira.mongodb.org/browse/CSHARP-1280) Deserializing fails if a document contains unmapped field which name starts with the same name as currently mapped class property.
- [CSHARP-1265](https://jira.mongodb.org/browse/CSHARP-1265) Update variants allow sending empty documents as update statements, which results in a replacement.


## Compatibility Changes

There were no intentional backwards breaking changes.  If you come across any,
please inform us as soon as possible by email dotnetdriver@mongodb.com or by reporting 
an issue at jira.mongodb.com.