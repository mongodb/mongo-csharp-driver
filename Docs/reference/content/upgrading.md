+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Upgrading"
[menu.main]
  parent = "What's New"
  identifier = "Upgrading"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Breaking Changes

### Backwards compatibility with driver version 2.7.0–2.10.x
An application that is unable to contact the OCSP endpoints and/or CRL
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
