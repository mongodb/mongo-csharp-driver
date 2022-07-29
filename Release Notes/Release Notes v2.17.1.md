# .NET Driver Version 2.17.1 Release Notes

This is a patch release that fixes a potential data corruption bug in `RewrapManyDataKey` when rotating encrypted data encryption keys backed by GCP or Azure key services.

The following conditions will trigger this bug:

- A GCP-backed or Azure-backed data encryption key being rewrapped requires fetching an access token for decryption of the data encryption key.

The result of this bug is that the key material for all data encryption keys being rewrapped is replaced by new randomly generated material, destroying the original key material.

To mitigate potential data corruption, upgrade to this version or higher before using `RewrapManyDataKey` to rotate Azure-backed or GCP-backed data encryption keys. A backup of the key vault collection should **always** be taken before key rotation.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.17.1.md

The list of JIRA tickets resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.17.1%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/

## Upgrading

There are no known backwards breaking changes in this release.
