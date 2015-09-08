+++
date = "2015-09-08T15:36:56Z"
draft = false
title = "Eventing"
[menu.main]
  parent = "Driver Core"
  identifier = "Eventing"
  weight = 60
  pre = "<i class='fa'></i>"
+++

## Eventing

The .NET Driver core provides a robust model for event publication and subscription. Each event is represented by a class or struct which contains all the information related to the particular event.


### ClusterBuilder

The [`ClusterBuilder`]({{< apiref "T_MongoDB_Driver_Core_Configuration_ClusterBuilder" >}}) class contains two methods for subscribing to events.


#### IEventSubscriber

The first version of [`Subscribe`]({{< apiref "M_MongoDB_Driver_Core_Configuration_ClusterBuilder_Subscribe" >}}) takes an [`IEventSubscriber`]({{< apiref "T_MongoDB_Driver_Core_Events_IEventSubscriber" >}}). [`IEventSubscriber`]({{< apiref "T_MongoDB_Driver_Core_Events_IEventSubscriber" >}}) contains a single method to implement, [`TryGetEventHandler`]({{< apiref "M_MongoDB_Driver_Core_Events_IEventSubscriber_TryGetEventHandler_1" >}}). It takes a generic parameter indicating the type of event and sets an out parameter with the handler. 

{{% note %}}This method will be invoked once on each subscriber per event type. Therefore, performance of this method is not critical.{{% /note %}}

For instance, to handle the [`ConnectionPoolAddedConnectionEvent`]({{< apiref "T_MongoDB_Driver_Core_Events_ConnectionPoolAddedConnectionEvent" >}}), the following could be done:

```csharp
public class MyEventSubscriber : IEventSubscriber
{
    public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
    {
    	if(typeof(TEvent)) == typeof(ConnectionPoolAddedConnectionEvent))
    	{
    		handler = (Action<TEvent>)HandleConnectionPoolAddedConnectionEvent;
    		return true;
    	}

    	handler = null;
    	return false;
	}

	private void HandleConnectionPoolAddedConnectionEvent(ConnectionPoolAddedConnectionEvent @event)
	{
		Console.WriteLine("Added a connection to the pool.");
	}
}
```

This could quickly become unmaintainable with multiple events. To make this easier, we have implemented the [`ReflectionEventSubscriber`]({{< apiref "T_MongoDB_Driver_Core_Events_ReflectionEventSubscriber" >}}). It uses reflection to find all the event handler methods inside a class based on certain constructor parameters for the method name and the binding flags. For instance, we could change the above `MyEventSubscriber` class as follows:

```csharp
public class MyEventSubscriber : IEventSubscriber
{
	private readonly IEventSubscriber _subscriber;

	public MyEventSubscriber()
	{
		_subscriber = new ReflectionEventSubscriber(this);
	}

    public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
    {
    	return _subscriber.TryGetEventHandler(out handler);
	}

	private void Handle(ConnectionPoolAddedConnectionEvent @event)
	{
		Console.WriteLine("Added a connection to the pool.");
	}

	private void Handle(ConnectionPoolRemovedConnectionEvent @event)
	{
		Console.WriteLine("Removed a connection from the pool.");
	}
}
```

{{% note %}}The default method name is "Handle" and the default binding flags are for public instance methods.{{% /note %}}

The [`PerformanceCounterEventSubscriber`]({{< srcref "MongoDB.Driver.Core/Core/Events/Diagnostics/PerformanceCounterEventSubscriber.cs" >}}) is a good example of utilizing the [`ReflectionEventSubscriber`]({{< apiref "T_MongoDB_Driver_Core_Events_ReflectionEventSubscriber" >}}).


#### Method

The second version of [`Subscribe`]({{< apiref "M_MongoDB_Driver_Core_Configuration_ClusterBuilder_Subscribe" >}}) takes an [`Action<TEvent>`]({{< msdnref "018hxwa8" >}}). For example, to use a static method:

```csharp
static void Main() 
{
	var builder = new ClusterBuilder();

	builder.Subscribe<ConnectionPoolAddedConnectionEvent>(Handle);

	// ... snip
}

private static void Handle(ConnectionPoolAddedConnectionEvent @event)
{
    Console.WriteLine("Added a connection to the pool.");
}
```

Alternatively, a lambda expression could be used:

```csharp
static void Main() 
{
	var builder = new ClusterBuilder();

	builder.Subscribe<ConnectionPoolAddedConnectionEvent>(x => Console.WriteLine("Added a connection to the pool."));

	// ... snip
}
```


### Operation Ids

Any commands that could occur based on user initiation will contain an operation identifier. This identifer can be used to link together all events that occured due to the user initiated action.


### ClusterId, ServerId, and ConnectionId

All events will contain at least one of these identifiers. They can be used to uniquely attribute a particular event to a cluster, a server, or a connection. In addition, the ConnectionId also contains a local value and a server value where the server value contains the same value that will show up in the server logs for its connection logging.


### Command Events

There are three events related to monitoring data sent on the wire. These are the [`CommandStartedEvent`]({{< apiref "T_MongoDB_Driver_Core_Events_CommandStartedEvent" >}}), the [`CommandSucceededEvent`]({{< apiref "T_MongoDB_Driver_Core_Events_CommandSucceededEvent" >}}), and the [`CommandFailedEvent`]({{< apiref "T_MongoDB_Driver_Core_Events_CommandFailedEvent" >}}). For every started event, there will always be a succeeded or failed event.

In addition, any messages sent to the server that are not already commands will be upconverted for the sake of consumption. For instance, an `OP_DELETE` wire protocol message on server 2.4 will appear as though it were a [`delete`]({{< docsref "reference/command/delete/" >}}) command.

{{% note class="warning" %}}These are heavy events to generate. Do not subscribe to these unless they provide necessary information.{{% /note %}}

{{% note %}}Certain information has been removed for security reasons. For instance, the [`authenticate`]({{< docsref "reference/command/authenticate/" >}}) command will not contain the actual command or its reply. However, it will still contain the command name itself.{{% /note %}}


#### CommandStartedEvent

The [`CommandStartedEvent`]({{< apiref "T_MongoDB_Driver_Core_Events_CommandStartedEvent" >}}) contains, amoungst other information, the command name as well as the command itself. While the command also contains the command name, the command is potentially heavy to access and will not live beyond the lifetime of the event. Any information necessary from the command should be pulled out and used immediately or stored. 


#### CommandSucceededEvent

The [`CommandSucceededEvent`]({{< apiref "T_MongoDB_Driver_Core_Events_CommandSucceededEvent" >}}) contains, amoungst other information, the command name, the duration of the command, and the reply. The reply is potentially heavy to access and will not live beyond the lifetime of the event.  Any information necessary from the reply should be pulled out and used immediately or stored. 


#### CommandFailedEvent

The [`CommandFailedEvent`]({{< apiref "T_MongoDB_Driver_Core_Events_CommandFailedEvent" >}}) contains, amoungst other information, the command name, the duration of the command, and the exception. The exception is potentially heavy to access and will not live beyond the lifetime of the event.  Any information necessary from the exception should be pulled out and used immediately or stored. 