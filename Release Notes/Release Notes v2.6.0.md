# .NET Driver Version 2.6.0 Release Notes

The main new feature of 2.6.0 is better support for running when FIPS mode is enabled in the operating system.

* GridFS now has an option to disable MD5 checksum computation
* PasswordEvidence has been refactored to no longer use MD5

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/v2.6.x/Release%20Notes/Release%20Notes%20v2.6.0.md

The JIRA tickets resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.6.0%20ORDER%20BY%20key%20ASC

Upgrading

We believe there are only minor breaking changes in classes that normally would not be directly used by applications.
