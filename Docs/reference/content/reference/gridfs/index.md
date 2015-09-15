+++
date = "2015-09-11T00:00:00Z"
draft = false
title = "GridFS"
[menu.main]
  parent = "Reference"
  identifier = "GridFS"
  weight = 40
  pre = "<i class='fa'></i>"
+++

## GridFS

GridFS is a way of storing binary information larger than the maximum document size (currently 16MB). When you upload a file to GridFS the file is broken into chunks and the individual chunks are uploaded. When you download a  file from GridFS the original content is reassembled from the chunks.

- [Getting Started]({{< relref "reference\gridfs\gettingstarted.md" >}})
- [Uploading files]({{< relref "reference\gridfs\uploadingfiles.md" >}})
- [Downloading files]({{< relref "reference\gridfs\downloadingfiles.md" >}})
- [Finding files]({{< relref "reference\gridfs\findingfiles.md" >}})
- [Deleting and renaming files]({{< relref "reference\gridfs\deletingandrenamingfiles.md" >}})
