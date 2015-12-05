+++
date = "2015-09-14T00:00:00Z"
draft = false
title = "Finding Files"
[menu.main]
  parent = "GridFS"
  identifier = "GridFS Finding Files"
  weight = 40
  pre = "<i class='fa'></i>"
+++

## Finding Files

Each file stored in GridFS has a unique Id assigned to it, and that is the primary way of accessing the stored files.

### Find and FindAsync methods

If you don't know the Id, you can use the [`Find`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_Find" >}}) or [`FindAsync`]({{< apiref "M_MongoDB_Driver_GridFS_IGridFSBucket_FindAsync" >}}) method to find matching files using a filter. The filter must be of type [`FilterDefinition<GridFSFileInfo>`]({{< apiref "T_MongoDB_Driver_FilterDefinition_1" >}}).

For example, to find the newest revision of the file named "securityvideo" uploaded in January 2015:

```csharp
IGridFSBucket bucket;
var filter = Builders<GridFSFileInfo>.Filter.And( 
    Builders<GridFSFileInfo>.Filter.EQ(x => x.Filename, "securityvideo"),
    Builders<GridFSFileInfo>.Filter.GTE(x => x.UploadDateTime, new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
    Builders<GridFSFileInfo>.Filter.LT(x => x.UploadDateTime, new DateTime(2015, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
var sort = Builders<GridFSFileInfo>.Sort.Descending(x => x.UploadDateTime);
var options = new GridFSFindOptions
{
    Limit = 1,
    Sort = sort
};
```
```csharp
using (var cursor = bucket.Find(filter, options))
{
   var fileInfo = cursor.ToList().FirstOrDefault();
   // fileInfo either has the matching file information or is null
}
```
```csharp
using (var cursor = await bucket.FindAsync(filter, options))
{
   var fileInfo = (await cursor.ToListAsync()).FirstOrDefault();
   // fileInfo either has the matching file information or is null
}
```

### GridFSFileInfo class

The [`GridFSFileInfo`]({{< apiref "T_MongoDB_Driver_GridFS_GridFSFileInfo" >}}) is a strongly typed class that represents the information about a GridFS file stored in the "fs.files" collection.

This class is a strongly typed wrapper around a backing [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}). It makes it easier to extract the information available in a files collection documents.

In older drivers it was possible to store arbitrary information at the root level of a files collection document. If you need to access that information you can use the [`BackingDocument`]({{< apiref "P_MongoDB_Driver_GridFS_GridFSFileInfo_BackingDocument" >}}) property to get access to the complete backing document. When uploading new GridFS files you should store any additional information you want to associate with the uploaded file inside the [`Metadata`]({{< apiref "P_MongoDB_Driver_GridFS_GridFSFileInfo_Metadata" >}}) document.
