+++
date = "2015-09-41T00:00:00Z"
draft = false
title = "Getting Started"
[menu.main]
  parent = "GridFS"
  identifier = "GridFS Getting Started"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Getting Started

GridFS files are stored in the database using two collections, normally called "fs.files" and "fs.chunks". Each file uploaded to GridFS has one document in the "fs.files" containing information about the file and as many chunks as necessary in the "fs.chunks" collection to store the contents of the file.

A GridFS "bucket" is the combination of an "fs.files" and "fs.chunks" collection which together represent a bucket where GridFS files can be stored.

### GridFSBucket

A GridFSBucket object is the root object representing a GridFS bucket. You should always use a GridFSBucket object to interact with GridFS instead of directly referencing the underlying collections.

You create a GridFSBucket instance by calling its constructor:

```
IMongoDatabase database;

var bucket = new GridFSBucket(database);
```

You can also provide options when instantiating the GridFSBucket object:

```
IMongoDatabase database;

var bucket = new GridFSBucket(database, new GridFSOptions
{
    BucketName = "videos",
    ChunkSizeBytes = 1048576, // 1MB
    WriteConcern = WriteConcern.Majority,
    ReadPreference = ReadPeference.Secondary
});
```

The BucketName value is the root part of the files and chunks collection names, so in this example the two collections would be named "videos.files" and "videos.chunks" instead of "fs.files" and "fs.chunks".

The ChunkSizeBytes value defines the size of each chunk, and in this example we are overriding the default value of 261120 (255MB).

The WriteConcern is used when uploading files to GridFS, and the ReadPreference is used when downloading files from GridFS.
