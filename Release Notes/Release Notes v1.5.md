C# Driver Version 1.5 Release Notes
=====================================

*** summarize highlights here ***

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.5-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.5-Driver.txt

These release notes describe the changes at a higher level, and omit describing
some of the minor changes.

Breaking changes
----------------

** summarize breaking changes here ***

JIRA issues resolved
--------------------

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=11900

BSON library changes
====================

Topics:
- C# null vs BsonNull.Value distinction (null in the .NET world, BsonNull.Value in the BsonDocument object model)
- medium trust
- KeyValuePairSerializer and KeyValuePairSerializationOptions
- classes that implement IDictionary or IDictionary<TKey, TValue> are now always serialized by a DictionarySerializer
- classes that implement IEnumerable ot IEnumerable<T> are now always serialized by a CollectionSerializer
- IBsonSerializer interface broken up into 4 separate interfaces
- renaming of BsonDefaultSerializer to BsonDefaultSerializationProvider and moving of many methods to BsonSerializer
- we now enforce the assumption that you can't register a serializer for a class that implements IBsonSerializable
- we now enforce the assumption that you can only register a serializer for a class once

Driver changes
==============

Topics:
- new simpler untyped query builder, how it's different, why it's better
- how to continue using the old query builder if you have to (create an alias with a using statement)
- describe the new typed builders (query and all the others)
- new connection string options (journal and uuidRepresentation)
- GridFS changes so read-only users can download files when authentication is enabled
- GridFS changes to support files greater than 2GB
- describe new features in LINQ queries (see changes to PredicateTranslator and SelectQuery)
- describe new WIX based installer and how it differs from old installer (e.g. no GAC)


