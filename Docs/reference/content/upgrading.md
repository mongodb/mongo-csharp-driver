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

### Backwards compatibility with driver version 2.7.0–2.16.x

Driver versions 2.13.0 to 2.15.X changed `EstimatedDocumentCount` to use
the `$collStats` aggregation stage instead of the `count` command. This
unintentionally broke estimated document counts on views. 2.16.0 and later
switched back to using the `count` command. Unfortunately MongoDB 5.0.0-5.0.8
do not include the `count` command in Stable API v1. If you are using the
Stable API with `EstimatedDocumentCount`, you must upgrade to server version
5.0.9+ or set `strict: false` when configuring `ServerApi` to avoid encountering
errors.

Starting from 2.15.0, feature detection is implemented through maxWireVersion
instead of buildInfo. This should have no user-visible impact.

Driver version 2.14.0 and later only supports MongoDB 3.6+. It cannot connect to
clusters running earlier versions of MongoDB. If you need to connect to
an older cluster, please use driver version 2.13.x or earlier.

Starting in 2.11.0, ``BsonSerializer.Serialize`` will throw an
``InvalidOperationException`` when attempting to serialize an array at
the root of a BSON document. Prior versions would allow this invalid operation.
See https://jira.mongodb.org/browse/CSHARP-2877 and
https://jira.mongodb.org/browse/CSHARP-3889 for more details.
