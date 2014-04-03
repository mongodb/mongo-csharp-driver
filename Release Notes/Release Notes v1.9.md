C#/.NET Driver Version 1.9.0 Release Notes
==============================================

This is a major release which supports all MongoDB server versions since 2.0.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v1.9.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=14713

Documentation on the C# driver can be found at:

http://www.mongodb.org/display/DOCS/CSharp+Language+Center
http://api.mongodb.org/csharp/current/

General Changes
===============

This release is primarily in support of new features offered by server 2.6.  
Additionally, this will be the last non-bug fix release supporting .NET 3.5.

Some of the notable features:

* [Bulk Write Commands](http://docs.mongodb.org/master/release-notes/2.6/#new-write-commands)
* [Text Search](http://docs.mongodb.org/master/release-notes/2.6/#text-search-changes)
* [$out for aggregation](http://docs.mongodb.org/master/release-notes/2.6/#out-stage-to-write-data-to-a-collection)
* [Aggregation cursors](http://docs.mongodb.org/master/release-notes/2.6/#aggregation-operations-now-return-cursors)
* [Improved sorting in aggregation](http://docs.mongodb.org/master/release-notes/2.6/#improved-sorting)
* [Aggregation explain](http://docs.mongodb.org/master/release-notes/2.6/#explain-option-for-the-aggregation-pipeline)
* [x.509 Authentication](http://docs.mongodb.org/master/release-notes/2.6/#x-509-authentication)
* [LDAP Support for Authentication](http://docs.mongodb.org/master/release-notes/2.6/#x-509-authentication)
* [Max execution time enforcement](https://jira.mongodb.org/browse/SERVER-2212)

A full set of server changes in server 2.6 can be found [here](http://docs.mongodb.org/master/release-notes/2.6/).

Compatibility Changes
---------------------

There were no intentional backwards breaking changes.  If you come across any,
please inform us as soon as possible by email dotnetdriver@mongodb.com or by reporting 
an issue at jira.mongodb.com.