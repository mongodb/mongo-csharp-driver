/* Copyright 2013-present MongoDB Inc.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core
{
    public interface IEventFormatter
    {
        object Format(object @event);
    }

    public interface IEventFormatter<TFormatted> : IEventFormatter
    {
        new TFormatted Format(object @event);
    }

    public class EventCapturer : IEventSubscriber
    {
        private Action<IEnumerable<object>, object> _addEventAction;
        private readonly Queue<object> _capturedEvents;
        private readonly IEventFormatter _eventFormatter;
        private readonly Dictionary<Type, Func<object, bool>> _eventsToCapture;
        private readonly object _lock = new object();
        private readonly IEventSubscriber _subscriber;

        public EventCapturer(IEventFormatter eventFormatter = null)
        {
            _capturedEvents = new Queue<object>();
            _subscriber = new ReflectionEventSubscriber(new CommandCapturer(this));
            _eventFormatter = eventFormatter; // can be null
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

        public EventCapturer CaptureCommandEvents(string commandName = null)
        {
            Func<string, bool> predicate = s => commandName == null || commandName == s;

            _eventsToCapture.Add(typeof(CommandStartedEvent), o => predicate(((CommandStartedEvent)o).CommandName));
            _eventsToCapture.Add(typeof(CommandSucceededEvent), o => predicate(((CommandSucceededEvent)o).CommandName));
            _eventsToCapture.Add(typeof(CommandFailedEvent), o => predicate(((CommandFailedEvent)o).CommandName));

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

        public List<object> Events
        {
            get
            {
                lock (_lock)
                {
                    return _capturedEvents.ToList();
                }
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

        public Task NotifyWhen(Func<IEnumerable<object>, bool> condition)
        {
            var notifier = new EventNotifier(this, condition);
            return notifier.Initialize();
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            Events.ForEach(e => stringBuilder.AppendLine(e.GetType().Name));
            return stringBuilder.ToString();
        }

        public void WaitForOrThrowIfTimeout(Func<IEnumerable<object>, bool> condition, TimeSpan timeout, Func<TimeSpan, string> message = null)
        {
            var notifyTask = NotifyWhen(condition);
            var index = Task.WaitAny(notifyTask, Task.Delay(timeout));
            if (index != 0)
            {
                throw new Exception(message != null ? message(timeout) : $"Waiting for the expected event exceeded the timeout {timeout}.") ;
            }
        }

        public void WaitForOrThrowIfTimeout(Type[] typesSequence, TimeSpan timeout, Func<TimeSpan, string> message = null)
        {
            var matchSequence = typesSequence
                .Select(t => new Func<object, bool>((object e) => e.GetType() == t))
                .ToArray();

            WaitForOrThrowIfTimeout(matchSequence, timeout, message);
        }

        public void WaitForEventOrThrowIfTimeout<TExpectedEvent>(TimeSpan timeout, Func<TimeSpan, string> message = null)
        {
            WaitForOrThrowIfTimeout(new[] { typeof(TExpectedEvent) }, timeout, message);
        }

        public void WaitForOrThrowIfTimeout(Func<object, bool>[] matchSequence, TimeSpan timeout, Func<TimeSpan, string> message = null)
        {
            Func<IEnumerable<object>, bool> condition = @events =>
            {
                var allEvents = @events.ToArray();
                var maxEventIndex = allEvents.Length - matchSequence.Length;

                for (int i = 0; i <= maxEventIndex; i++)
                {
                    var allMatched = true;

                    for (int j = 0; j < matchSequence.Length; j++)
                    {
                        if (!matchSequence[j](allEvents[i + j]))
                        {
                            allMatched = false;
                            break;
                        }
                    }

                    if (allMatched)
                    {
                        return true;
                    }
                }

                return false;
            };

            WaitForOrThrowIfTimeout(condition, timeout, message);
        }

        public void WaitForOrThrowIfTimeout(Func<IEnumerable<object>, bool> condition, TimeSpan timeout, string message)
        {
            WaitForOrThrowIfTimeout(condition, timeout, (t) => Ensure.IsNotNull(message, nameof(message)));
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

        // private methods
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

            var formattedEvent = _eventFormatter != null ? _eventFormatter.Format(@event) : @event;
            lock (_lock)
            {
                _capturedEvents.Enqueue(formattedEvent);
                _addEventAction?.Invoke(_capturedEvents, @event);
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
                    RecursivelyMaterialize(@event.Command),
                    @event.DatabaseNamespace,
                    @event.OperationId,
                    @event.RequestId,
                    @event.ConnectionId,
                    @event.ServiceId);
                _parent.Capture(@event);
            }

            public void Handle(CommandSucceededEvent @event)
            {
                @event = new CommandSucceededEvent(
                    @event.CommandName,
                    RecursivelyMaterialize(@event.Reply),
                    @event.DatabaseNamespace,
                    @event.OperationId,
                    @event.RequestId,
                    @event.ConnectionId,
                    @event.ServiceId,
                    @event.Duration);
                _parent.Capture(@event);
            }

            public void Handle(CommandFailedEvent @event)
            {
                var exception = @event.Failure;

                var mongoCommandException = exception as MongoCommandException;
                if (mongoCommandException != null)
                {
                    exception = new MongoCommandException(
                        mongoCommandException.ConnectionId,
                        mongoCommandException.Message,
                        RecursivelyMaterialize(mongoCommandException.Command),
                        RecursivelyMaterialize(mongoCommandException.Result));
                }

                @event = new CommandFailedEvent(
                    @event.CommandName,
                    @event.DatabaseNamespace,
                    exception,
                    @event.OperationId,
                    @event.RequestId,
                    @event.ConnectionId,
                    @event.ServiceId,
                    @event.Duration);
                _parent.Capture(@event);
            }

            private BsonDocument RecursivelyMaterialize(BsonDocument document)
            {
                if (document == null)
                {
                    return null;
                }

                var rawDocument = document as RawBsonDocument;
                if (rawDocument != null)
                {
                    return rawDocument.Materialize(new BsonBinaryReaderSettings());
                }

                for (var i = 0; i < document.ElementCount; i++)
                {
                    document[i] = RecursivelyMaterialize(document[i]);
                }

                return document;
            }

            private BsonArray RecursivelyMaterialize(BsonArray array)
            {
                var rawArray = array as RawBsonArray;
                if (rawArray != null)
                {
                    return rawArray.Materialize(new BsonBinaryReaderSettings());
                }

                for (var i = 0; i < array.Count; i++)
                {
                    array[i] = RecursivelyMaterialize(array[i]);
                }

                return array;
            }

            private BsonValue RecursivelyMaterialize(BsonValue value)
            {
                switch (value.BsonType)
                {
                    case BsonType.Array: return RecursivelyMaterialize(value.AsBsonArray);
                    case BsonType.Document: return RecursivelyMaterialize(value.AsBsonDocument);
                    case BsonType.JavaScriptWithScope: return RecursivelyMaterialize(value.AsBsonJavaScriptWithScope);
                    default: return value;
                }
            }

            private BsonJavaScriptWithScope RecursivelyMaterialize(BsonJavaScriptWithScope value)
            {
                return new BsonJavaScriptWithScope(value.Code, RecursivelyMaterialize(value.Scope));
            }
        }

        private class EventNotifier
        {
            private readonly Func<IEnumerable<object>, bool> _condition;
            private readonly EventCapturer _eventCapturer;
            private readonly TaskCompletionSource<bool> _taskCompletionSource;

            public EventNotifier(EventCapturer eventCapturer, Func<IEnumerable<object>, bool> condition)
            {
                _eventCapturer = Ensure.IsNotNull(eventCapturer, nameof(eventCapturer));
                _condition = Ensure.IsNotNull(condition, nameof(condition));
                _taskCompletionSource = new TaskCompletionSource<bool>();
            }

            // public methods
            public Task Initialize()
            {
                _eventCapturer._addEventAction += (events, @event) => TriggerNotificationIfCondition(events);

                // condition might already be true even before any more events are added
                lock (_eventCapturer._lock)
                {
                    TriggerNotificationIfCondition(_eventCapturer.Events);
                }

                return _taskCompletionSource.Task;
            }

            // private methods
            private void TriggerNotificationIfCondition(IEnumerable<object> events)
            {
                if (_condition(events))
                {
                    _taskCompletionSource.TrySetResult(true);
                }
            }
        }
    }
}
