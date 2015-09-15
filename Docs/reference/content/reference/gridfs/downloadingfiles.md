+++
date = "2015-09-41T00:00:00Z"
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

1. The driver downloads a file as a byte array or by writing the contents to a Stream provided by the application
2. The driver supplies a Stream object that the application can read the contents from

### Downloading as a byte array

This is the easiest way to download a file from GridFS, assuming that the file is small enough for the entire contents to be held in memory at once.

```
IGridFSBucket bucket;
ObjectId id;

var bytes = await bucket.DownloadAsBytesAsync(id);
```

### Downloading to a Stream

If you don't want to hold the entire contents of the downloaded file in memory at once, you can have the driver write the contents of the file to a Stream provided by the application.

```
IGridFSBucket bucket;
ObjectId id;
Stream destination;

await bucket.DownloadToStreamAsync(id, destination);
```

The driver will download the contents of the GridFS file and write them to the destination Stream. The driver begins writing the contents at the current position of the Stream, and does **not** close the Stream when it is done. The Stream is owned by the application and it is up to the application to close the Stream when it is ready to do so.

### Downloading from a Stream

In some cases it the application might prefer to read the contents of the GridFS file from a Stream.

```
IGridFSBucket bucket;
ObjectId id;

using (var stream = await bucket.OpenDownloadStreamAsync(id))
{
    // read from stream until end of file is reached
    await stream.CloseAsync();
}
```

The Stream object returned by OpenDownloadStreamAsync is actually a GridFSDownloadStream (a subclass of Stream), which has the following additional members in addition to those found in Stream:

```
public abstract class GridFSDownloadStream : Stream
{
    public abstract GridFSFileInfo FileInfo { get; }
    public abstract Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken));
};
```

The FileInfo property contains information about the GridFS file being dowloaded. See the FindAsync method for details about the GridFSFileInfo class.

Calling CloseAsync is optional, but recommended. Since Stream is IDisposable and it is used inside a using statement, it would be closed automatically when Dispose is called. However, in async programming we want to avoid blocking and calling CloseAsync first allows the Stream to be closed with an async call. If you call CloseAsync first then Dispose will no longer block.

By default the driver assumes that you want to read the entire contents of the file from beginning to end, and returns a Stream implementation that does not support seeking, which allows for a more efficient implementation.

If you do want to use Seek with the returned Stream, you can use the options parameter to indicate that.

```
IGridFSBucket bucket;
ObjectId id;

var options = new GridFSDownloadOptions
{
    Seekable = true
};

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

```
IGridFSBucket bucket;
string filename;

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

```
IGridFSBucket bucket;
string filename;

var options = new GridFSDownloadByNameOptions
{
    Revision = 0
};

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

