# .NET Driver Version 2.11.0-beta2 Release Notes

This is a beta release for the 2.11.0 version of the driver.

The main new features in 2.11.0-beta2 support new features in MongoDB 4.4.0. These features include:

* Support for all new
  [``$meta``](https://docs.mongodb.com/manual/reference/operator/projection/meta/)
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
* Support for [hedged reads](https://docs.mongodb.com/master/core/read-preference-hedge-option/index.html)

Other new additions and updates in this beta include:

* A new target of .NET Standard 2.0
* Support for Snappy compression on .NET Core on Windows (in addition
  to existing support on .NET Framework)
* Support for Zstandard compression on Windows on 64-bit platforms
* A new default of enabling certificate revocation checking.
* A new URI option `tlsDisableCertificateRevocationCheck` to disable
  certificate revocation checking.
* An expanded list of retryable write errors via the inclusion of
  `ExceededTimeLimit`, `LockTimeout` and `ClientDisconnect`
* A new GuidRepresentationMode setting to opt-in to the new V3 GuidRepresentation mode
* Improved SDAM (Server Discovery and Monitoring) error handling
* Support for the `AuthorizedDatabases` option in `ListDatabases`
* Session support for `AsQueryable`

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/master/Release%20Notes/Release%20Notes%20v2.11.0-beta2.md

The full list of JIRA issues that are currently scheduled to be resolved in this release is available at:

https://jira.mongodb.org/issues/?jql=project%20%3D%20CSHARP%20AND%20fixVersion%20%3D%202.11.0%20ORDER%20BY%20key%20ASC

The list may change as we approach the release date.

Documentation on the .NET driver can be found at:

http://mongodb.github.io/mongo-csharp-driver/

## Upgrading

### Backwards compatibility with driver version 2.7.0–2.10.x
Because certificate revocation checking is now enabled by default, an
application that is unable to contact the OCSP endpoints and/or CRL
distribution points specified in a server's certificate may experience
connectivity issues (e.g. if the application is behind a firewall with
an outbound whitelist). This is because the driver needs to contact
the OCSP endpoints and/or CRL distribution points specified in the
server’s certificate and if these OCSP endpoints and/or CRL
distribution points are not accessible, then the connection to the
server may fail. In such a scenario, connectivity may be able to be
restored by disabling certificate revocation checking by adding
`tlsDisableCertificateRevocationCheck=true` to the application's connection
string.
