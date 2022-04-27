# .NET Driver Version 2.11.0 Release Notes

The main new features in 2.11.0 support new features in MongoDB 4.4.0. These features include:

* Support for all new
  [``$meta``](https://www.mongodb.com/docs/manual/reference/operator/projection/meta/)
  projections: `randVal`, `searchScore`, `searchHighlights`,
  `geoNearDistance`, `geoNearPoint`, `recordId`, `indexKey` and
  `sortKey`
* Support for passing a hint to update commands as well as
  `findAndModify` update and replace operations
* Support for `allowDiskUse` on find operations
* Support for `MONGODB-AWS` authentication using Amazon Web Services
  (AWS) Identity and Access Management (IAM) credentials
* Support for stapled OCSP (Online Certificate Status Protocol) (macOS only)
* Support for shorter SCRAM (Salted Challenge Response Authentication Mechanism) conversations
* Support for speculative SCRAM and MONGODB-X509 authentication
* Support for the `CommitQuorum` option in `createIndexes`
* Support for [hedged reads](https://www.mongodb.com/docs/master/core/read-preference-hedge-option/index.html)

Other new additions and updates in this release include:

* A new target of .NET Standard 2.0
* Support for Snappy compression on .NET Core on Windows (in addition
  to existing support on .NET Framework)
* Support for Zstandard compression on Windows on 64-bit platforms
* A new URI option `tlsDisableCertificateRevocationCheck` to disable
  certificate revocation checking.
* An expanded list of retryable write errors via the inclusion of
  `ExceededTimeLimit`, `LockTimeout` and `ClientDisconnect`
* A new GuidRepresentationMode setting to opt-in to the new V3 GuidRepresentation mode
* Improved SDAM (Server Discovery and Monitoring) error handling
* Support for the `AuthorizedDatabases` option in `ListDatabases`
* Session support for `AsQueryable`

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.11.0.md

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.11.0%20ORDER%20BY%20key%20ASC

Documentation on the .NET driver can be found at:

https://mongodb.github.io/mongo-csharp-driver/
