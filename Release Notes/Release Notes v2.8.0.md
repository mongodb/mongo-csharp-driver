# .NET Driver Version 2.8.0 Release Notes

This is a minor release.

An online version of these release notes is available at:

<https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.8.0.md>

The list of JIRA tickets resolved in this release is available at:

<https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.8.0%20ORDER%20BY%20key%20ASC>

Documentation on the .NET driver can be found at:

<http://mongodb.github.io/mongo-csharp-driver/>

## Upgrading

* The minimum .NET Framework version we support has been changed from 4.5 to 4.5.2.

* If you were having compatibility problems when adding a dependency (either directly or indirectly) on either
System.Runtime.InteropServices.RuntimeInformation or DnsClient this is a recommended upgrade. We now depend
on the latest version of those packages.
