+++
date = "2015-09-14T00:00:00Z"
draft = false
title = "Deleting and Renaming Files"
[menu.main]
  parent = "GridFS"
  identifier = "GridFS Deleting and Renaming Files"
  weight = 50
  pre = "<i class='fa'></i>"
+++

## Deleting and Renaming Files

These methods allow you to delete or rename GridFS files.

### Deleting a single file

The [`DeleteAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSBucket_DeleteAsync_1" >}}) method is used to delete a single file identified by its Id.

```csharp
IGridFSBucket bucket;
ObjectId id;

await bucket.DeleteAsync(id);
```

### Dropping an entire GridFS bucket

If you want to drop an entire GridFS bucket at once use the [`DropAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSBucket_DropAsync" >}}) method.

```csharp
IGridFSBucket bucket;

await bucket.DropAsync();
```

{{% note %}}The "fs.files" collection will be dropped first, followed by the "fs.chunks" collection. This is the fastest way to delete all files stored in a GridFS bucket at once.{{% /note %}}

### Renaming a single file

The [`RenameAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSBucket_RenameAsync_1" >}}) method is used to rename a single file identified by its Id.

```csharp
IGridFSBucket bucket;
ObjectId id;
string newFilename;

await bucket.RenameAsync(id, newFilename);
```

### Renaming all revisions of a file

If you want to rename all revisions of a file you first use [`FindAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSBucket_FindAsync" >}}) to find their ids and then call [`RenameAsync`]({{< apiref "M_MongoDB_Driver_GridFS_GridFSBucket_RenameAsync_1" >}}) in a loop to rename them one at a time.

```csharp
IGridFSBucket bucket;
ObjectId id;
string oldFilename;
string newFilename;

var filter = Builders<GridFSFileInfo>.Filter.EQ(x => x.Filename, oldFilename);
var filesCursor = await bucket.FindAsync(filter);
var files = await filesCursor.ToListAsync();

foreach (var file in files)
{
    await bucket.RenameAsync(file.Id, newFilename);
}
```
