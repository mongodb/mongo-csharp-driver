# ${PRODUCT_NAME} SSDLC compliance report

This report is available
<a href=https://us-west-2.console.aws.amazon.com/s3/object/csharp-driver-release-assets?region=us-west-2&bucketType=general&prefix=${PRODUCT_NAME}/${PACKAGE_VERSION}/ssdlc_compliance_report.md>here</a>.

<table>
  <tr>
    <th>Product name</th>
    <td><a href="https://github.com/mongodb/mongo-csharp-driver">${PRODUCT_NAME}</a></td>
  </tr>
  <tr>
    <th>Product version</th>
    <td>${PACKAGE_VERSION}</td>
  </tr>
  <tr>
    <th>Report date, UTC</th>
    <td>${REPORT_DATE_UTC}</td>
  </tr>
</table>

## Release creator

This information is available in multiple ways:

<table>
  <tr>
    <th>Evergreen</th>
    <td>
        See the "Submitted by" field in <a href="https://spruce.mongodb.com/version/dot_net_driver_v${PACKAGE_VERSION}_${github_commit}">Evergreen release patch</a>.
    </td>
  </tr>
   <tr>
    <th>Papertrail</th>
    <td>
        Refer to data in Papertrail. There is currently no official way to serve that data.
    </td>
  </tr>
</table>

## Process document

Blocked on <https://jira.mongodb.org/browse/CSHARP-5047>.

The MongoDB SSDLC policy is available at
<https://docs.google.com/document/d/1u0m4Kj2Ny30zU74KoEFCN4L6D_FbEYCaJ3CQdCYXTMc>.

## Third-darty dependency information

There are no dependencies to report vulnerabilities of.
Our [SBOM](https://docs.devprod.prod.corp.mongodb.com/mms/python/src/sbom/silkbomb/docs/CYCLONEDX/) lite
is <https://github.com/mongodb/mongo-csharp-driver/blob/v${PACKAGE_VERSION}/sbom.json>.

## Static analysis findings

Coverity static analysis report is available <a href="https://coverity.corp.mongodb.com/login">here</a>, under mongodb-csharp-driver project.

## Signature information

Blocked on <https://jira.mongodb.org/browse/CSHARP-3050>.
