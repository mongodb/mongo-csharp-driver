+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Driver"
[menu.main]
  parent = "Reference"
  identifier = "Driver"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## Driver Reference

The MongoDB .NET Driver is mostly just a wrapper around [MongoDB.Driver.Core]({{< relref "reference\driver_core\index.md" >}}). It takes the very verbose and low-level core driver and creates a nice high-level API.

- [Connecting]({{< relref "reference\driver\connecting.md" >}})
	- [Authentication]({{< relref "reference\driver\authentication.md" >}})
	- [SSL]({{< relref "reference\driver\ssl.md" >}})
- [Administration]({{< relref "reference\driver\admin.md" >}})
- [Definitions and Builders]({{< relref "reference\driver\definitions.md" >}})
- [CRUD Operations]({{< relref "reference\driver\crud\index.md" >}})
- [Error Handling]({{< relref "reference\driver\error_handling.md" >}})