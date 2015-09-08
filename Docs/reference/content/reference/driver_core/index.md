+++
date = "2015-03-17T15:36:56Z"
draft = false
title = "Driver Core"
[menu.main]
  parent = "Reference"
  identifier = "Driver Core"
  weight = 30
  pre = "<i class='fa'></i>"
+++

## Driver Core

Driver Core is a full driver with complete support for all types of deployment configurations, authentication, SSL, and cursors. The API is verbose, but highly configurable which is why it's a great solution to build higher-level APIs upon. The [MongoDB .NET Driver]({{< relref "reference\driver\index.md" >}} is built upon Driver Core.

## Services

Driver core provides a number of services that higher-level drivers can utilize either implicitly or explicitly.

### Connection Pooling

Connection pooling is provided for every server that is discovered. There are a number of settings that govern behavior ranging from connection lifetimes to the maximum number of connections in the pool.

### Server Monitoring

Each server that is discovered is monitored. By default, this monitoring happens every 10 seconds and consists of an `{ ismaster: 1 }` call followed by a `{ buildinfo: 1 }` call. When servers go down, the frequency of these calls will be increased to 500 milliseconds. See the [Server Discovery and Monitory Specification](https://github.com/mongodb/specifications/blob/master/source/server-discovery-and-monitoring/server-discovery-and-monitoring-summary.rst) for more information.

### Server Selection

An API is provided to allow for robust and configurable server selection capabilities. These capabilities align with the [Server Selection Specification](https://github.com/mongodb/specifications/blob/master/source/server-selection/server-selection.rst), but are also extensible if additional needs are required. 

### Operations

A large number of operations have been implemented for everything from a generic command like "ismaster" to the extremely complicated bulk write ("insert", "update", and "delete") commands and presented as instantiatable classes. These classes handle version checking the server to ensure that they will function against all versions of the server in which they exist as well as ensuring that subsequent correlated operations (such as get more's for cursors) function correctly.

### Bindings

Bindings glue together server selection and operation execution by influencing how and where operations get executed. It would be possible to construct bindings that, for instance, pipeline multiple operations down the same connection or ensure that OP_GETMORE requests are sent down the same connection as the initial OP_QUERY.

### [Eventing]({{< relref "events.md" >}})

The driver provides many events related to server selection, connection pooling, cluster monitoring, command execution, etc... These events are subscribable to provide solutions such as logging and performance counters.