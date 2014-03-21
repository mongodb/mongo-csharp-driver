MongoDB C# Driver
=================

You can get the latest stable release from the [official Nuget.org feed](http://www.nuget.org/packages/mongocsharpdriver) or from our [github releases page](https://github.com/mongodb/mongo-csharp-driver/releases).

If you'd like to work with the bleeding edge, you can use our [build feed](https://www.myget.org/gallery/mongocsharpdriverbuild). Packages on this feed are pre-release and, while they've passed all our tests, are not yet ready for production.

Build Status
------------

[![Build Status](http://jenkins.bci.10gen.cc:8080/buildStatus/icon?job=mongo-csharp-driver-1.x)](http://jenkins.bci.10gen.cc:8080/job/mongo-csharp-driver-1.x/)

Getting Started
---------------

### Untyped Documents
```C#
using MongoDB.Bson;
using MongoDB.Driver;
```

```C#
var client = new MongoClient("mongodb://localhost:27017");
var server = client.GetServer();
var database = server.GetDatabase("foo");
var collection = database.GetCollection("bar");

collection.Insert(new BsonDocument("Name", "Jack"));

foreach(var document in collection.FindAll())
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
var server = client.GetServer();
var database = server.GetDatabase("foo");
var collection = database.GetCollection<Person>("bar");

collection.Insert(new Person { Name = "Jack" });

foreach(var person in collection.FindAll())
{
	Console.WriteLine(person.Name);
}
```

Documentation
-------------
* [MongoDB](http://www.mongodb.org/)
* [Documentation](http://www.mongodb.org/display/DOCS/CSharp+Language+Center)
* [Api Documentation](http://api.mongodb.org/csharp/)

Questions/Bug Reports
---------------------
* [Discussion Forum](http://groups.google.com/group/mongodb-user)
* [Stack Overflow](http://stackoverflow.com/questions/tagged/mongodb)
* [Jira](https://jira.mongodb.org/browse/CSHARP)
* [Jabbr](https://jabbr.net/#/rooms/mongodb)

If you’ve identified a security vulnerability in a driver or any other MongoDB project, please report it according to the [instructions here](http://docs.mongodb.org/manual/tutorial/create-a-vulnerability-report).

Contributing
------------

Please see our [guidelines](CONTRIBUTING.md) for contributing to the driver.

### Maintainers:
* Robert Stam               robert@mongodb.com
* Craig Wilson              craig.wilson@mongodb.com

### Contributors (in alphabetical order):
* Bit Diffusion Limited     code@bitdiff.com
* Alex Brown                https://github.com/alexjamesbrown
* Justin Dearing            zippy1981@gmail.com
* Dan DeBilt                dan.debilt@gmail.com
* Teun Duynstee             teun@duynstee.com
* Einar Egilsson            https://github.com/einaregilsson
* Ken Egozi                 mail@kenegozi.com
* Daniel Goldman            daniel@stackwave.com
* Simon Green               simon@captaincodeman.com
* Nik Kolev                 nkolev@gmail.com
* Oleg Kosmakov             kosmakoff@gmail.com
* Brian Knight              brianknight10@gmail.com  
* Richard Kreuter           richard@10gen.com
* Kevin Lewis               kevin.l.lewis@gmail.com
* Dow Liu                   redforks@gmail.com
* Alex Lyman                mail.alex.lyman@gmail.com
* Alexander Nagy            optimiz3@gmail.com
* Sridhar Nanjundeswaran    https://github.com/sridharn
* Andrew Rondeau            github@andrewrondeau.com
* Ed Rooth                  edward.rooth@wallstreetjapan.com
* Pete Smith                roysvork@gmail.com
* staywellandy              https://github.com/staywellandy
* Testo                     test1@doramail.com   

If you have contributed and we have neglected to add you to this list please contact one of the maintainers to be added to the list (with apologies).