+++
date = "2022-11-22T15:36:56Z"
draft = false
title = "Logging"
[menu.main]
  parent = "Driver"
  identifier = "Logging"
  weight = 10
  pre = "<i class='fa'></i>"
+++

## Logging

Starting in version 2.18, the .NET/C# driver uses the standard  [.NET logging API](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line). The [MongoDB logging specification](https://github.com/mongodb/specifications/blob/master/source/logging/logging.rst) defines the components, structure, and verbosity of the logs. On this page, you can learn how to set up and configure logging for your application.

You can configure logging using the [`LoggingSettings`]({{< apiref "T_MongoDB_Driver_Core_Configuration_LoggingSettings" >}}). ```LoggingSettings``` contains the following properties:

|Property|Description|
|--------|-----------|
|LoggerFactory|The [ILoggerFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.iloggerfactory?view=dotnet-plat-ext-6.0) object that will create an [ILogger](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger?view=dotnet-plat-ext-6.0).<br><br>**Data Type:** ILoggerFactory<br>**Default:** null  |
|MaxDocumentSize|Maximum number of characters for extended JSON documents in logged messages<br><br>For example, when the driver logs the [CommandStarted](https://github.com/mongodb/specifications/blob/master/source/command-logging-and-monitoring/command-logging-and-monitoring.rst#command-started-message) event, it truncates the Command field to the specified character limit.<br><br>**Data Type:** int<br>**Default:** 1000|


The following code example creates a MongoClient that logs debug messages to the console. To do so, the code performs the following steps:

- Creates a LoggerFactory, which specifies the logging destination and level
- Creates a LoggingSettings object, passing the LoggerFactory object as a parameter to the constructor
- Creates a MongoClient object, passing the LoggingSettings object as a parameter to the constructor


```csharp
 using var loggerFactory = LoggerFactory.Create(b =>
 {
    b.AddSimpleConsole();
    b.SetMinimumLevel(LogLevel.Debug);
 });
 
 var settings = MongoClientSettings.FromConnectionString(...);
 settings.LoggingSettings = new LoggingSettings(loggerFactory);
 var client = new MongoClient(settings);
```
.NET/C# driver log category naming:

|Property|Description|
|--------|-----------|
|MongoDB.Command|command|
|MongoDB.SDAM|topology|
|MongoDB.ServerSelection|serverSelection|
|MongoDB.Connection|connection|
|MongoDB.Internal.*|Prefix for all .NET/C# Driver internal components not described by spec|


How to configure log categories verbosity example:

```csharp
var categoriesConfiguration = new Dictionary<string, string>
{
    // Output all logs from all categories with Error and higher level
    { "LogLevel:Default", "Error" },
    // Output SDAM logs with Debug and higher level
    { "LogLevel:MongoDB.SDAM", "Debug" }
};
var config = new ConfigurationBuilder()
   .AddInMemoryCollection(categoriesConfiguration)
   .Build();
using var loggerFactory = LoggerFactory.Create(b =>
{
    b.AddConfiguration(config);
    b.AddSimpleConsole();
});

var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
settings.LoggingSettings = new LoggingSettings(loggerFactory);
var client = new MongoClient(settings);
```
