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
* Vincent Kam               vincent.kam@mongodb.com
* Dmitry Lukyanov           dmitry.lukyanov@mongodb.com
* Robert Stam               robert@mongodb.com
* Craig Wilson              craig.wilson@mongodb.com

### Contributors:
* Alexander Aramov          https://github.com/alex687
* Bar Arnon                 https://github.com/I3arnon
* Wan Bachtiar              https://github.com/sindbach
* Mark Benvenuto            https://github.com/markbenvenuto
* Ethan Celletti            https://github.com/Gekctek
* Bit Diffusion Limited     code@bitdiff.com
* Nima Boscarino            https://github.com/NimaBoscarino
* Oscar Bralo               https://github.com/Oscarbralo
* Alex Brown                https://github.com/alexjamesbrown
* Adam Avery Cole           https://github.com/adamaverycole
* Alex Dawes                https://github.com/alexdawes
* Justin Dearing            zippy1981@gmail.com
* Dan DeBilt                dan.debilt@gmail.com
* Teun Duynstee             teun@duynstee.com
* Einar Egilsson            https://github.com/einaregilsson
* Ken Egozi                 mail@kenegozi.com
* Alexander Endris          https://github.com/AlexEndris
* Daniel Goldman            daniel@stackwave.com
* Simon Green               simon@captaincodeman.com
* James Hadwen              james.hadwen@sociustec.com
* Jacob Jewell              jacobjewell@eflexsystems.com
* Danny Kendrick            https://github.com/dkendrick
* Ruslan Khasanbaev         https://github.com/flaksirus
* Konstantin Khitrykh       https://github.com/KonH
* Brian Knight              brianknight10@gmail.com  
* John Knoop                https://github.com/johnknoop
* Andrey Kondratyev         https://github.com/byTimo
* Anatoly Koperin           https://github.com/ExM
* Nik Kolev                 nkolev@gmail.com
* Oleg Kosmakov             https://github.com/kosmakoff
* Maksim Krautsou           https://github.com/MaKCbIMKo
* Richard Kreuter           richard@10gen.com
* Daniel Lee                https://github.com/dlee148
* Kevin Lewis               kevin.l.lewis@gmail.com
* Dow Liu                   redforks@gmail.com
* Chuck Lu                  https://github.com/chucklu
* Alex Lyman                mail.alex.lyman@gmail.com
* John Murphy               https://github.com/jsmurphy
* Alexander Nagy            optimiz3@gmail.com
* Sridhar Nanjundeswaran    https://github.com/sridharn
* Rich Quackenbush          rich.quackenbush@captiveaire.com
* Carl Reinke               https://github.com/mindless2112
* Gian Maria Ricci          https://github.com/alkampfergit
* Andrew Rondeau            github@andrewrondeau.com
* Ed Rooth                  edward.rooth@wallstreetjapan.com
* Sam558                    https://github.com/Sam558
* Sergey Shushlyapin        https://github.com/sergeyshushlyapin
* Alexey Skalozub           pieceofsummer@gmail.com
* Pete Smith                roysvork@gmail.com
* staywellandy              https://github.com/staywellandy
* Vyacheslav Stroy          https://github.com/kreig
* Testo                     test1@doramail.com   
* Zhmayev Yaroslav          https://github.com/salaros
* Aristarkh Zagorodnikov    https://github.com/onyxmaster

If you have contributed and we have neglected to add you to this list please contact one of the maintainers to be added to the list (with apologies).
