+++
date = "2015-09-41T00:00:00Z"
draft = false
title = "Uploading Files"
[menu.main]
  parent = "GridFS"
  identifier = "GridFS Uploading Files"
  weight = 20
  pre = "<i class='fa'></i>"
+++

## Uploading Files

There are several ways to upload a file to GridFS. The two main approaches are:

1. The driver uploads a file from a source provided by the application
2. The driver supplies a Stream object that the application can write the contents to

Files uploaded to GridFS are identified either by Id or by Filename. Each uploaded file is assigned a unique Id of type ObjectId. If multiple files are uploaded to GridFS with the same Filename, they are considered to be "revisions" of the same file, and the UploadDate is used to decide whether one revision is newer than another.

### Uploading from a byte array

This is the easiest way to upload a file to GridFS, assuming that you have, or can easily get, the contents of the file as a byte array.

```
IGridFSBucket bucket;
bytes[] source;

var id = await bucket.UploadFromBytesAsync("filename", source);
```

The id returned is the unique ObjectId assigned by the driver to represent this revision of "filename" in the GridFS bucket.

When using UploadFromBytesAsync you can also provide additional options.

```
IGridFSBucket bucket;
bytes[] source;

var id = await bucket.UploadFromBytesAsync("filename", source, new GridFSUploadOptions
{
    ChunkSizeBytes = 64512, // 63KB
    Metadata = new BsonDocument
    {
        { "resolution", "1080P" },
        { "copyrighted", true }
    }   
});
```

In this example we are overriding the ChunkSizeBytes defined in the GridFSBucket and providing additional metadata to be stored with the GridFS file.

### Uploading from a Stream

If the contents of the file you want to upload are more easily accessible using a Stream than a byte array (or are too large to load entirely into memory at once), you can use the UploadFromStreamAsync method instead. 

```
IGridFSBucket bucket;
Stream source;

var id = await bucket.UploadFromStreamAsync("filename", source);
```

The driver will read from the current position of the source Stream and upload everything read from the Stream until the Stream reaches end of file.

The UploadFromStreamAsync method also supports providing additional options, just like the example above for UploadFromBytesAsync.

### Uploading to a Stream

Sometimes it is more convenient for an application to upload a file to GridFS by writing the contents to an output Stream rather than providing the contents to the driver either as a byte array or an input Stream.

```
IGridFSBucket bucket;

using (var stream = await bucket.OpenUploadStreamAsync("filename"))
{
    var id = stream.Id; // the unique Id of the file being uploaded
    // write the contents of the file to stream
    await stream.CloseAsync();
}
```

The Stream object returned by OpenUploadStreamAsync is actually a GridFSUploadStream (a subclass of Stream), which has the following additional members in addition to those found in Stream:

```
public abstract class GridFSUploadStream : Stream
{
    public abstract ObjectId Id { get; }
    public abstract Task AbortAsync(CancellationToken cancellationToken = default(CancellationToken));
    public abstract Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken));
};
```

The Id property allows the calling application to know the unique Id that was assigned to the file being uploaded. AbortAsync allows the application to abort the upload operation part-way through if it needs to. CloseAsync can be called instead of Dispose to close the Stream in an async way.

Calling CloseAsync is optional, but recommended. Since Stream is IDisposable and it is used inside a using statement, it would be closed automatically when Dispose is called. However, in async programming we want to avoid blocking and calling CloseAsync first allows the Stream to be closed with an async call. If you call CloseAsync first then Dispose will no longer block.

When opening an upload stream using OpenUploadStreamAsync you can provide the same options that are supported by UploadFromStreamAsync:

```
IGridFSBucket bucket;

var options = new GridFSUploadOptions
{
    ChunkSizeBytes = 64512, // 63KB
    Metadata = new BsonDocument
    {
        { "resolution", "1080P" },
        { "copyrighted", true }
    }   
});

using (var stream = await bucket.OpenUploadStreamAsync("filename", options))
{
    var id = stream.Id; // the unique Id of the file being uploaded
    // write the contents of the file to stream
    await stream.CloseAsync();
}
```

