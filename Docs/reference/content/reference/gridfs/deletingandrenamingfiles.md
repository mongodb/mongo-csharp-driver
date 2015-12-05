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

Use the [`Delete`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_Delete" >}}) or [`DeleteAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_DeleteAsync" >}}) methods to delete a single file identified by its Id.

```csharp
IGridFSBucket bucket;
ObjectId id;
```
```csharp
bucket.Delete(id);
```
```csharp
await bucket.DeleteAsync(id);
```

### Dropping an entire GridFS bucket

Use the [`Drop`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_Drop" >}}) or [`DropAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_DropAsync" >}}) methods to drop an entire GridFS bucket at once.

```csharp
IGridFSBucket bucket;
```
```csharp
bucket.Drop();
```
```csharp
await bucket.DropAsync();
```

{{% note %}}The "fs.files" collection will be dropped first, followed by the "fs.chunks" collection. This is the fastest way to delete all files stored in a GridFS bucket at once.{{% /note %}}

### Renaming a single file

Use the [`Rename`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_Rename" >}}) or [`RenameAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_RenameAsync" >}}) methods to rename a single file identified by its Id.

```csharp
IGridFSBucket bucket;
ObjectId id;
string newFilename;
```
```csharp
bucket.Rename(id, newFilename);
```
```csharp
await bucket.RenameAsync(id, newFilename);
```

### Renaming all revisions of a file

To rename all revisions of a file you first use the [`Find`]({{< apiref "_MongoDB_Driver_GridFS_IGridFSBucket_Find" >}}) or [`FindAsync`]({{< apiref "_MongoDB_Driver_GridFS_IGridFSBucket_FindAsync" >}}) method to find all the revisions, and then loop over the revisions and use the [`Rename`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_Rename" >}}) or [`RenameAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_RenameAsync" >}}) method to rename each revision one at a time.

```csharp
IGridFSBucket bucket;
string oldFilename;
string newFilename;
var filter = Builders<GridFSFileInfo>.Filter.EQ(x => x.Filename, oldFilename);
```
```csharp
var filesCursor = bucket.Find(filter);
var files = filesCursor.ToList();

foreach (var file in files)
{
    bucket.Rename(file.Id, newFilename);
}
```
```csharp
var filesCursor = await bucket.FindAsync(filter);
var files = await filesCursor.ToListAsync();

foreach (var file in files)
{
    await bucket.RenameAsync(file.Id, newFilename);
}
```
