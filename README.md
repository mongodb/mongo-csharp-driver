MongoDB C# Driver
=================

You can get the latest stable release from the [official Nuget.org feed](http://www.nuget.org/packages/mongocsharpdriver) or from our [github releases page](https://github.com/mongodb/mongo-csharp-driver/releases).

If you'd like to work with the bleeding edge, you can use our [custom feed](https://www.myget.org/gallery/mongodb). Some packages on this feed are pre-release and, while they've passed all our tests, are not yet ready for production.


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
* [MongoDB](http://www.mongodb.org/)
* [Documentation](http://mongodb.github.io/mongo-csharp-driver/)

Questions/Bug Reports
---------------------
* [Discussion Forum](http://groups.google.com/group/mongodb-user)
* [Stack Overflow](http://stackoverflow.com/questions/tagged/mongodb)
* [Jira](https://jira.mongodb.org/browse/CSHARP)

If you’ve identified a security vulnerability in a driver or any other MongoDB project, please report it according to the [instructions here](http://docs.mongodb.org/manual/tutorial/create-a-vulnerability-report).

Contributing
------------

Please see our [guidelines](CONTRIBUTING.md) for contributing to the driver.

### Maintainers:
* Robert Stam               robert@mongodb.com
* Craig Wilson              craig.wilson@mongodb.com

### Contributors:
* Alexander Aramov          https://github.com/alex687
* Bar Arnon                 https://github.com/I3arnon
* Bit Diffusion Limited     code@bitdiff.com
* Alex Brown                https://github.com/alexjamesbrown
* Justin Dearing            zippy1981@gmail.com
* Dan DeBilt                dan.debilt@gmail.com
* Teun Duynstee             teun@duynstee.com
* Einar Egilsson            https://github.com/einaregilsson
* Ken Egozi                 mail@kenegozi.com
* Daniel Goldman            daniel@stackwave.com
* Simon Green               simon@captaincodeman.com
* James Hadwen              james.hadwen@sociustec.com
* Jacob Jewell              jacobjewell@eflexsystems.com
* Danny Kendrick            https://github.com/dkendrick
* Brian Knight              brianknight10@gmail.com  
* Nik Kolev                 nkolev@gmail.com
* Oleg Kosmakov             kosmakoff@gmail.com
* Maksim Krautsou           https://github.com/MaKCbIMKo
* Richard Kreuter           richard@10gen.com
* Kevin Lewis               kevin.l.lewis@gmail.com
* Dow Liu                   redforks@gmail.com
* Alex Lyman                mail.alex.lyman@gmail.com
* Alexander Nagy            optimiz3@gmail.com
* Sridhar Nanjundeswaran    https://github.com/sridharn
* Rich Quackenbush          rich.quackenbush@captiveaire.com
* Andrew Rondeau            github@andrewrondeau.com
* Ed Rooth                  edward.rooth@wallstreetjapan.com
* Alexey Skalozub           pieceofsummer@gmail.com
* Pete Smith                roysvork@gmail.com
* staywellandy              https://github.com/staywellandy
* Testo                     test1@doramail.com   

If you have contributed and we have neglected to add you to this list please contact one of the maintainers to be added to the list (with apologies).