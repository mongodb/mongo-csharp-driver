C#/.NET Driver Version 1.10.1 Release Notes
=================================================================

This is a minor release and contains no API changes.

It also fixes some issues reported since 1.10.0 was released.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/v1.x/Release%20Notes/Release%20Notes%20v1.10.1.md

Documentation on the C# driver can be found at:

http://www.mongodb.org/display/DOCS/CSharp+Language+Center
http://api.mongodb.org/csharp/current/

General Changes
===============

The 1.x releases will be the last non-bug fix release supporting .NET 3.5.

The issues fixed in 1.10.1 are:

[CSHARP-1351](https://jira.mongodb.org/browse/CSHARP-1351) - ObjectSerializer should not be dependent on the order of the _t and _v elements.


Compatibility Changes
---------------------

There were no intentional backwards breaking changes.  If you come across any,
please inform us as soon as possible by email dotnetdriver@mongodb.com or by reporting 
an issue at jira.mongodb.com.
