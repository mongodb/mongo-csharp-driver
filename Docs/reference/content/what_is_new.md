+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "What's New"
[menu.main]
  weight = 20
  identifier = "What's New"
  pre = "<i class='fa fa-star'></i>"
+++

## What's New in the MongoDB .NET 2.2 Driver

The 2.2 driver ships with a number of new features. The most notable are discussed below.


## Sync API

The 2.0 and 2.1 versions of the .NET driver featured a new async-only API. Version 2.2 introduces sync versions of every async method.


## Support for server 3.2

* Support for bypassing document validation for write operations on collections where document validation has been enabled
* Support for write concern for FindAndModify methods
* Support for read concern
* Builder support for new aggregation stages and new accumulators in $group stage
* Support for version 3 text indexes