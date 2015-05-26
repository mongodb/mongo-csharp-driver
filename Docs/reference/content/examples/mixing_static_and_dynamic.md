+++
date = "2015-05-26T15:36:56Z"
draft = false
title = "Mixing Static and Dynamic Data"
[menu.main]
  parent = "Examples"
  identifier = "Mixing Static and Dynamic Data"
  weight = 30
  pre = "<i class='fa'></i>"
+++

## Mixing Static and Dynamic Data

Many applications have the notion of known data and unknown data. One such example would be a Blog application where there exists the known data (content, created date, tags) and unknown data (metadata) which a user is allowed to configure.

## The Class

Continuing with the Blog example, below is how we might model the static content for a post.

```csharp
public class Post
{
    public ObjectId Id { get; set; }
  
    public string Content { get; set; }
  
    public DateTime CreatedAtUtc { get; set; }
  
    public List<string> Tags { get; set; }  
}
```

In order to include dynamic data, we have 2 options.

{{% note %}}It is still possible to filter, sort, project, update, etc... this dynamic data using the same tooling described in our [reference documentation]({{< relref "reference\driver\definitions.md" >}}).{{% /note %}}

### BsonDocument Property

The first option is to include a [`BsonDocument`]({{< apiref "T_MongoDB_Bson_BsonDocument" >}}). Our class would then look as follows:

```csharp
class Post
{
    // previous properties
  
    public BsonDocument Metadata { get; set; }
}
```

We can then put all the dynamic data into the `Metadata` property.

```csharp
var post = new Post
{
    Content = "My Post Content",
    CreatedAtUtc = DateTime.UtcNow,
    Tags = new List<string> { "first", "post" },
    Metadata = new BsonDocument("rel", "mongodb")
};
```

In the above example, we set the static properties, but also added a `rel` field with a value of `mongodb` to our metadata. Not all documents may have the `rel` field because it isn't part of the schema. The above document would look like this in the database:

```json
{
    "_id" : ObjectId("5564b6c11de315e733f173cf"),
    "Content": "My Post Content",
    "CreatedAtUtc" : ISODate("2015-05-26T18:09:05.883Z"),
    "Tags" : ["first", "post"],
    "Metadata": {
        "rel": "mongodb"   
    }
}
```

Using this method, all our "dynamic" data is stored underneath the `Metadata` field.

### Extra Elements

The second option is to store dynamic data using the [extra elements]({{< relref "reference\bson\mapping\index.md#supporting-extra-elements" >}}) feature in the Bson library.

Our full class would like this:

```csharp
public class Post
{
    public ObjectId Id { get; set; }
  
    public string Content { get; set; }
  
    public DateTime CreatedAtUtc { get; set; }
  
    public List<string> Tags { get; set; }  
    
    [BsonExtraElements]
    public BsonDocument Metadata { get; set; }
}
```

Using the following code to create `Post`:

```csharp
var post = new Post
{
    Content = "My Post Content",
    CreatedAtUtc = DateTime.UtcNow,
    Tags = new List<string> { "first", "post" },
    Metadata = new BsonDocument("rel", "mongodb")
};
```

Our document in the database would look like this:

```json
{
    "_id" : ObjectId("5564b6c11de315e733f173cf"),
    "Content": "My Post Content",
    "CreatedAtUtc" : ISODate("2015-05-26T18:09:05.883Z"),
    "Tags" : ["first", "post"],
    "rel": "mongodb"   
}
```

Using this method, the `rel` field is stored inline with the static content. 

{{% note class="warning" %}}There is a danger with this approach that dynamic field names could clash with names already being used by members of the class.{{% /note %}} 