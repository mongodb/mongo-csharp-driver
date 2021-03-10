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

The NuGet packages include support for four target framework monikers (TFMs): net452, netstandard1.5, netstandard2.0, and netstandard2.1. The net452 target allows the driver to be used with the full .NET Framework version 4.5.2 and later. The netstandardX.Y TFMs allow the driver to be used with any .NET implementation supporting that TFM. This includes support for various versions of .NET Core as well as .NET 5.0 and above.

## NuGet Installation

[NuGet](http://www.nuget.org/) is the simplest way to get the driver. There are 5 packages available on nuget.

- [MongoDB.Driver](http://www.nuget.org/packages/mongodb.driver): The new driver. It is mostly free of any legacy code and should be used for all new projects. More documentation can be found in the [reference guide]({{< relref "reference\driver\index.md" >}}).
- [MongoDB.Driver.Core](http://www.nuget.org/packages/mongodb.driver.core): The core of the driver and a dependency of MongoDB.Driver. You will probably not use this package directly. More documentation can be found in the [reference guide]({{< relref "reference\driver_core\index.md" >}}).
- [MongoDB.Driver.GridFS](http://www.nuget.org/packages/mongodb.driver.gridfs): The GridFS package. More documentation can be found in the [reference guide]({{< relref "reference\gridfs\index.md" >}}).
- [MongoDB.Bson](http://www.nuget.org/packages/mongodb.bson): The BSON layer. It is a dependency of MongoDB.Driver.Core. It may be used by itself. More documentation can be found in the [reference guide]({{< relref "reference\bson\index.md" >}}).
- [mongocsharpdriver](http://www.nuget.org/packages/mongocsharpdriver): The compatibility layer for those upgrading from our 1.x series. This should not be used for new projects. More information can be found in the [1.x documentation](http://mongodb.github.io/mongo-csharp-driver/1.11);
