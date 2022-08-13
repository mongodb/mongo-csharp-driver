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

        public bool IsEventTracked<T>()
        {
            _eventSubscriber.TryGetEventHandler<T>(out var handler);
            return handler != null;
        }

        public void Publish<T>(EventType eventType, T @event)
        {
            var eventHandler = _eventHandlers[(int)eventType];

            if (eventHandler == null)
            {
                _eventSubscriber.TryGetEventHandler<T>(out var registeredHandler);
                eventHandler = registeredHandler ?? __eventHandlerNull;
                _eventHandlers[(int)eventType] = eventHandler;
            }

            if (eventHandler != __eventHandlerNull)
            {
                var action = (Action<T>)eventHandler;
                action(@event);
            }
        }
    }
}
