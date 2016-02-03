+++
date = "2015-09-14T00:00:00Z"
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
2. The driver supplies a [`Stream`]({{< msdnref "system.io.stream" >}}) object that the application can write the contents to

Files uploaded to GridFS are identified either by Id or by Filename. Each uploaded file is assigned a unique Id of type [`ObjectId`]({{< apiref "T_MongoDB_Bson_ObjectId" >}}). If multiple files are uploaded to GridFS with the same Filename, they are considered to be "revisions" of the same file, and the [`UploadDateTime`]({{< apiref "P_MongoDB_Driver_GridFS_GridFSFileInfo_UploadDateTime" >}}) is used to decide whether one revision is newer than another.

### Uploading from a byte array

This is the easiest way to upload a file to GridFS, assuming that you have, or can easily get, the contents of the file as a byte array.

```csharp
IGridFSBucket bucket;
bytes[] source;
```
```csharp
var id = bucket.UploadFromBytes("filename", source);
```
```csharp
var id = await bucket.UploadFromBytesAsync("filename", source);
```

The id returned is the unique [`ObjectId`]({{< apiref "T_MongoDB_Bson_ObjectId" >}}) assigned by the driver to represent this revision of "filename" in the GridFS bucket.

When using the [`UploadFromBytes`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_UploadFromBytes" >}}) or [`UploadFromBytesAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_UploadFromBytesAsync" >}}) method you can also provide additional options.

```csharp
IGridFSBucket bucket;
bytes[] source;
var options = new GridFSUploadOptions
{
    ChunkSizeBytes = 64512, // 63KB
    Metadata = new BsonDocument
    {
        { "resolution", "1080P" },
        { "copyrighted", true }
    } 
};  
```
```csharp
var id = bucket.UploadFromBytes("filename", source, options);
```
```csharp
var id = await bucket.UploadFromBytesAsync("filename", source, options);
```

In this example we are overriding the [`ChunkSizeBytes`]({{< apiref "P_MongoDB_Driver_GridFS_GridFSUploadOptions_ChunkSizeBytes" >}}) defined in the [`GridFSBucket`]({{< apiref "T_MongoDB_Driver_GridFS_GridFSBucket" >}}) and providing additional metadata to be stored with the GridFS file.

### Uploading from a Stream

If the contents of the file you want to upload are more easily accessible using a [`Stream`]({{< msdnref "system.io.stream" >}}) than a byte array (or are too large to load entirely into memory at once), you can use the [`UploadFromStream`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_UploadFromStream" >}}) or [`UploadFromStreamAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_UploadFromStreamAsync" >}}) method instead. 

```csharp
IGridFSBucket bucket;
Stream source;
```
```csharp
var id = bucket.UploadFromStream("filename", source);
```
```csharp
var id = await bucket.UploadFromStreamAsync("filename", source);
```

The driver will read from the current position of the source [`Stream`]({{< msdnref "system.io.stream" >}}) and upload everything read from the [`Stream`]({{< msdnref "system.io.stream" >}}) until the [`Stream`]({{< msdnref "system.io.stream" >}}) reaches end of file.

The [`UploadFromStream`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_UploadFromStream" >}}) and [`UploadFromStreamAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_UploadFromStreamAsync" >}}) methods also support providing additional options, just like the example above for [`UploadFromBytes`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_UploadFromBytes" >}}) and [`UploadFromBytesAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_UploadFromBytessAsync" >}}).

### Uploading to a Stream

Sometimes it is more convenient for an application to upload a file to GridFS by writing the contents to an output [`Stream`]({{< msdnref "system.io.stream" >}}) rather than providing the contents to the driver either as a byte array or an input [`Stream`]({{< msdnref "system.io.stream" >}}).

```csharp
IGridFSBucket bucket;
```
```csharp
using (var stream = bucket.OpenUploadStream("filename"))
{
    var id = stream.Id; // the unique Id of the file being uploaded
    // write the contents of the file to stream using synchronous Stream methods
    stream.Close(); // optional because Dispose calls Close
}
```
```csharp
using (var stream = await bucket.OpenUploadStreamAsync("filename"))
{
    var id = stream.Id; // the unique Id of the file being uploaded
    // write the contents of the file to stream using asynchronous Stream methods
    await stream.CloseAsync(); // optional but recommended so Dispose does not block
}
```

The [`Stream`]({{< msdnref "system.io.stream" >}}) object returned by [`OpenUploadStream`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_OpenUploadStream" >}}) or [`OpenUploadStreamAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_OpenUploadStreamAsync" >}}) is actually a [`GridFSUploadStream`]({{< apiref "T_MongoDB_Driver_GridFS_GridFSUploadStream" >}}) (a subclass of [`Stream`]({{< msdnref "system.io.stream" >}})), which has the following additional members in addition to those found in [`Stream`]({{< msdnref "system.io.stream" >}}):

```csharp
public abstract class GridFSUploadStream : Stream
{
    public abstract ObjectId Id { get; }
    public abstract void Abort(CancellationToken cancellationToken = default(CancellationToken));
    public abstract Task AbortAsync(CancellationToken cancellationToken = default(CancellationToken));
    public abstract void Close(CancellationToken cancellationToken = default(CancellationToken));
    public abstract Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken));
};
```

The [`Id`]({{< apiref "P_MongoDB_Driver_GridFS_GridFSUploadStream_Id" >}}) property allows the calling application to know the unique Id that was assigned to the file being uploaded. The application can call [`Abort`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSDownloadStream_Abort." >}}) or [`AbortAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSDownloadStream_AbortAsync." >}}) to abort the upload operation part-way through if it needs to. [`CloseAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSUploadStream_CloseAsync." >}}) can be called instead of [`Dispose`]({{< msdnref "system.idisposable.dispose" >}}) to close the [`Stream`]({{< msdnref "system.io.stream" >}}) in an async way.

{{% note %}}Calling [`CloseAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSUploadStream_CloseAsync." >}}) is optional, but recommended. Since [`Stream`]({{< msdnref "system.io.stream" >}}) is [`IDisposable`]({{< msdnref "system.idisposable" >}}) and it is used inside a using statement, it would be closed automatically when [`Dispose`]({{< msdnref "system.idisposable.dispose" >}}) is called. However, in async programming we want to avoid blocking and calling [`CloseAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSUploadStream_CloseAsync." >}}) first allows the [`Stream`]({{< msdnref "system.io.stream" >}}) to be closed with an async call. If you call [`CloseAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSUploadStream_CloseAsync." >}}) first then [`Dispose`]({{< msdnref "system.idisposable.dispose" >}}) will no longer block.{{% /note %}}

When opening an upload stream using [`OpenUploadStream]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_OpenUploadStream" >}}) or [`OpenUploadStreamAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_OpenUploadStreamAsync" >}}) you can provide the same options that are supported by [`UploadFromStream`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_UploadFromStream" >}}) and [`UploadFromStreamAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_UploadFromStreamAsync" >}}):

```csharp
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
```
```csharp
using (var stream = bucket.OpenUploadStream("filename", options))
{
    var id = stream.Id; // the unique Id of the file being uploaded
    // write the contents of the file to stream
    stream.Close();
}
```
```csharp
using (var stream = await bucket.OpenUploadStreamAsync("filename", options))
{
    var id = stream.Id; // the unique Id of the file being uploaded
    // write the contents of the file to stream
    await stream.CloseAsync();
}
```

