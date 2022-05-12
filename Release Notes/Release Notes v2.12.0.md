# .NET Driver Version 2.12.0 Release Notes

This is the general availability release for the 2.12.0 version of the driver.

The main new features in 2.12.0 include:

* Support for Hidden Indexes in MongoDB 4.4
* Support for AWS temporary credentials in client-side field level encryption (CSFLE)
* Support for Azure and GCP keystores in client-side field level encryption (CSFLE)
* Support for client-side field level encryption (CSFLE) on Linux and Mac OSX
* Support for GSSAPI/Kerberos on Linux
* Support for .NET Standard 2.1
* Various improvements in serialization performance
* Fixed DNS failures in Kubernetes and Windows Subsystem for Linux (WSL/WSL2)
* Fixed memory leak in heartbeat when cluster is inaccessible
* Fixed SDAM deadlock when invalidating former primary

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.12.0.md

The full list of JIRA issues that are resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.12.0%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

https://www.mongodb.com/docs/drivers/csharp/

