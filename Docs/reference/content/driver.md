+++
date = "2015-03-18T16:56:14Z"
draft = false
title = "Driver Tutorial"
[menu.main]
  weight = 20
  pre = "<i class='fa'></i>"
+++

Driver Tutorial
===================

Introduction
------------

This tutorial introduces the officially supported C\# Driver for
MongoDB. The C\# Driver consists of two libraries: the BSON Library and
the C\# Driver. The BSON Library can be used independently of the C\#
Driver if desired. The C\# Driver requires the BSON Library.

You may also be interested in the C\# Driver Serialization
Tutorial \</tutorial/serialize-documents-with-the-csharp-driver\>. It is
a separate tutorial because it covers quite a lot of material.

Downloading
-----------

The C\# Driver is available in source and binary form. While the BSON
Library can be used independently of the C\# Driver they are both stored
in the same repository.

The simplest way to get started is by using the nuget package. The
[package page](http://www.nuget.org/packages/mongocsharpdriver/) will
provide details on using nuget.

The source may be downloaded from
[github.com](http://github.com/mongodb/mongo-csharp-driver).

We use `msysgit` as our Windows git client. It can be downloaded from:
http://msysgit.github.com.

To clone the repository run the following commands from a git bash
shell:

``` {.sourceCode .sh}
cd <parentdirectory>
git config --global core.autocrlf true
git clone git://github.com/mongodb/mongo-csharp-driver.git
cd mongo-csharp-driver
git config core.autocrlf true
```

You must set the global setting for `core.autocrlf` to `true` before
cloning the repository. After you clone the repository, we recommend you
set the local setting for `core.autocrlf` to `true` (as shown above) so
that future changes to the global setting for `core.autocrlf` do not
affect this repository. If you then want to change your global setting
for `core.autocrlf` to `false` run:

``` {.sourceCode .sh}
git config --global core.autocrlf false
```

The typical symptom of problems with the setting for `core.autocrlf` is
git reporting that an entire file has been modified (because of
differences in the line endings). It is rather tedious to change the
setting of `core.autocrlf` for a repository after it has been created,
so it is important to get it right from the start.

-   You can download a zip file of the source files (without cloning the
    repository) by clicking on the Downloads button at:
    http://github.com/mongodb/mongo-csharp-driver
-   You can download binaries (in both `.msi` and `.zip` formats) from:
    https://github.com/mongodb/mongo-csharp-driver/releases

Building
--------

We are currently building the C\# Driver with Visual Studio 2012. The
name of the solution file is CSharpDriver.sln.

Running Unit Tests
------------------

The unit tests depend on NUnit 2.6, which is included in the Tools
folder of the repository. You can build the C\# Driver without
installing NUnit, but you must have a test runner capable of running
NUnit tests in order to run the tests.

There are three projects containing unit tests:

1.  `BsonUnitTests`
2.  `DriverUnitTests`
3.  `DriverUnitTestsVB`

The `BsonUnitTests` do not connect to a MongoDB server. The
`DriverUnitTests` and `DriverUnitTestsVB` connect to an instance of
MongoDB running on the default port (27017) on localhost.

If you are running Visual Studio 2012, you can use the NUnit Test
Adapter extension to run the unit tests. This is by far the easiest
solution.

> **note**
>
> The NUnit Test Adapter for Visual Studio 2012 is an extension that
> must be installed through the Visual Studio Extension Manager.

An alternative is to use the NUnit Test Runner. An easy way to use the
NUnit Test Runner is to set one of the unit test projects as the startup
project and configure the project settings as follows (using
`BsonUnitTests` as an example):

-   On the **Debug** tab:
    -   Set `Start Action` to: `Start External Program`
    -   Set `external program` to: .\\\\Tools\\\\NUnit\\\\nunit.exe
    -   Set `command line arguments` to:
        `BsonUnitTests.csproj /config:Debug /run`
    -   Set `working directory` to: the directory where
        BsonUnitTest.csproj is located.

Repeat the above steps for the `Release` configuration (using
`/config:Release` instead) if you also want to run unit tests for
`Release` builds.

To run the `DriverUnitTests` and `DriverUnitTestsVB` perform the same
steps (modified as necessary).

Installing
----------

If you want to install the C\# Driver on your machine, you can use the
setup program (see above for download instructions). The setup program
is very simple and just copies the DLLs to your specified installation
directory.

If you downloaded the binaries zip file simply extract the files and
place them wherever you want them to be.

> **note**
>
> If you download the `.zip` file, Windows might require you to
> "Unblock" the help file. If Windows asks "Do you want to open this
> file?" when you double click on the CSharpDriverDocs.chm file, clear
> the check box next to "Always ask before opening this file" before
> pressing the Open button. Alternatively, you can right click on the
> CSharpDriverDocs.chm file and select `Properties`, and then press the
> `Unblock` button at the bottom of the `General` tab. If the `Unblock`
> button is not present then the help file does not need to be
> unblocked.

References and Namespaces
-------------------------

To use the C\# Driver you must add references to the following DLLs:

1.  MongoDB.Bson.dll
2.  MongoDB.Driver.dll

> **note**
>
> If you used nuget to pull down the assemblies, these references likely
> already exist.

As a minimum add the following using statements to your source files:

``` {.sourceCode .csharp}
using MongoDB.Bson;
using MongoDB.Driver;
```

Additionally you will frequently add some of the following using
statements:

``` {.sourceCode .csharp}
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
```

In some cases you might add some of the following using statements if
you are using some of the optional parts of the C\# Driver:

``` {.sourceCode .csharp}
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Wrappers;
```

The BSON Library
----------------

The C\# Driver is built on top of the BSON Library, which handles all
the details of the BSON specification, including: I/O, serialization,
and an in-memory object model of BSON documents.

The important classes of the BSON object model are: `BsonType`,
`BsonValue`, `BsonElement`, `BsonDocument` and `BsonArray`.

### `BsonType`

This enumeration is used to specify the type of a BSON value. It is
defined as:

``` {.sourceCode .csharp}
public enum BsonType {
    Double = 0x01,
    String = 0x02,
    Document = 0x03,
    Array = 0x04,
    Binary = 0x05,
    Undefined = 0x06,
    ObjectId = 0x07,
    Boolean = 0x08,
    DateTime = 0x09,
    Null = 0x0a,
    RegularExpression = 0x0b,
    JavaScript = 0x0d,
    Symbol = 0x0e,
    JavaScriptWithScope = 0x0f,
    Int32 = 0x10,
    Timestamp = 0x11,
    Int64 = 0x12,
    MinKey = 0xff,
    MaxKey = 0x7f
}
```

### `BsonValue` and Subclasses

`BsonValue` is an abstract class that represents a typed BSON value.
There is a concrete subclass of `BsonValue` for each of the values
defined by the `BsonType` enum. There are several ways to obtain an
instance of `BsonValue`:

-   Use a public constructor (if available) of a subclass of `BsonValue`
-   Use a static `Create` method of `BsonValue`
-   Use a static `Create` method of a subclass of `BsonValue`
-   Use a static property of a subclass of `BsonValue`
-   Use an implicit conversion to `BsonValue`

The advantage of using the static `Create` methods is that they can
return a pre-created instance for frequently used values. They can also
return `null` (which a constructor cannot) which is useful for handling
optional elements when creating `BsonDocuments` using functional
construction. The static properties refer to pre-created instances of
frequently used values. Implicit conversions allow you to use primitive
.NET values wherever a `BsonValue` is expected, and the .NET value will
automatically be converted to a `BsonValue`.

### `BsonType` Property

`BsonValue` has a property called `BsonType` that you can use to query
the actual type of a `BsonValue`. The following example shows several
ways to determine the type of a `BsonValue`:

``` {.sourceCode .csharp}
BsonValue value;
if (value.BsonType == BsonType.Int32) {
    // we know value is an instance of BsonInt32
}
if (value is BsonInt32) {
    // another way to tell that value is a BsonInt32
}
if (value.IsInt32) {
    // the easiest way to tell that value is a BsonInt32
}
```

### `As[Type]` Properties

`BsonValue` has a number of properties that cast a `BsonValue` to one of
its subclasses or a primitive .NET type. It is important to note that
these all are casts, not conversions. They will throw an
`InvalidCastException` if the `BsonValue` is not of the corresponding
type. See also the To[Type] methods
\<totype-conversion-methods\> which do conversions, and the
Is[Type] properties \<istype-properties\> which you can use to query the
type of a `BsonValue` before attempting to use one of the `As[Type]`
properties.

``` {.sourceCode .csharp}
BsonDocument document;
string name = document["name"].AsString;
int age = document["age"].AsInt32;
BsonDocument address = document["address"].AsBsonDocument;
string zip = address["zip"].AsString;
```

### `Is[Type]` Properties

`BsonValue` has the following boolean properties, you can use to test
what kind of `BsonValue` it is. These can be used as follows:

``` {.sourceCode .csharp}
BsonDocument document;
int age = -1;
if (document.Contains["age"] && document["age"].IsInt32) {
    age = document["age"].AsInt32;
}
```

### `To[Type]` Conversion Methods

Unlike the `As[Type]` methods, the `To[Type]` methods perform some
limited conversion between convertible types, like `int` and `double`.

The `ToBoolean` method never fails. It uses JavaScript's definition of
truthiness: `false`, `0`, `0.0`, `NaN`, `BsonNull`, `BsonUndefined` and
`""` are `false`, and everything else is `true` (include the string
`"false"`).

The `ToBoolean` method is particularly useful when the documents you are
processing might have inconsistent ways of recording `true`/`false`
values:

``` {.sourceCode .csharp}
if (employee["ismanager"].ToBoolean()) {
    // we know the employee is a manager
    // works with many ways of recording boolean values
}
```

The `ToDouble`, `ToInt32`, and `ToInt64` methods never fail when
converting between numeric types, though the value might be truncated if
it doesn't fit in the target type. A string can be converted to a
numeric type, but an exception will be thrown if the string cannot be
parsed as a value of the target type.

### Static Create Methods

Because `BsonValue` is an abstract class you cannot create instances of
`BsonValue` (only instances of concrete subclasses). `BsonValue` has a
static `Create` method that takes an argument of type `object` and
determines at runtime the actual type of `BsonValue` to create.
Subclasses of `BsonValue` also have static `Create` methods tailored to
their own needs.

#### Implicit Conversions

Implicit conversions are defined from the following .NET types to
`BsonValue`:

-   `bool`
-   `byte[]`
-   `DateTime`
-   `double`
-   `Enum`
-   `Guid`
-   `int`
-   `long`
-   `ObjectId`
-   `Regex`
-   `string`

These eliminate the need for almost all calls to `BsonValue`
constructors or `Create` methods. For example:

``` {.sourceCode .csharp}
BsonValue b = true; // b is an instance of BsonBoolean
BsonValue d = 3.14159; // d is an instance of BsonDouble
BsonValue i = 1; // i is an instance of BsonInt32
BsonValue s = "Hello"; // s is an instance of BsonString
```

### `BsonMaxKey`, `BsonMinKey`, `BsonNull` and `BsonUndefined`

These classes are singletons, so only a single instance of each class
exists. You refer to these instances using the static `Value` property
of each class:

``` {.sourceCode .csharp}
document["status"] = BsonNull.Value;
document["priority"] = BsonMaxKey.Value;
```

Note that C\# `null` and `BsonNull.Value` are two different things. The
latter is an actual C\# object that represents a BSON `null` value (it's
a subtle difference, but plays an important role in functional
construction).

### `ObjectId` and `BsonObjectId`

`ObjectId` is a struct that holds the raw value of a BSON `ObjectId`.
`BsonObjectId` is a subclass of `BsonValue` whose Value property is of
type `ObjectId`.

Here are some common ways of creating `ObjectId` values:

``` {.sourceCode .csharp}
var id1 = new ObjectId(); // same as ObjectId.Empty
var id2 = ObjectId.Empty; // all zeroes
var id3 = ObjectId.GenerateNewId(); // generates new unique Id
var id4 = ObjectId.Parse("4dad901291c2949e7a5b6aa8"); // parses a 24 hex digit string
```

> **note**
>
> The first example behaves differently in C\# than in JavaScript. In
> C\# it creates an `ObjectId` of all zeroes, but in JavaScript it
> generates a new unique `Id`. This difference can't be avoided because
> in C\# the default constructor of a value type always initializes the
> value to all zeros.

`BsonElement`
-------------

A `BsonElement` is a name/value pair, where the value is a `BsonValue`.
It is used as the building block of `BsonDocument`, which consists of
zero or more elements. You will rarely create `BsonElements` directly,
as they are usually created indirectly as needed. For example:

``` {.sourceCode .csharp}
document.Add(new BsonElement("age", 21)); // OK, but next line is shorter
document.Add("age", 21); // creates BsonElement automatically
```

`BsonDocument`
--------------

A `BsonDocument` is a collection of name/value pairs (represented by
`BsonElements`). It is an in-memory object model of a BSON document.
There are three ways to create and populate a `BsonDocument`:

1.  Create a new document and call `Add` and `Set` methods.
2.  Create a new document and use the fluent interface `Add` and `Set`
    methods.
3.  Create a new document and use C\#'s collection initializer syntax
    (recommended).

### `BsonDocument` Constructor

`BsonDocument` has the following constructors:

-   `BsonDocument()`
-   `BsonDocument(string name, BsonValue value)`
-   `BsonDocument(BsonElement element)`
-   `BsonDocument(Dictionary<string, object> dictionary)`
-   `BsonDocument(Dictionary<string, object> dictionary, IEnumerable<string> keys)`
-   `BsonDocument(IDictionary dictionary)`
-   `BsonDocument(IDictionary dictionary, IEnumerable<string> keys)`
-   `BsonDocument(IDictionary<string, object> dictionary)`
-   `BsonDocument(IDictionary<string, object> dictionary, IEnumerable<string> keys)`
-   `BsonDocument(IEnumerable<BsonElement> elements)`
-   `BsonDocument(params BsonElement[] elements)`
-   `BsonDocument(bool allowDuplicateNames)`

The first two are the ones you are most likely to use. The first creates
an empty document, and the second creates a document with one element
(in both cases you can of course add more elements).

All the constructors (except the one with `allowDuplicateNames`) simply
call the `Add` method that takes the same parameters, so refer to the
corresponding `Add` method for details about how the new document is
initially populated.

A `BsonDocument` normally does not allow duplicate names, but if you
must allow duplicate names call the constructor with the
`allowDuplicateNames` parameter and pass in `true`. It is *not*
recommended that you allow duplicate names, and this option exists only
to allow handling existing BSON documents that might have duplicate
names. MongoDB makes no particular guarantees about whether it supports
documents with duplicate names, so be cautious about sending any such
documents you construct to the server.

### Create a New Document and Call `Add` and `Set` Methods

This is a traditional step by step method to create and populate a
document using multiple C\# statements. For example:

``` {.sourceCode .csharp}
BsonDocument book = new BsonDocument();
book.Add("author", "Ernest Hemingway");
book.Add("title", "For Whom the Bell Tolls");
```

### Create a New Document and Use the Fluent Interface `Add` and `Set` Methods

This is similar to the previous approach but the fluent interface allows
you to chain the various calls to `Add` so that they are all a single
C\# statement. For example:

``` {.sourceCode .csharp}
BsonDocument book = new BsonDocument()
    .Add("author", "Ernest Hemingway")
    .Add("title", "For Whom the Bell Tolls");
```

### Create a New Document and Use C\#'s Collection Initializer Syntax (Recommended)

This is the recommended way to create and initialize a `BsonDocument` in
one statement. It uses C\#'s collection initializer syntax:

``` {.sourceCode .csharp}
BsonDocument book = new BsonDocument {
    { "author", "Ernest Hemingway" },
    { "title", "For Whom the Bell Tolls" }
};
```

The compiler translates this into calls to the matching `Add` method:

``` {.sourceCode .csharp}
BsonDocument book = new BsonDocument();
book.Add("author", "Ernest Hemingway");
book.Add("title", "For Whom the Bell Tolls");
```

A common mistake is to forget the inner set of braces. This will result
in a compilation error. For example:

``` {.sourceCode .csharp}
BsonDocument bad = new BsonDocument {
    "author", "Ernest Hemingway"
};
```

is translated by the compiler to:

``` {.sourceCode .csharp}
BsonDocument bad = new BsonDocument();
bad.Add("author");
bad.Add("Ernest Hemingway");
```

which results in a compilation error because there is no `Add` method
that takes a single string argument.

### Creating Nested BSON Documents

Nested BSON documents are created by setting the value of an element to
a BSON document. For example:

``` {.sourceCode .csharp}
BsonDocument nested = new BsonDocument {
    { "name", "John Doe" },
    { "address", new BsonDocument {
        { "street", "123 Main St." },
        { "city", "Centerville" },
        { "state", "PA" },
        { "zip", 12345}
    }}
};
```

This creates a top level document with two elements (`name` and
`address`). The value of `address` is a nested BSON document.

### `Add` Methods

`BsonDocument` has the following overloaded `Add` methods:

-   `Add(BsonElement element)`
-   `Add(Dictionary<string, object> dictionary)`
-   `Add(Dictionary<string, object> dictionary, IEnumerable<string> keys)`
-   `Add(IDictionary dictionary)`
-   `Add(IDictionary dictionary, IEnumerable<string> keys)`
-   `Add(IDictionary<string, object> dictionary)`
-   `Add(IDictionary<string, object> dictionary, IEnumerable<string> keys)`
-   `Add(IEnumerable<BsonElement> elements)`
-   `Add(string name, BsonValue value)`
-   `Add(string name, BsonValue value, bool condition)`

It is important to note that sometimes the `Add` methods *do NOT* add a
new element. If the value supplied is `null` (or the condition supplied
in the last overload is `false`) then the element isn't added. This
makes it really easy to handle optional elements without having to write
any if statements or conditional expressions.

For example:

``` {.sourceCode .csharp}
BsonDocument document = new BsonDocument {
    { "name", name },
    { "city", city }, // not added if city is null
    { "dob", dob, dobAvailable } // not added if dobAvailable is false
};
```

is more compact and readable than:

``` {.sourceCode .csharp}
BsonDocument document = new BsonDocument();
document.Add("name", name);
if (city != null) {
    document.Add("city", city);
}
if (dobAvailable) {
    document.Add("dob", dob);
}
```

If you want to add a `BsonNull` if a value is missing you have to say
so. A convenient way is to use C\#'s `null` coalescing operator as
follows:

``` {.sourceCode .csharp}
BsonDocument = new BsonDocument {
    { "city", city ?? BsonConstants.Null }
};
```

The `IDictionary` overloads initialize a `BsonDocument` from a
dictionary. Each key in the dictionary becomes the name of a new
element, and each value is mapped to a matching `BsonValue` and becomes
the value of the new element. The overload with the keys parameter lets
you select which dictionary entries to load (you might also use the keys
parameter to control the order in which the elements are loaded from the
dictionary).

### Accessing `BsonDocument` Elements

The recommended way to access `BsonDocument` elements is to use one of
the following indexers:

-   `BsonValue this[int index]`
-   `BsonValue this[string name]`
-   `BsonValue this[string name, BsonValue defaultValue]`

Note that the return value of the indexers is `BsonValue`, not
`BsonElement`. This actually makes `BsonDocuments` much easier to work
with (if you ever need to get the actual `BsonElements` use
`GetElement`).

We've already seen samples of accessing `BsonDocument` elements. Here
are some more:

``` {.sourceCode .csharp}
BsonDocument book;
string author = book["author"].AsString;
DateTime publicationDate = book["publicationDate"].AsDateTime;
int pages = book["pages", -1].AsInt32; // default value is -1
```

BsonArray
---------

This class is used to represent BSON arrays. While arrays happen to be
represented externally as BSON documents (with a special naming
convention for the elements), the `BsonArray` class is unrelated to the
`BsonDocument` class because they are used very differently.

### Constructors

`BsonArray` has the following constructors:

-   `BsonArray()`
-   `BsonArray(IEnumerable<bool> values)`
-   `BsonArray(IEnumerable<BsonValue> values)`
-   `BsonArray(IEnumerable<DateTime> values)`
-   `BsonArray(IEnumerable<double> values)`
-   `BsonArray(IEnumerable<int> values)`
-   `BsonArray(IEnumerable<long> values)`
-   `BsonArray(IEnumerable<ObjectId> values)`
-   `BsonArray(IEnumerable<string> values)`
-   `BsonArray(IEnumerable values)`

All the constructors with a parameter call the matching `Add` method.
The multiple overloads are needed because C\# does not provide automatic
conversions from `IEnumerable<T>` to `IEnumerable<object>`.

### `Add` and `AddRange` Methods

`BsonArray` has the following `Add` methods:

-   `BsonArray Add(BsonValue value)`
-   `BsonArray AddRange(IEnumerable<bool> values)`
-   `BsonArray AddRange(IEnumerable<BsonValue> values)`
-   `BsonArray AddRange(IEnumerable<DateTime> values)`
-   `BsonArray AddRange(IEnumerable<double> values)`
-   `BsonArray AddRange(IEnumerable<int> values)`
-   `BsonArray AddRange(IEnumerable<long> values)`
-   `BsonArray AddRange(IEnumerable<ObjectId> values)`
-   `BsonArray AddRange(IEnumerable<string> values)`
-   `BsonArray AddRange(IEnumerable values)`

Note that the `Add` method takes a single parameter. To create and
initialize a `BsonArray` with multiple values use any of the following
approaches:

``` {.sourceCode .csharp}
// traditional approach
BsonArray a1 = new BsonArray();
a1.Add(1);
a2.Add(2);

// fluent interface
BsonArray a2 = new BsonArray().Add(1).Add(2);

// values argument
int[] values = new int[] { 1, 2 };
BsonArray a3 = new BsonArray(values);

// collection initializer syntax
BsonArray a4 = new BsonArray { 1, 2 };
```

### Indexer

Array elements are accessed using an integer index. Like `BsonDocument`,
the type of the elements is `BsonValue`. For example:

``` {.sourceCode .csharp}
BsonArray array = new BsonArray { "Tom", 39 };
string name = array[0].AsString;
int age = array[1].AsInt32;
```

LazyBsonDocument and LazyBsonArray
----------------------------------

The lazy classes are special in that they defer the deserialiation of
BSON until it is needed. This is useful for when you only need a field
or two out of a complex document because it will not incur the cost of
deserializing the entire document or array, but just the pieces that are
necessary. This deserialization occurs a level at a time. For example,
in the following document, asking for field `FirstName` will deserialize
`LastName` and `BirthDate` as well, but it will leave `Addresses` in its
serialized form until they are accessed.

``` {.sourceCode .javascript}
{
  FirstName: "Jack",
  LastName: "McJack",
  BirthDate: new ISODate("..."),
  Addresses: [
     { Line1: "123 AnyStreet", City: "Anytown" }
  ]
}
```

It can be used as follows:

``` {.sourceCode .csharp}
var db = server.GetDatabase("foo");
var col = db.GetCollection<LazyBsonDocument>("bar");

foreach (var doc in col.Find(Query.GT("age", 15)))
{
    using (doc)
    {
        // the first access will incur the cost of deserializing
        // FirstName, LastName, and BirthDate.  However,
        // Addresses will remain in its serialized form.
        var firstName = doc["FirstName"];

        // this was already deserialized and is a simple
        // dictionary lookup.
        var lastName = doc["LastName"];

        Console.WriteLine("{0} {1}", firstName, lastName);
    }
}
```

> **warning**
>
> LazyBsonDocument and LazyBsonArray implement IDisposable. It is very
> important that these classes be disposed of to ensure high performance
> and lower memory utilization.

RawBsonDocument and RawBsonArray
--------------------------------

The raw classes are special in that they defer the deserialiation of
BSON until it is needed. This is useful for when you only need a field
or two out of a document and will not be accessing them repeatedly. The
Raw documents are best utilized when shuffling data from one database or
collection to another one in MongoDB or to another, external storage
location.

> **note**
>
> The behavior of the RawXXX classes differ from that of the LazyXXX
> classes in that repeated access to the same field in the LazyXXX
> version will not incur repeated deserialization overhead whereas the
> RawXXX version will deserialize the same field without caching on
> repeated requests.

It can be used as follows:

``` {.sourceCode .csharp}
var db = server.GetDatabase("foo");
var col = db.GetCollection<RawBsonDocument>("bar");

foreach (var doc in col.Find(Query.GT("age", 15)))
{
    using (doc)
    {
        // this access incurs the cost of deserializing
        // the FirstName element.
        var firstName = doc["FirstName"];

        // even though this was deserialized just above,
        // it will need to get re-deserialized because
        // we do not store the results of deserialization.
        var firstNameAgain = doc["FirstName"];

        Console.WriteLine("{0} {1}", doc["FirstName"], doc[])
    }
}
```

> **note**
>
> The Raw classes are readonly and will throw exceptions if attempts are
> made to update them.

> **warning**
>
> RawBsonDocument and RawBsonArray implement IDisposable. It is very
> important that these classes be disposed of to ensure high performance
> and lower memory utilization.

The C\# Driver
--------------

Up until now we have been focusing on the BSON Library. The remainder of
this tutorial focuses on the C\# Driver.

Thread Safety
-------------

Only a few of the C\# Driver classes are thread safe. Among them:
`MongoClient`, `MongoServer`, `MongoDatabase`, `MongoCollection` and
`MongoGridFS`. Common classes you will use a lot that are not thread
safe include `MongoCursor` and all the classes from the BSON Library
(except `BsonSymbolTable` which is thread safe). A class is not thread
safe unless specifically documented as being thread safe.

All static properties and methods of all classes are thread safe.

`MongoClient` Class
-------------------

This class serves as the root object for working with a MongoDB server.
The connections to the server are handled automatically behind the
scenes (a connection pool is used to increase efficiency).

When you are connecting to a replica set you will still use only one
instance of `MongoClient`, which represents the replica set as a whole.
The driver automatically finds all the members of the replica set and
identifies the current primary.

Instances of this class are thread safe.

By default and unless set otherwise, all operations requiring a
`WriteConcern` use `w=1`. In other words, by default, all write
operations will block until the server has acknowledged the write.

### Connection Strings

The easiest way to connect to a MongoDB server is to use a connection
string. The standard connection string format is:

``` {.sourceCode .none}
mongodb://[username:password@]hostname[:port][/[database][?options]]
```

The `username` and `password` should only be present if you are using
authentication on the MongoDB server. The `database` segment indicates
which database the `username` is designated. When not specified, the
default `database` is *admin*.

> **note**
>
> If you are using the `database` segment as the initial database to
> use, but the username and password specified are defined in a
> different database, you can use the `authSource` option to specify the
> database in which the credential is defined. For example,
> mongodb://user:pass@hostname/db1?authSource=userDb would authenticate
> the credential against the userDb database instead of db1.

> **warning**
>
> Versions prior to 1.8 of the driver allowed the specification of an
> `admin` credential by appending `(admin)` to the username. This is not
> recognized as of version 1.8. Consult the official [connection string
> documentation](http://docs.mongodb.org/manual/reference/connection-string/)
> for more information.

The port number is optional and defaults to `27017`.

To connect to multiple servers, specify the seed list by providing
multiple hostnames (and port numbers if required) separated by commas.
For example:

``` {.sourceCode .none}
mongodb://server1,server2:27017,server2:27018
```

This connection string specifies a seed list consisting of three servers
(two of which are on the same machine but on different port numbers).
Because specifying multiple servers is ambiguous as to whether or not it
is a replica set or multiple mongos (in a sharded setup), the driver
will go through a discovery phase of connecting to the servers to
determine their type. This has a little overhead at connection time and
can be avoided by specifying a connection mode in the connection string:

> **warning**
>
> It is required that each MongoDB server have a name that is DNS
> resolvable by the client machine. Each MongoDB server reports its
> hostname back through the isMaster command and the driver uses this
> name to talk with the server. This issue can occur when the seed list
> contains an IP address and the MongoDB server reports back a hostname
> that the client machine is unable to resolve.

``` {.sourceCode .none}
mongodb://server1,server2:27017,server2:27018/?connect=replicaset
```

The available connection modes are automatic (the default), direct,
replica set, and shardrouter. The rules for connection mode are as
follows:

1.  If a connect mode is specified other than automatic, it is used.
2.  If the option replicaset is specified on the connection string using
    literal `?connect=replicaset` then replicaset mode is used. Don’t
    use replica set name.
3.  If there is only one server listed on the connection string, then
    direct mode is used.
4.  Otherwise, discovery occurs and the first server to respond
    determines the connection mode.

> **note**
>
> If you have multiple servers listed, and one is part of a replica set
> and another is not, then the connection mode is non-deterministic. Be
> sure that you are not mixing server types on the connection string.

Should the connection mode resolve to a replica set, the driver will
find the primary server even if it is not in the seed list, as long as
at least one of the servers in the seed list responds (the response will
contain the full replica set and the name of the current primary). In
addition, other secondaries will also be discovered and added (or
removed) into the mix automatically, even after initial connection. This
will enable you to add and remove servers from the replica set and the
driver will handle the changes automatically.

As alluded to above, the options part of the connection string is used
to set various connection options. Suppose you wanted to connect
directly to a member of a replica set regardless of whether it was the
current primary or not (perhaps to monitor its status or to issue read
only queries against it). You could use:

``` {.sourceCode .none}
mongodb://server2/?connect=direct;readpreference=nearest
```

The full documentation for connection strings can be found at
[Connection
String](http://docs.mongodb.org/manual/reference/connection-string/) and
read preferences at
http://docs.mongodb.org/manual/applications/replication/#replica-set-read-preference.

### SSL Support

Support for SSL is baked into the driver. You can configure this via the
connection string be adding an `ssl=true` option to the options.

``` {.sourceCode .none}
mongodb://server2/?ssl=true
```

By default, the server certificate will get validated against the local
trusted certificate store. This sometimes causes issues in test
environments where test servers don't have signed certs. To alleviate
this issue, you can also add an `sslverifycertificate=false` as another
connection string option to ignore any certificate errors.

In addition to the above connection string settings, there are a number
of settings that can be utilized through code. The `SslSettings` class
is the container of these settings.

-   CheckCertificateRevocation: defaults to false. \*
    ClientCertificates: see
    [MSDN](http://msdn.microsoft.com/en-us/library/hh159283.aspx).
-   ClientCertificateSelectionCallback: see [MSDN
    LocalCertificateSelectionCallback](http://msdn.microsoft.com/en-us/library/system.net.security.localcertificateselectioncallback.aspx).
-   EnabledSslProtocols: see
    [MSDN](http://msdn.microsoft.com/en-us/library/hh159283.aspx).
-   ServerCertificateValidationCallback: see [MSDN
    RemoteCertificateValidationCallback](http://msdn.microsoft.com/en-us/library/system.net.security.remotecertificatevalidationcallback.aspx)

To set these values, create a new instance of `SslSettings`, assign it
values, and then add it to an instance of `MongoClientSettings`.

``` {.sourceCode .csharp}
var sslSettings = new SslSettings
{
    CheckCertificateRevocation = true
};
var clientSettings = new MongoClientSettings
{
    SslSettings = sslSettings
};
```

### Authentication

The .NET driver supports both MongoDB's authentication simple protocol
as well as the more complex and robust Kerberos protocol. This enables
the driver to function with single-sign-on functionality against a
Kerberose Key Distribution Center. One such KDC is Active Directory. See
below for more details:

/tutorial/authenticate-with-csharp-driver

> **warning**
>
> Version 2.4 of the server supports [Delegated
> Authentication](http://docs.mongodb.org/manual/reference/privilege-documents/#delegated-credentials-for-mongodb-authentication).
> Due to this change, the 1.8 release of the driver has some backwards
> breaking changes in order to support this feature. Most importantly,
> all the credentials supplied in a MongoClientSettings instance will be
> authenticated when a new connection is opened, regardless of the
> database you are currently using.

### `GetServer` Method

You can navigate from an instance of a `MongoClient` to an instance of
`MongoServer` by using the `GetServer` method.

`MongoServer` Class
-------------------

The `MongoServer` class is used to provide more control over the driver.
It contains advanced ways of getting a database and pushing a sequence
of operations through a single socket in order to guarantee consistency.

### `GetDatabase` Method

You can navigate from an instance of `MongoServer` to an instance of
`MongoDatabase` (see next section) using one of the following
`GetDatabase` methods or indexers:

-   `MongoDatabase GetDatabase(MongoDatabaseSettings settings)`
-   `MongoDatabase GetDatabase(string databaseName)`
-   `MongoDatabase GetDatabase(string databaseName, MongoCredentials credentials)`
-   `MongoDatabase GetDatabase(string databaseName, MongoCredentials credentials, WriteConcern writeConcern)`
-   `MongoDatabase GetDatabase(string databaseName, WriteConcern writeConcern)`

Sample code:

``` {.sourceCode .csharp}
MongoClient client = new MongoClient(); // connect to localhost
MongoServer server = client.GetServer();
MongoDatabase test = server.GetDatabase("test");
MongoCredentials credentials = new MongoCredentials("username", "password");
MongoDatabase salaries = server.GetDatabase("salaries", credentials);
```

Most of the database settings are inherited from the server object, and
the provided overloads of `GetDatabase` let you override a few of the
most commonly used settings. To override other settings, call
`CreateDatabaseSettings` and change any settings you want before calling
`GetDatabase`, like this:

``` {.sourceCode .csharp}
var databaseSettings = server.CreateDatabaseSettings("test");
databaseSettings.SlaveOk = true;
var database = server.GetDatabase(databaseSettings);
```

`GetDatabase` maintains a table of MongoDatabase instances it has
returned before, so if you call `GetDatabase` again with the same
parameters you get the same instance back again.

### `RequestStart`/`RequestDone` Methods

Sometimes a series of operations needs to be performed on the same
connection in order to guarantee correct results. This is rarely the
case, and most of the time there is no need to call
`RequestStart`/`RequestDone`. An example of when this might be necessary
is when a series of `Inserts` are called in rapid succession with a
`WriteConcern` of `w=0`, and you want to query that data is in a
consistent manner immediately thereafter (with a `WriteConcern` of
`w=0`, the writes can queue up at the server and might not be
immediately visible to other connections). Using `RequestStart` you can
force a query to be on the same connection as the writes, so the query
won't execute until the server has caught up with the writes.

A thread can temporarily reserve a connection from the connection pool
by using `RequestStart` and `RequestDone`. For example:

``` {.sourceCode .csharp}
using(server.RequestStart(database)) {
// a series of operations that must be performed on the same connection
}
```

The database parameter simply indicates some database which you intend
to use during this request. This allows the server to pick a connection
that is already authenticated for that database (if you are not using
authentication then this optimization won't matter to you). You are free
to use any other databases as well during the request.

`RequestStart` increments a counter (for this thread) which is
decremented upon completion. The connection that was reserved is not
actually returned to the connection pool until the count reaches zero
again. This means that calls to `RequestStart` can be nested and the
right thing will happen.

> **note**
>
> `RequestStart` returns an `IDisposable`. If you do not use
> `RequestStart` with a `using` block, it is imperative that
> `RequestDone` be called in order to release the connection.

### Other Properties and Methods

For a reference of other properties and method, see the api
documentation.

MongoDatabase Class
-------------------

This class represents a database on a MongoDB server. Normally there
will be only one instance of this class per database, unless you are
using different settings to access the same database, in which case
there will be one instance for each set of settings.

Instances of this class are thread safe.

### `GetCollection` Method

This method returns an object representing a collection in a database.
When we request a collection object, we also specify the default
document type for the collection. For example:

``` {.sourceCode .csharp}
MongoDatabase hr = server.GetDatabase("hr");
MongoCollection<Employee> employees =
    hr.GetCollection<Employee>("employees");
```

A collection is not restricted to containing only one kind of document.
The default document type simply makes it more convenient to work with
that kind of document, but you can always specify a different kind of
document when required.

Most of the collection settings are inherited from the database object,
and the provided overloads of `GetCollection` let you override a few of
the most commonly used settings. To override other settings, call
`CreateCollectionSettings` and change any settings you want before
calling `GetCollection` , like this:

``` {.sourceCode .csharp}
var collectionSettings = database.CreateCollectionSettings<TDocument>("test");
collectionSettings.SlaveOk = true;
var collection = database.GetCollection(collectionSettings);
```

`GetCollection` maintains a table of instances it has returned before,
so if you call `GetCollection` again with the same parameters you get
the same instance back again.

### Other Properties and Methods

For a reference of other properties and method, see the api
documentation.

`MongoCollection<TDefaultDocument>` Class
-----------------------------------------

This class represents a collection in a MongoDB database. The
`<TDefaultDocument>` type parameter specifies the type of the default
document for this collection.

Instances of this class are thread safe.

### `Insert<TDocument>` Method

To insert a document in the collection create an object representing the
document and call `Insert`. The object can be an instance of
`BsonDocument` or of any class that can be successfully serialized as a
BSON document. For example:

``` {.sourceCode .csharp}
MongoCollection<BsonDocument> books =
    database.GetCollection<BsonDocument>("books");
BsonDocument book = new BsonDocument {
    { "author", "Ernest Hemingway" },
    { "title", "For Whom the Bell Tolls" }
};
books.Insert(book);
```

If you have a class called `Book` the code might look like:

``` {.sourceCode .csharp}
MongoCollection<Book> books = database.GetCollection<Book>("books");
Book book = new Book {
    Author = "Ernest Hemingway",
    Title = "For Whom the Bell Tolls"
};
books.Insert(book);
```

### `InsertBatch` Method

You can insert more than one document at a time using the `InsertBatch`
method. For example:

``` {.sourceCode .csharp}
MongoCollection<BsonDocument> books;
BsonDocument[] batch = {
    new BsonDocument {
        { "author", "Kurt Vonnegut" },
        { "title", "Cat's Cradle" }
    },
    new BsonDocument {
        { "author", "Kurt Vonnegut" },
        { "title", "Slaughterhouse-Five" }
    }
};
books.InsertBatch(batch);
```

When you are inserting multiple documents `InsertBatch` can be much more
efficient than `Insert`.

### `FindOne` and `FindOneAs` Methods

To retrieve documents from a collection use one of the various `Find`
methods. `FindOne` is the simplest. It returns the first document it
finds (when there are many documents in a collection you can't be sure
which one it will be). For example:

``` {.sourceCode .csharp}
MongoCollection<Book> books;
Book book = books.FindOne();
```

If you want to read a document that is not of the `<TDefaultDocument>`
type use the `FindOneAs` method, which allows you to override the type
of the returned document. For example:

``` {.sourceCode .csharp}
MongoCollection<Book> books;
BsonDocument document = books.FindOneAs<BsonDocument>();
```

In this case the default document type of the collection is `Book`, but
we are overriding that and specifying that the result be returned as an
instance of `BsonDocument`.

### `Find` and `FindAs` Methods

The `Find` and `FindAs` methods take a query that tells the server which
documents to return. The query parameter is of type `IMongoQuery`.
`IMongoQuery` is a marker interface that identifies classes that can be
used as queries. The most common ways to construct a query are to either
use the `Query` builder class or to create a `QueryDocument` yourself (a
`QueryDocument` is a subclass of `BsonDocument` that also implements
`IMongoQuery` and can therefore be used as a query object).

One way to query is to create a `QueryDocument` object yourself:

``` {.sourceCode .csharp}
MongoCollection<BsonDocument> books;
var query = new QueryDocument("author", "Kurt Vonnegut");
foreach (BsonDocument book in books.Find(query)) {
    // do something with book
}
```

Another way to query is to use the Query Builder (recommended):

``` {.sourceCode .csharp}
MongoCollection<BsonDocument> books;
var query = Query.EQ("author", "Kurt Vonnegut");
foreach (BsonDocument book in books.Find(query)) {
    // do something with book
}
```

If you want to read a document of a type that is not the default
document type use the `FindAs` method instead:

``` {.sourceCode .csharp}
MongoCollection<BsonDocument> books;
var query = Query<Book>.EQ(b => b.Author, "Kurt Vonnegut");
foreach (Book book in books.FindAs<Book>(query)) {
    // do something with book
}
```

### `Save<TDocument>` Method

The `Save` method is a combination of `Insert` and `Update`. If the `Id`
member of the document has a value, then it is assumed to be an existing
document and `Save` calls `Update` on the document (setting the `Upsert`
flag just in case it actually is a new document after all). Otherwise it
is assumed to be a new document and `Save` calls `Insert` after first
assigning a newly generated unique value to the `Id` member.

For example, you could correct an error in the title of a book using:

``` {.sourceCode .csharp}
MongoCollection<BsonDocument> books;
var query = Query.And(
    Query.EQ("author", "Kurt Vonnegut"),
    Query.EQ("title", "Cats Craddle")
);
BsonDocument book = books.FindOne(query);
if (book != null) {
    book["title"] = "Cat's Cradle";
    books.Save(book);
}
```

The `TDocument` class must have an `Id` member to be used with the
`Save` method. If it does not you can call `Insert` instead of `Save` to
insert the document.

### `Update` Method

The `Update` method is used to update existing documents. The code
sample shown for the `Save` method could also have been written as:

``` {.sourceCode .csharp}
MongoCollection<BsonDocument> books;
var query = new QueryDocument {
    { "author", "Kurt Vonnegut" },
    { "title", "Cats Craddle" }
};
var update = new UpdateDocument {
    { "$set", new BsonDocument("title", "Cat's Cradle") }
};
BsonDocument updatedBook = books.Update(query, update);
```

or using `Query` and `Update` builders:

``` {.sourceCode .csharp}
MongoCollection<BsonDocument> books;
var query = Query.And(
    Query.EQ("author", "Kurt Vonnegut"),
    Query.EQ("title", "Cats Craddle")
);
var update = Update.Set("title", "Cat's Cradle");
BsonDocument updatedBook = books.Update(query, update);
```

### `FindAndModify` Method

Use `FindAndModify` when you want to find a matching document and update
it in one atomic operation. `FindAndModify` always updates a single
document, and you can combine a query that matches multiple documents
with a sort criteria that will determine exactly which matching document
is updated. In addition, `FindAndModify` will return the matching
document (either as it was before the update or after) and if you wish
you can specify which fields of the matching document to return.

Using the example documented here, [findAndModify
Command](http://docs.mongodb.org/manual/reference/command/findAndModify/),
the call to `FindAndModify` would be written in C\# as:

``` {.sourceCode .csharp}
var jobs = database.GetCollection("jobs");
var query = Query.And(
    Query.EQ("inprogress", false),
    Query.EQ("name", "Biz report")
);
var sortBy = SortBy.Descending("priority");
var update = Update.
    .Set("inprogress", true)
    .Set("started", DateTime.UtcNow);
var result = jobs.FindAndModify(
    query,
    sortBy,
    update,
    true // return new document
);
var chosenJob = result.ModifiedDocument;
```

### `MapReduce` Method

Map/Reduce is a way of aggregating data from a collection. Every
document in a collection (or some subset if an optional query is
provided) is sent to the `map` function, which calls `emit` to produce
intermediate values. The intermediate values are then sent to the
`reduce` function to be aggregated.

This example is taken from page 87 of **MongoDB: The Definitive Guide**,
by Kristina Chodorow and Michael Dirolf. It counts how many times each
key is found in a collection.

``` {.sourceCode .csharp}
var map =
    "function() {" +
    "    for (var key in this) {" +
    "        emit(key, { count : 1 });" +
    "    }" +
    "}";

var reduce =
    "function(key, emits) {" +
    "    total = 0;" +
    "    for (var i in emits) {" +
    "        total += emits[i].count;" +
    "    }" +
    "    return { count : total };" +
    "}";

var mr = collection.MapReduce(map, reduce);
foreach (var document in mr.GetResults()) {
    Console.WriteLine(document.ToJson());
}
```

### Other Properties and Methods

For a reference of other properties and method, see the api
documentation.

`MongoCursor<TDocument>` Class
------------------------------

The `Find` method (and its variations) don't immediately return the
actual results of a query. Instead they return a cursor that can be
enumerated to retrieve the results of the query. The query isn't
actually sent to the server until we attempt to retrieve the first
result (technically, when `MoveNext` is called for the first time on the
enumerator returned by `GetEnumerator`). This means that we can control
the results of the query in interesting ways by modifying the cursor
before fetching the results.

Instances of `MongoCursor` are not thread safe, at least not until they
are frozen (see below). Once they are frozen they are thread safe
because they are read-only (in particular, `GetEnumerator` is thread
safe so the same cursor *could* be used by multiple threads).

### Enumerating a cursor

The most convenient way to consume the results of a query is to use the
C\# foreach statement. For example:

``` {.sourceCode .csharp}
var query = Query.EQ("author", "Ernest Hemingway");
var cursor = books.Find(query);
foreach (var book in cursor) {
    // do something with book
}
```

You can also use any of the extensions methods defined by LINQ for
`IEnumerable<T>` to enumerate a cursor:

``` {.sourceCode .csharp}
var query = Query.EQ("author", "Ernest Hemingway");
var cursor = books.Find(query);
var firstBook = cursor.FirstOrDefault();
var lastBook = cursor.LastOrDefault();
```

> **note**
>
> In the above example, the query is actually sent to the server twice
> (once when `FirstOrDefault` is called and again when `LastOrDefault`
> is called).

It is important that a cursor cleanly release any resources it holds.
The key to guaranteeing this is to make sure the `Dispose` method of the
enumerator is called. The `foreach` statement and the LINQ extension
methods all guarantee that `Dispose` will be called. Only if you
enumerate the cursor manually are you responsible for calling `Dispose`.

### Modifying a Cursor Before Enumerating It

A cursor has several properties that can be modified before it is
enumerated to control the results returned. There are two ways to modify
a cursor:

1.  Modify the properties directly.
2.  Use the fluent interface to set the properties.

For example, if we want to skip the first 100 results and limit the
results to the next 10, we could write:

``` {.sourceCode .csharp}
var query = Query.EQ("status", "pending");
var cursor = tasks.Find(query);
cursor.Skip = 100;
cursor.Limit = 10;
foreach (var task in cursor) {
    // do something with task
}
```

or using the fluent interface:

``` {.sourceCode .csharp}
var query = Query.EQ("status", "pending");
foreach (var task in tasks.Find(query).SetSkip(100).SetLimit(10)) {
    // do something with task
}
```

The fluent interface works well when you are setting only a few values.
When setting more than a few you might prefer to use the properties
approach.

Once you begin enumerating a cursor it becomes "frozen" and you can no
longer change any of its properties. So you must set all the properties
before you start enumerating it.

### Modifiable Properties of a Cursor

The following properties of a cursor are modifiable:

-   `BatchSize` (`SetBatchSize`)
-   `Fields` (`SetFields`)
-   `Flags` (`SetFlags`)
-   `Limit` (`SetLimit`)
-   `Options` (`SetOption` and `SetOptions`)
-   `SerializationOptions` (`SetSerializationOptions`)
-   `Skip` (`SetSkip`)
-   `SlaveOk` (`SetSlaveOk`)

The names in parenthesis are the corresponding fluent interface method
names.

The fluent interface also supports additional options that aren't used
very frequently and are not exposed as properties:

-   `SetHint`
-   `SetMax`
-   `SetMaxScan`
-   `SetMaxTime`
-   `SetMin`
-   `SetShowDiskLoc`
-   `SetSnapshot`
-   `SetSortOrder`

### Other Methods

`MongoCursor` has a few methods used for some special purpose
operations:

-   `Clone`
-   `Count`
-   `Explain`
-   `Size`

`WriteConcern` Class
--------------------

There are various levels of `WriteConcern`, and this class is used to
represent those levels. `WriteConcern` applies only to operations that
don't already return a value (so it doesn't apply to queries or
commands). It applies to the following `MongoCollection` methods:
`Insert`, `Remove`, `Save` and `Update`.

The gist of `WriteConcern` is that after an `Insert`, `Remove`, `Save`
or `Update` message is sent to the server it is followed by a
`GetLastError` command so the driver can verify that the operation
succeeded. In addition, when using replica sets it is possible to verify
that the information has been replicated to some minimum number of
secondary servers.

`MaxTime`
---------

MongoDB 2.6 introduced the ability to timeout individual queries:

``` {.sourceCode .csharp}
coll.FindAll().SetMaxTime(TimeSpan.FromSeconds(1));
```

In the example above the maxTimeMS is set to one second and the query
will be aborted after the full second is up.

### Bulk operations

Under the covers MongoDB is moving away from the combination of a write
operation followed by get last error (GLE) and towards a write commands
API. These new commands allow for the execution of bulk
insert/update/remove operations. There are two types of bulk operations:

1.  

    Ordered bulk operations.

    :   Executes all the operations in order and error out on the first
        write error.

2.  

    Unordered bulk operations.

    :   Executes all the operations in parallel and aggregates all the
        errors. Unordered bulk operations do not guarantee order of
        execution.

Let's look at two simple examples using ordered and unordered
operations:

``` {.sourceCode .csharp}
// 1. Ordered bulk operation
var bulk = coll.InitializeOrderedBulkOperation();
bulk.Insert(new BsonDocument("_id", 1));
bulk.Insert(new BsonDocument("_id", 2));
bulk.Insert(new BsonDocument("_id", 3));

bulk.Find(Query.EQ("_id", 1)).UpdateOne(Update.Set("x", 2)));
bulk.Find(Query.EQ("_id", 2)).RemoveOne();
bulk.Find(Query.EQ("_id", 3)).ReplaceOne(Update.Replace(new BsonDocument("_id", 3).Add("x", 4)));

BulkWriteResult result = bulk.Execute();

// 2. Unordered bulk operation - no guarantee of order of operation
bulk = coll.InitializeUnorderedBulkOperation();
bulk.Find(Query.EQ("_id", 1)).RemoveOne();
bulk.Find(Query.EQ("_id", 2)).RemoveOne();

result = bulk.Execute();
```

> **note**
>
> For servers older than 2.6 the API will down-convert the operations.
> To support the correct semantics for BulkWriteResult and
> BulkWriteException, the operations have to be done one at a time. It's
> not possible to down convert 100% so there might be slight edge cases
> where it cannot correctly report the right numbers.

### ParallelScan

MongoDB 2.6 added the `parallelCollectionScan` command that allows
reading an entire collection using multiple cursors.

``` {.sourceCode .csharp}
var args = new ParallelScanArgs<BsonDocument>
{
   NumberOfCursors = 3,
   BatchSize = 300
};

var cursors = coll.ParallelScan(args);
for (var cursor in cursors) 
{
    using(cursor) // need to close the cursor when we are done
    {
      while (cursor.MoveNext()) 
      {
          Console.WriteLine(cursor.Current);
      }
    }
 }
```

> **note**
>
> In practice, one thread per cursor would be used such that each cursor
> is enumerated in parallel.

> **note**
>
> ParallelScan does not work via mongos.
