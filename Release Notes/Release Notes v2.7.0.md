# .NET Driver Version 2.7.0 Release Notes

## Known Issues:
* Incomplete SCRAM-SHA-256 Support in .NET Standard: In .NET Standard, authenticating via SCRAM-SHA-256 may not work with non-ASCII passwords because SaslPrep is not fully implemented due to the lack of a string normalization function in .NET Standard 1.6. Normalizing the password into Unicode Normalization Form KC beforehand MAY help. SCRAM-SHA-1 is the recommended alternative for now.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.7.0.md

The full list of JIRA issues that are currently scheduled to be resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.7.0%20ORDER%20BY%20key%20ASC

The list may change as we approach the release date.

Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/

Upgrading

There are no known backwards breaking changes in this release.
