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
using System.Collections.Generic;
using FluentAssertions;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Events
{
    public class EventPublisherTests
    {
        [Theory]
        [MemberData(nameof(EventsData))]
        internal void Publish_should_publish_event_for_registered_handlers<TEvent>(TEvent @event) where TEvent : struct, IEvent
        {
            var eventCapturer = new EventCapturer();
            eventCapturer.Capture<TEvent>();
            var eventPublisher = new EventPublisher(eventCapturer);

            eventPublisher.Publish(@event);

            eventCapturer.Events.Should().HaveCount(1);
            eventCapturer.Events[0].Should().Be(@event);
        }

        [Theory]
        [MemberData(nameof(EventsData))]
        internal void Publish_should_not_publish_event_for_not_registered_handlers<TEvent>(TEvent @event) where TEvent : struct, IEvent
        {
            var eventCapturer = new EventCapturer();
            eventCapturer.Capture<CommandStartedEvent>();
            var eventPublisher = new EventPublisher(eventCapturer);

            eventPublisher.Publish(@event);

            eventCapturer.Events.Should().HaveCount(0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Publish_should_not_try_get_handler_twice([Values(false, true)] bool isHandlerRegistered)
        {
            var eventsSubscriber = new Mock<IEventSubscriber>();

            bool handlerWasCalled = false;
            Action<ClusterAddedServerEvent> eventHandler = e => { handlerWasCalled = true; };
            if (isHandlerRegistered)
            {
                eventsSubscriber
                    .Setup(s => s.TryGetEventHandler(out eventHandler))
                    .Returns(true);
            }

            var @event = new ClusterAddedServerEvent();
            var eventPublisher = new EventPublisher(eventsSubscriber.Object);

            eventPublisher.Publish(@event);
            eventPublisher.Publish(@event);

            var anyDelegate = It.IsAny<Action<object>>();
            eventsSubscriber.Verify(s => s.TryGetEventHandler(out eventHandler), Times.Once());
            eventsSubscriber.Verify(s => s.TryGetEventHandler(out anyDelegate), Times.Once());

            handlerWasCalled.Should().Be(isHandlerRegistered);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsEventTracked_should_return_correct_value([Values(false, true)] bool isEventTracked)
        {
            var eventsSubscriber = new Mock<IEventSubscriber>();
            if (isEventTracked)
            {
                Action<ClusterAddedServerEvent> eventHandler = e => { };
                eventsSubscriber
                    .Setup(s => s.TryGetEventHandler(out eventHandler))
                    .Returns(true);
            }

            var eventPublisher = new EventPublisher(eventsSubscriber.Object);
            eventPublisher.IsEventTracked<ClusterAddedServerEvent>().Should().Be(isEventTracked);
        }

        private static IEnumerable<object[]> EventsData()
        {
            yield return new object[] { new ClusterAddedServerEvent(null, TimeSpan.FromSeconds(1)) };
            yield return new object[] { new ConnectionCreatedEvent(null, null, 1) };
        }
    }
}
