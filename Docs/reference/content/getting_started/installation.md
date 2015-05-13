+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Installation"
[menu.main]
  parent = "Getting Started"
  weight = 10
  identifier = "Installation"
  pre = "<i class='fa'></i>"
+++

## System Requirements

.NET 4.5 or later is required to utilize the libraries. It has also been tested with Mono 3.10 on OS X.

### Core CLR

As the Core CLR hasn't shipped yet, we don't yet have support for it. We run compatibility reports using the [.NET Portability Analyzer](https://visualstudiogallery.msdn.microsoft.com/1177943e-cfb7-4822-a8a6-e56c7905292b) to mitigate the need to make public API changes when we are ready to release compatible assemblies.

## Nuget Installation

[Nuget](http://www.nuget.org/) is the simplest way to get the driver. There are 4 packages available on nuget.

- [MongoDB.Driver](http://www.nuget.org/packages/mongodb.driver): The new driver. It is mostly free of any legacy code and should be used for all new projects. More documentation can be found in the [reference guide]({{< relref "reference\driver\index.md" >}}).
- [MongoDB.Driver.Core](http://www.nuget.org/packages/mongodb.driver.core): The core of the driver and a dependency of MongoDB.Driver. You will probably not use this package directly. More documentation can be found in the [reference guide]({{< relref "reference\driver_core\index.md" >}}).
- [MongoDB.Bson](http://www.nuget.org/packages/mongodb.bson): The BSON layer. It is a dependency of MongoDB.Driver.Core. It may be used by itself. More documentation can be found in the [reference guide]({{< relref "reference\bson\index.md" >}}).
- [mongocsharpdriver](http://www.nuget.org/packages/mongocsharpdriver): The compatibility layer for those upgrading from our 1.x series. This should not be used for new projects. More information can be found in the [1.x documentation](http://mongodb.github.io/mongo-csharp-driver/1.x);

## Binary Installation

Alternatively, if you'd like to pull down binaries, you can do that from the [releases section](https://github.com/mongodb/mongo-csharp-driver/releases) on our [github repository](https://github.com/mongodb/mongo-csharp-driver), which contains zip files for each release.

The assembly names mostly correlate strongly with the package names above. For new applications, you'll add references to `MongoDB.Driver.dll`, `MongoDB.Driver.Core.dll`, and `MongoDB.Bson.dll`. For those working with legacy applications, you'll also want to add a reference to `MongoDB.Driver.Legacy.dll`.

