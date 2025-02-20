# .NET Driver Version 3.2.1 Release Notes

This is a patch release that addresses some issues reported since 3.2.0 was released:
- Fix potential leak with KMS retry mechanism - [CSHARP-5489](https://jira.mongodb.org/browse/CSHARP-5489)
- Fix stack overflow exception on POCOs that represents tree-like structures - [CSHARP-5493](https://jira.mongodb.org/browse/CSHARP-5493)

An online version of these release notes is available [here](https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v3.2.1.md).

The list of JIRA tickets resolved in this release is available at [CSHARP JIRA project](https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%203.2.1%20ORDER%20BY%20key%20ASC).

Documentation on the .NET driver can be found [here](https://www.mongodb.com/docs/drivers/csharp/v3.2/).

## Upgrading

There are no known backwards breaking changes in this release.
