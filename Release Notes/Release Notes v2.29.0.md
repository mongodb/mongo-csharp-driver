# .NET Driver Version 2.29.0 Release Notes

This is the general availability release for the 2.29.0 version of the driver.

Version 2.29.0 of the driver has been tested against MongoDB Server version 8.0 and adds support for the following new features in server version 8.0:

+ Support for v2 of the Queryable Encryption range protocol - [CSHARP-4959](https://jira.mongodb.org/browse/CSHARP-4959)
+ Range indexes for Queryable Encryption are now GA - [CSHARP-5057](https://jira.mongodb.org/browse/CSHARP-5057)

The following server 8.0 features are not yet supported and will be supported in a later release of the driver:

+ Improved Bulk Write API - [CSHARP-4145](https://jira.mongodb.org/browse/CSHARP-4145)
+ Update Sort option - [CSHARP-5201](https://jira.mongodb.org/browse/CSHARP-5201)

The full list of issues resolved in this release is available at [CSHARP JIRA project](https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.29.0%20ORDER%20BY%20key%20ASC).

Documentation on the .NET driver can be found [here](https://www.mongodb.com/docs/drivers/csharp/v2.29/).
