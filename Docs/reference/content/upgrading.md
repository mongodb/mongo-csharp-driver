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

For most users there should be no breaking changes in version 2.8.0 of the driver.

Because we have updated several external dependencies to newer versions, you might encounter compatibility
issues if your application depends on different versions of those dependencies. This might apply to you
if you depend on System.Runtime.InteropServices.RuntimeInformation or DnsClient. Note that even if your
application does not depend on these directly, it is possible that you depend on them indirectly.
