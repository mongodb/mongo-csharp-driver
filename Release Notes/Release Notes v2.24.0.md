# .NET Driver Version 2.24.0 Release Notes

This is the general availability release for the 2.24.0 version of the driver.

The main new features in 2.24.0 include:

* New DistinctMany method in IMongoCollection
* Support for latest $dateFromString optional arguments
* ExceededTimeLimit server error is now retryable for reads as well as writes
* Support sort by score in $search stage
* New custom method to test if a field exists or is missing in LINQ (Mql.Exists, Mql.IsMissing, Mql.IsNullOrMissing)
* Enable TLS 1.3 support
* Fix a GridFS error when attempting to upload a GridFS file with a duplicate id and the new file is smaller than the original file
* Add support for Atlas Search $in operator
* Add support for IComparable CompareTo method in LINQ queries
* Add VectorSearchScore builder for $vectorSearch stage
* Update libmongocrypt package version
* Support for nested AsQueryable in LINQ queries (not a common use case but legal, primarily added for use by the EF Core Provider)
* Updated authentication to occur over OP_MSG on supporting servers to improve MongoDB Atlas Serverless compatibility
* Use polling monitoring when running within a FaaS environment such as AWS Lambda
* Fixed segfault in Kerberos (libgssapi) on newer Linux distros

An online version of these release notes is available [here](https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.24.0.md).

The full list of issues resolved in this release is available at [CSHARP JIRA project](https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.24.0%20ORDER%20BY%20key%20ASC).

Documentation on the .NET driver can be found [here](https://www.mongodb.com/docs/drivers/csharp/v2.24/).
