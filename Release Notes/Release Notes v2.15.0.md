# .NET Driver Version 2.15.0 Release Notes

This is the general availability release for the 2.15.0 version of the driver.

The main new features in 2.15.0 include:

* Reimplement CMAP Maintance and SDAM threads to use dedicated threads
* Support for Window Functions using $setWindowFields
* Support $merge and $out executing on secondaries
* Publish symbols to NuGet.org Symbol Server and add Source Link support for improved debugging experience
* Switch to using maxWireVersion rather than buildInfo to determine feature support
* Support 'let' option for multiple CRUD commands
* Support authorizedCollections option for listCollections helpers
* Add support for 'comment' field in multiple commands for profiling
* Upgrade DnsClient.NET up to 1.6.0. This should address problems that some users have had in containerized environments like Kubernetes.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.15.0.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.15.0%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

https://mongodb.github.io/mongo-csharp-driver/

