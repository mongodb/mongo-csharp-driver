C#/.NET Driver Version 1.9.2 Release Notes
==========================================

This is a minor release which fixes some issues reported since 1.9.0 was released. It is a recommended
upgrade for anyone using 1.9.1.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/v1.x/Release%20Notes/Release%20Notes%20v1.9.2.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?filter=15440

Documentation on the C# driver can be found at:

http://www.mongodb.org/display/DOCS/CSharp+Language+Center
http://api.mongodb.org/csharp/current/

General Changes
===============

The 1.9.x releases will be the last non-bug fix release supporting .NET 3.5.

The issues fixed in 1.9.2 are:

[Kerberos authentication occasionally crashes runtime](|https://jira.mongodb.org/browse/CSHARP-980)
[Allow $external special database to be used](https://jira.mongodb.org/browse/CSHARP-986)
[Handle pre-2.6 upserted identifier](https://jira.mongodb.org/browse/CSHARP-987)
[Altered ObjectId generation to take into account the AppDomain](https://jira.mongodb.org/browse/CSHARP-993)
[Added 2.6 extended json support to JsonReader](https://jira.mongodb.org/browse/CSHARP-995)
[SetMaxScan rendered $maxscan instead of $maxScan](https://jira.mongodb.org/browse/CSHARP-1000)
[Updated output of JsonWriter to include 2.6 extended json support](https://jira.mongodb.org/browse/CSHARP-1001)
[Replica set tags GetHashCode was calculated improperly](https://jira.mongodb.org/browse/CSHARP-1005)


Compatibility Changes
---------------------

There were no intentional backwards breaking changes.  If you come across any,
please inform us as soon as possible by email dotnetdriver@mongodb.com or by reporting 
an issue at jira.mongodb.com.