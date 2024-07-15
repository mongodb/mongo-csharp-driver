MongoDB C# Driver
=================

You can get the latest stable release from the [official Nuget.org feed](https://www.nuget.org/packages/MongoDB.Driver) or from our [github releases page](https://github.com/mongodb/mongo-csharp-driver/releases).

Getting Started
---------------

### Untyped Documents
```C#
using MongoDB.Bson;
using MongoDB.Driver;
```

```C#
var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("foo");
var collection = database.GetCollection<BsonDocument>("bar");

await collection.InsertOneAsync(new BsonDocument("Name", "Jack"));

var list = await collection.Find(new BsonDocument("Name", "Jack"))
    .ToListAsync();

foreach(var document in list)
{
    Console.WriteLine(document["Name"]);
}
```

### Typed Documents

```C#
using MongoDB.Bson;
using MongoDB.Driver;
```

```C#
public class Person
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
}
```

```C#
var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("foo");
var collection = database.GetCollection<Person>("bar");

await collection.InsertOneAsync(new Person { Name = "Jack" });

var list = await collection.Find(x => x.Name == "Jack")
    .ToListAsync();

foreach(var person in list)
{
    Console.WriteLine(person.Name);
}
```

Documentation
-------------
* [MongoDB](https://www.mongodb.com/docs)
* [Documentation](https://www.mongodb.com/docs/drivers/csharp/current/)

Questions/Bug Reports
---------------------
* [MongoDB Community Forum](https://www.mongodb.com/community/forums/tags/c/data/drivers-odms/7/dot-net)
* [Jira](https://jira.mongodb.org/browse/CSHARP)

If you’ve identified a security vulnerability in a driver or any other MongoDB project, please report it according to the [instructions here](https://www.mongodb.com/docs/manual/tutorial/create-a-vulnerability-report).

Contributing
------------

Please see our [guidelines](CONTRIBUTING.md) for contributing to the driver.

Thank you to [everyone](https://github.com/mongodb/mongo-csharp-driver/graphs/contributors) who has contributed to this project.
