/* Copyright 2013-2015 MongoDB Inc.
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
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core
{
    public class EventCapturer : IEventSubscriber
    {
        private readonly Queue<object> _capturedEvents;
        private readonly object _lock = new object();
        private readonly IEventSubscriber _subscriber;
        private readonly Dictionary<Type, Func<object, bool>> _eventsToCapture;

        public EventCapturer()
        {
            _capturedEvents = new Queue<object>();
            _subscriber = new ReflectionEventSubscriber(new CommandCapturer(this));
            _eventsToCapture = new Dictionary<Type, Func<object, bool>>();
        }

        public EventCapturer Capture<TEvent>(Func<TEvent, bool> predicate = null)
        {
            if (predicate == null)
            {
                predicate = o => true;
            }
            _eventsToCapture.Add(typeof(TEvent), o => predicate((TEvent)o));
            return this;
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _capturedEvents.Count;
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _capturedEvents.Clear();
            }
        }

        public object Next()
        {
            lock (_lock)
            {
                if (_capturedEvents.Count == 0)
                {
                    throw new Exception("No captured events exist.");
                }

                return _capturedEvents.Dequeue();
            }
        }

        public bool Any()
        {
            lock (_lock)
            {
                return _capturedEvents.Count > 0;
            }
        }

        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
        {
            if (_eventsToCapture.Count > 0 && !_eventsToCapture.ContainsKey(typeof(TEvent)))
            {
                handler = null;
                return false;
            }

            if (!_subscriber.TryGetEventHandler(out handler))
            {
                handler = e => Capture(e);
            }

            return true;
        }

        private void Capture<TEvent>(TEvent @event)
        {
            var obj = @event as object;
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            Func<object, bool> predicate;
            if (_eventsToCapture.TryGetValue(typeof(TEvent), out predicate) && !predicate(@event))
            {
                return;
            }

            lock (_lock)
            {
                _capturedEvents.Enqueue(@event);
            }
        }

        private class CommandCapturer
        {
            private readonly EventCapturer _parent;

            public CommandCapturer(EventCapturer parent)
            {
                _parent = parent;
            }

            public void Handle(CommandStartedEvent @event)
            {
                @event = new CommandStartedEvent(
                    @event.CommandName,
                    (BsonDocument)@event.Command.DeepClone(),
                    @event.DatabaseNamespace,
                    @event.OperationId,
                    @event.RequestId,
                    @event.ConnectionId);
                _parent.Capture(@event);
            }

            public void Handle(CommandSucceededEvent @event)
            {
                @event = new CommandSucceededEvent(
                    @event.CommandName,
                    @event.Reply == null ? null : (BsonDocument)@event.Reply.DeepClone(),
                    @event.OperationId,
                    @event.RequestId,
                    @event.ConnectionId,
                    @event.Duration);
                _parent.Capture(@event);
            }

            public void Handle(CommandFailedEvent @event)
            {
                _parent.Capture(@event);
            }
        }
    }
}
