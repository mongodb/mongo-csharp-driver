+++
date = "2015-09-41T00:00:00Z"
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

### FindAsync method

If you don't know the Id, you can use FindAsync to find matching files using a filter. The filter must be of type FilterDefinition&lt;GridFSFileInfo&gt;.

For example, to find the newest revision of the file named "securityvideo" uploaded in January 2015 we could use FindAsync like this:

```
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

using (var cursor = await bucket.FindAsync(filter, options))
{
   var fileInfo = (await cursor.ToListAsync()).FirstOrDefault();
   // fileInfo either has the matching file information or is null
}
```

### GridFSFileInfo class

The GridFSFileInfo is a strongly typed class that represents the information about a GridFS file stored in the "fs.files" collection.

It is defined as:

```
public class GridFSFileInfo
{
    public BsonDocument BackingDocument { get; }
    public int ChunkSizeBytes { get; }
    public string Filename { get; }
    public ObjectId Id { get; }
    public long Length { get; }
    public string MD5 { get; }
    public BsonDocument Metadata { get; }
    public DateTime UploadDateTime { get; }
    
    // the following are deprecated but kept for backward compatibility
    public IEnumerable<string> Aliases { get; }
    public string ContentType { get; }
    public BsonValue IdAsBsonValue { get; }
}
```

This class is a strongly typed wrapper around a backing BsonDocument. It makes it easier to extract the information available in a files collection documents.

In older drivers it was possible to store arbitrary information at the root level of a files collection document. If you need to access that information you can use the BackingDocument property to get access to the complete backing document. When uploading new GridFS files you should store any additional information you want to associate with the uploaded file inside the Metadata document.
