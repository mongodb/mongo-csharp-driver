/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Helpers
{
    internal class EventCapturer : IEventSubscriber
    {
        private readonly Queue<object> _capturedEvents;

        public EventCapturer()
        {
            _capturedEvents = new Queue<object>();
        }

        public void Clear()
        {
            _capturedEvents.Clear();
        }

        public object Next()
        {
            if (_capturedEvents.Count == 0)
            {
                throw new Exception("No captured events exist.");
            }

            return _capturedEvents.Dequeue();
        }

        public bool Any()
        {
            return _capturedEvents.Count > 0;
        }

        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
        {
            handler = e => Capture(e);
            return true;
        }

        private void Capture(object @event)
        {
            _capturedEvents.Enqueue(@event);
        }
    }
}
