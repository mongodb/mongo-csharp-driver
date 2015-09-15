+++
date = "2015-09-14T00:00:00Z"
draft = false
title = "Getting Started"
[menu.main]
  parent = "GridFS"
  identifier = "GridFS Getting Started"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Getting Started

GridFS files are stored in the database using two collections, normally called "fs.files" and "fs.chunks". Each file uploaded to GridFS has one document in the "fs.files" collection containing information about the file and as many chunks as necessary in the "fs.chunks" collection to store the contents of the file.

A GridFS "bucket" is the combination of an "fs.files" and "fs.chunks" collection which together represent a bucket where GridFS files can be stored.

### GridFSBucket

A [`GridFSBucket`]({{< apiref "T_MongoDB_Driver_GridFS_GridFSBucket" >}}) object is the root object representing a GridFS bucket. 

{{% note class="warning" %}}You should always use a [`GridFSBucket`]({{< apiref "T_MongoDB_Driver_GridFS_GridFSBucket" >}}) object to interact with GridFS instead of directly referencing the underlying collections.{{% /note %}}

You create a [`GridFSBucket`]({{< apiref "T_MongoDB_Driver_GridFS_GridFSBucket" >}}) instance by calling its constructor:

```csharp
IMongoDatabase database;

var bucket = new GridFSBucket(database);
```

You can also provide options when instantiating the [`GridFSBucket`]({{< apiref "T_MongoDB_Driver_GridFS_GridFSBucket" >}}) object:

```csharp
IMongoDatabase database;

var bucket = new GridFSBucket(database, new GridFSOptions
{
    BucketName = "videos",
    ChunkSizeBytes = 1048576, // 1MB
    WriteConcern = WriteConcern.Majority,
    ReadPreference = ReadPeference.Secondary
});
```

The [`BucketName`]({{< apiref "P_MongoDB_Driver_GridFS_GridFSBucketOptions_BucketName" >}}) value is the root part of the files and chunks collection names, so in this example the two collections would be named "videos.files" and "videos.chunks" instead of "fs.files" and "fs.chunks".

The [`ChunkSizeBytes`]({{< apiref "P_MongoDB_Driver_GridFS_GridFSBucketOptions_ChunkSizeBytes" >}}) value defines the size of each chunk, and in this example we are overriding the default value of 261120 (255MB).

The [`WriteConcern`]({{< apiref "P_MongoDB_Driver_GridFS_GridFSBucketOptions_WriteConcern" >}}) is used when uploading files to GridFS, and the [`ReadPreference`]({{< apiref "P_MongoDB_Driver_GridFS_GridFSBucketOptions_ReadPreference" >}}) is used when downloading files from GridFS.
