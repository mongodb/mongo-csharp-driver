C#/.NET Driver Version 1.10 Release Notes
=========================================

This is a minor release which is fully compatible with server version 3.0 and fully supports
the new features introduced by server version 3.0.

It also fixes some issues reported since 1.9.2 was released.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/v1.x/Release%20Notes/Release%20Notes%20v1.10.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?filter=16411

Documentation on the C# driver can be found at:

http://www.mongodb.org/display/DOCS/CSharp+Language+Center
http://api.mongodb.org/csharp/current/

General Changes
===============

The 1.x releases will be the last non-bug fix release supporting .NET 3.5.

The issues fixed in 1.10 are:

- Support for changes in server 3.0
- Support SCRAM-SHA1 authentication
- Deprecate classes, properties and methods that will be removed in version 2.0
- other minor fixes (see JIRA tickets)

Note about SCRAM-SHA1 authenticaton
-----------------------------------

Starting with the MongoDB 3.0 release, the SCRAM-SHA1 authentication protocol is supported. By
itself, this will not cause any compatibility issues. However, before updating the server's
authentication schema such that the MONGODB-CR protocol is no longer available, you must
replace any calls to:

    MongoCredential.CreateMongoCRCredential(...)

with calls to:

    MongoCredential.CreateCredential(...)

Compatibility Changes
---------------------

There were no intentional backwards breaking changes.  If you come across any,
please inform us as soon as possible by email dotnetdriver@mongodb.com or by reporting 
an issue at jira.mongodb.com.
