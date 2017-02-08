# .NET Driver Version 2.4.2 Release Notes

This is a patch release that fixes a few bugs reported since 2.4.1 was released.

The main change is adding back support for using a <TField> which is not the same as the actual
field type in filter builder methods and the Distinct method. Normally <TField> is expected to
match the actual field type, but it turns out there are cases where one might want to specify
a type for <TField> that does not match the field type exactly. See the following tickets for
more information about these changes

https://jira.mongodb.org/browse/CSHARP-1884
https://jira.mongodb.org/browse/CSHARP-1890
https://jira.mongodb.org/browse/CSHARP-1891

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.4.2.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.4.2%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/

Upgrading

There are no known backwards breaking changes in this release.
