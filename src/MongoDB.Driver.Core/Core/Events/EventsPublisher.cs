using System;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Events
{
    internal sealed class EventsPublisher
    {
        private static readonly Delegate __eventHandlerNull = new Action(() => { });
        private static readonly int __eventTypesCount = Enum.GetValues(typeof(EventType)).Length;

        private readonly Delegate[] _eventHandlers;
        private readonly IEventSubscriber _eventSubscriber;

        public EventsPublisher(IEventSubscriber eventSubscriber)
        {
            _eventSubscriber = Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));
            _eventHandlers = new Delegate[__eventTypesCount];
        }

        public bool IsEventTracked<TEvent>() where TEvent : struct, IEvent
        {
            _eventSubscriber.TryGetEventHandler<TEvent>(out var handler);
            return handler != null;
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : struct, IEvent
        {
            var eventType = (int)@event.Type;
            var eventHandler = _eventHandlers[eventType];

            if (eventHandler == null)
            {
                _eventSubscriber.TryGetEventHandler<TEvent>(out var registeredHandler);
                eventHandler = registeredHandler ?? __eventHandlerNull;
                _eventHandlers[(int)eventType] = eventHandler;
            }

            if (eventHandler != __eventHandlerNull)
            {
                var action = (Action<TEvent>)eventHandler;
                action(@event);
            }
        }
    }
}
