C# Driver Version 1.7 Release Notes
===================================

This is a major release.

This release has two major goals: to standardize on the name WriteConcern and
to make Acknowledged the new default WriteConcern.

The following classes are being replaced:

- SafeMode is replaced by WriteConcern
- SafeModeResult is replaced by WriteConcernResult
- MongoSafeModeException is replaced by WriteConcernException

To make Acknowledged the new default WriteConcern without breaking any existing
code that might rely on the old default we are introducing a new root class
called MongoClient.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v1.7.md

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.7-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Change%20Log%20v1.7-Driver.txt

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=12915

Documentation on the C# driver can be found at:

http://www.mongodb.org/display/DOCS/CSharp+Language+Center
http://api.mongodb.org/csharp/current/

Standardizing on WriteConcern instead of SafeMode
-------------------------------------------------

Some MongoDB drivers (the C# driver included) have used SafeMode as the name 
for the class which determines whether writes to the database are checked for
errors. We are now standardizing across all drivers to use the name WriteConcern
instead of SafeMode. The C# driver will continue to support the SafeMode class
for a few releases but eventually it will be removed.

You should start using the new WriteConcern class, but we have also provided
an implicit conversion from SafeMode to WriteConcern so any code that passes
a SafeMode argument to a method taking a WriteConcern parameter will still
compile and work.

New MongoClient class and default WriteConcern
----------------------------------------------

The new default WriteConcern is Acknowledged, but we have introduced the new
default in a way that doesn't alter the behavior of existing programs. We
are introducing a new root class called MongoClient that defaults the 
WriteConcern to Acknowledged. The existing MongoServer Create methods are
deprecated but when used continue to default to a WriteConcern of Unacknowledged.

In prior releases you would start using the C# driver with code like this:

    var connectionString = "mongodb://localhost";
    var server = MongoServer.Create(connectionString); // deprecated
    var database = server.GetDatabase("test"); // WriteConcern defaulted to Unacknowledged

The new way to start using the C# driver is:

    var connectionString = "mongodb://localhost";
    var client = new MongoClient(connectionString);
    var server = client.GetServer();
    var database = server.GetDatabase("test"); // WriteConcern defaulted to Acknowledged

If you use the old way to start using the driver the default WriteConcern will
be Unacknowledged, but if you use the new way (using MongoClient) the default
WriteConcern will be Acknowledged.
