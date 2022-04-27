# .NET Driver Version 2.14.0 Release Notes

This is the general availability release for the 2.14.0 version of the driver.

The main new features in 2.14.0 include:

* Added a new improved LINQ provider (known as LINQ3, see: [LINQ3](https://mongodb.github.io/mongo-csharp-driver/2.14/reference/driver/crud/linq3/)), which is available on an opt-in basis
* The current LINQ provider (known as LINQ2) continues to be available and is still the default LINQ provider for this version
* Support for Zstandard and Snappy on Linux and MacOS
* Added connection storm avoidance features
* Added KMIP support for client-side field level encryption (CSFLE)
* Added `srvMaxHosts` URI option to limit the number of `mongos` nodes used in connecting to sharded clusters
* Use "hello" command for monitoring if supported
* Removed support for .NET Framework 4.5.2; minimum is now 4.7.2
* Removed support for .NET Standard 1.5; minimum is now 2.0
* Minimum server version is now MongoDB 3.6+

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.14.0.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.14.0%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

https://mongodb.github.io/mongo-csharp-driver/

