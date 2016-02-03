+++
date = "2015-09-14T00:00:00Z"
draft = false
title = "Downloading Files"
[menu.main]
  parent = "GridFS"
  identifier = "GridFS Downloading Files"
  weight = 30
  pre = "<i class='fa'></i>"
+++

## Downloading Files

There are several ways to download a file from GridFS. The two main approaches are:

1. The driver downloads a file as a byte array or by writing the contents to a [`Stream`]({{< msdnref "system.io.stream" >}}) provided by the application
2. The driver supplies a [`Stream`]({{< msdnref "system.io.stream" >}}) object that the application can read the contents from

### Downloading as a byte array

This is the easiest way to download a file from GridFS, assuming that the file is small enough for the entire contents to be held in memory at once.

```csharp
IGridFSBucket bucket;
ObjectId id;
```
```csharp
var bytes = bucket.DownloadAsBytes(id);
```
```csharp
var bytes = await bucket.DownloadAsBytesAsync(id);
```

### Downloading to a Stream

If you don't want to hold the entire contents of the downloaded file in memory at once, you can have the driver write the contents of the file to a [`Stream`]({{< msdnref "system.io.stream" >}}) provided by the application.

```csharp
IGridFSBucket bucket;
ObjectId id;
Stream destination;
```
```csharp
bucket.DownloadToStream(id, destination);
```
```csharp
await bucket.DownloadToStreamAsync(id, destination);
```

The driver will download the contents of the GridFS file and write them to the destination [`Stream`]({{< msdnref "system.io.stream" >}}). The driver begins writing the contents at the current position of the [`Stream`]({{< msdnref "system.io.stream" >}}).

{{% note class="important" %}}The driver does **not** close the [`Stream`]({{< msdnref "system.io.stream" >}}) when it is done. The [`Stream`]({{< msdnref "system.io.stream" >}}) is owned by the application and it is up to the application to close the [`Stream`]({{< msdnref "system.io.stream" >}}) when it is ready to do so.{{% /note %}}

### Downloading from a Stream

In some cases the application might prefer to read the contents of the GridFS file from a [`Stream`]({{< msdnref "system.io.stream" >}}).

```csharp
IGridFSBucket bucket;
ObjectId id;
```
```csharp
using (var stream = bucket.OpenDownloadStream(id))
{
    // read from stream until end of file is reached
    stream.Close();
}
```
```csharp
using (var stream = await bucket.OpenDownloadStreamAsync(id))
{
    // read from stream until end of file is reached
    await stream.CloseAsync();
}
```

The [`Stream`]({{< msdnref "system.io.stream" >}}) object returned by [`OpenDownloadStream`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_OpenDownloadStream" >}}) or [`OpenDownloadStreamAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_OpenDownloadStreamAsync" >}}) is actually a [`GridFSDownloadStream`]({{< apiref "T_MongoDB_Driver_GridFS_GridFSDownloadStream" >}}) (a subclass of [`Stream`]({{< msdnref "system.io.stream" >}})), which has the following additional members in addition to those found in [`Stream`]({{< msdnref "system.io.stream" >}}):

```csharp
public abstract class GridFSDownloadStream : Stream
{
    public abstract GridFSFileInfo FileInfo { get; }
    public abstract Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken));
};
```

The [`FileInfo`]({{< apiref "P_MongoDB_Driver_GridFS_GridFSDownloadStream_FileInfo" >}}) property contains information about the GridFS file being dowloaded. See the [`Find`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_Find" >}}) or [`FindAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_FindAsync" >}}) method for details about the [`GridFSFileInfo`]({{< apiref "T_MongoDB_Driver_GridFS_GridFSFileInfo" >}}) class.

{{% note %}}Calling [`CloseAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSDownloadStream_CloseAsync." >}}) is optional, but recommended. Since [`Stream`]({{< msdnref "system.io.stream" >}}) is [`IDisposable`]({{< msdnref "system.idisposable" >}}) and it is used inside a using statement, it would be closed automatically when [`Dispose`]({{< msdnref "system.idisposable.dispose" >}}) is called. However, in async programming we want to avoid blocking and calling [`CloseAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSDownloadStream_CloseAsync." >}}) first allows the [`Stream`]({{< msdnref "system.io.stream" >}}) to be closed with an async call. If you call [`CloseAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSDownloadStream_CloseAsync." >}}) first then [`Dispose`]({{< msdnref "system.idisposable.dispose" >}}) will no longer block.{{% /note %}}

By default the driver assumes that you want to read the entire contents of the file from beginning to end, and returns a [`Stream`]({{< msdnref "system.io.stream" >}}) implementation that does not support seeking, which allows for a more efficient implementation.

If you do want to use [`Seek`]({{< msdnref "system.io.stream.seek" >}}) with the returned [`Stream`]({{< msdnref "system.io.stream" >}}), you can use the options parameter to indicate that.

```csharp
IGridFSBucket bucket;
ObjectId id;

var options = new GridFSDownloadOptions
{
    Seekable = true
};
```
```csharp
using (var stream = bucket.OpenDownloadStream(id, options))
{
    // this time the Stream returned supports seeking
    stream.Close();
}
```
```csharp
using (var stream = await bucket.OpenDownloadStreamAsync(id, options))
{
    // this time the Stream returned supports seeking
    await stream.CloseAsync();
}
```

### Downloading by filename

All the previous examples used an Id to specify which GridFS file to download. You can also use a filename to specify which GridFS file to download, but in this case you might need to indicate which "revision" of the file you want to download if there are multiple GridFS files with the same filename.

Revisions are identified using an integer, as follows:

- 0 = the original version uploaded
- 1 = the first revision of the file
- 2 = the second revision of the file
- ...
- -2 the second newest revision of the file
- -1 the newest revision of the file

The default value for the revision is -1 (i.e. the newest revision).

The following examples all download the newest revision:

```csharp
IGridFSBucket bucket;
string filename;
```
```csharp
var bytes = bucket.DownloadAsBytesByName(filename);

// or

Stream destination;
bucket.DownloadToStreamByName(filename, destination);

// or

using (var stream = bucket.OpenDownloadStreamByName(filename))
{
    // read from stream until end of file is reached
    stream.Close(); 
}
```
```csharp
var bytes = await bucket.DownloadAsBytesByNameAsync(filename);

// or

Stream destination;
await bucket.DownloadToStreamByNameAsync(filename, destination);

// or

using (var stream = await bucket.OpenDownloadStreamByNameAsync(filename))
{
    // read from stream until end of file is reached
    await stream.CloseAsync(); 
}
```

If you want to download a different revision, you specify the desired revision using the options parameter.

```csharp
IGridFSBucket bucket;
string filename;

var options = new GridFSDownloadByNameOptions
{
    Revision = 0
};
```
```csharp
var bytes = bucket.DownloadAsBytesByName(filename, options);

// or

Stream destination;
bucket.DownloadToStreamByName(filename, destination, options);

// or

using (var stream = bucket.OpenDownloadStreamByName(filename, options))
{
    // read from stream until end of file is reached
    stream.Close(); 
}
```
```csharp
var bytes = await bucket.DownloadAsBytesByNameAsync(filename, options);

// or

Stream destination;
await bucket.DownloadToStreamByNameAsync(filename, destination, options);

// or

using (var stream = await bucket.OpenDownloadStreamByNameAsync(filename, options))
{
    // read from stream until end of file is reached
    await stream.CloseAsync(); 
}
```

