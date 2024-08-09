/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Events
{
    internal sealed class EventPublisher
    {
        private static readonly Delegate __eventHandlerNull = new Action(() => { });
        private static readonly int __eventTypesCount = Enum.GetValues(typeof(EventType)).Length;

        private readonly Delegate[] _eventHandlers;
        private readonly IEventSubscriber _eventSubscriber;

        public EventPublisher(IEventSubscriber eventSubscriber)
        {
            _eventSubscriber = Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));
            _eventHandlers = new Delegate[__eventTypesCount];
        }

        public bool IsEventTracked<TEvent>() where TEvent : IEvent
        {
            _eventSubscriber.TryGetEventHandler<TEvent>(out var handler);
            return handler != null;
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
        {
            var eventType = (int)@event.Type;
            var eventHandler = _eventHandlers[eventType];

            if (eventHandler == null)
            {
                _eventSubscriber.TryGetEventHandler<TEvent>(out var registeredHandler);
                eventHandler = registeredHandler ?? __eventHandlerNull;
                _eventHandlers[eventType] = eventHandler;
            }

            if (eventHandler != __eventHandlerNull)
            {
                var action = (Action<TEvent>)eventHandler;
                action(@event);
            }
        }
    }
}
