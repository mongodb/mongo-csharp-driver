# .NET Driver Version 3.2.0 Release Notes

This is the general availability release for the 3.2.0 version of the driver.

The main new features in 3.2.0 include:

+ Support casting from an interface to a concrete type in a filter expression - [CSHARP-4572](https://jira.mongodb.org/browse/CSHARP-4572)
+ Support for BSON Binary Vector subtype that helps make MongoDB Vector Search more efficient and easy to work with - [CSHARP-5202](https://jira.mongodb.org/browse/CSHARP-5202)
+ Support for additional methods in LINQ, such as Append, OfType, Repeat, SequenceEqual - [CSHARP-4872](https://jira.mongodb.org/browse/CSHARP-4872), [CSHARP-4876](https://jira.mongodb.org/browse/CSHARP-4876), [CSHARP-4878](https://jira.mongodb.org/browse/CSHARP-4878), [CSHARP-4880](https://jira.mongodb.org/browse/CSHARP-4880)
+ Support strings with "Range" operator for Atlas Search - [CSHARP-5429](https://jira.mongodb.org/browse/CSHARP-5429)
+ Added `ObjectSerializerAllowedTypesConvention` to more easily configure allowed types for `ObjectSerializer` - [CSHARP-4495](https://jira.mongodb.org/browse/CSHARP-4495)
+ Added Kubernetes Support for OIDC - [CSHARP-5026](https://jira.mongodb.org/browse/CSHARP-5026)
+ Added `BsonDateOnlyOptionsAttribute` to control the serialization of `DateOnly` - [CSHARP-5345](https://jira.mongodb.org/browse/CSHARP-5345)
+ Added array field support for "In" and "Range" operators in Atlas Search - [CSHARP-5430](https://jira.mongodb.org/browse/CSHARP-5430)
+ Fixed an error where `BulkWrite` command would not be retried when throwing a `ClientBulkWriteException` - [CSHARP-5449](https://jira.mongodb.org/browse/CSHARP-5449)
+ Fixed a bug where `BsonGuidRepresentationAttribute` will not work on nullable GUIDs - [CSHARP-5456](https://jira.mongodb.org/browse/CSHARP-5456)
+ `EnumRepresentationConvention` will now also be applied to collection of enums - [CSHARP-2096](https://jira.mongodb.org/browse/CSHARP-2096)
+ Automatically retry KMS requests on transient errors - [CSHARP-5017](https://jira.mongodb.org/browse/CSHARP-5017)
+ LINQ aggregate Group now supports $addToSet operator  - [CSHARP-5446](https://jira.mongodb.org/browse/CSHARP-5446)
+ Minor bug fixes and improvements.

The full list of issues resolved in this release is available at [CSHARP JIRA project](https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%203.2.0%20ORDER%20BY%20key%20ASC).

Documentation on the .NET driver can be found [here](https://www.mongodb.com/docs/drivers/csharp/v3.2/).
