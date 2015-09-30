+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "What's New"
[menu.main]
  weight = 20
  identifier = "What's New"
  pre = "<i class='fa fa-star'></i>"
+++

## What's New in the MongoDB .NET 2.1 Driver

The 2.1 driver ships with a number of new features. The most notable are discussed below.


## GridFS

[CSHARP-1191](https://jira.mongodb.org/browse/CSHARP-1191) - GridFS support has been implemented.


## LINQ

[CSHARP-935](https://jira.mongodb.org/browse/CSHARP-935) - LINQ support has been rewritten and now targets the aggregation framework. It is a more natural translation and enables many features of LINQ that were previously not able to be translated.

Simply use the new [`AsQueryable`]({{< apiref "M_MongoDB_Driver_IMongoCollectionExtensions_AsQueryable__1" >}}) method to work with LINQ.


## Eventing Implementation

[CSHARP-1374](https://jira.mongodb.org/browse/CSHARP-1374) - An eventing API has been added allowing a user to subscribe to one or more events from the core driver for insight into server discovery, server selection, connection pooling, and commands.