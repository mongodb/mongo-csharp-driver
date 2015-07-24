﻿/* Copyright 2013-2014 MongoDB Inc.
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
using FluentAssertions;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Configuration
{
    [TestFixture]
    public class ServerSettingsTests
    {
        private static readonly ServerSettings __defaults = new ServerSettings();

        [Test]
        public void constructor_should_initialize_instance()
        {
            var subject = new ServerSettings();

            subject.HeartbeatInterval.Should().Be(TimeSpan.FromSeconds(10));
            subject.HeartbeatTimeout.Should().Be(TimeSpan.FromSeconds(10));
        }

        [Test]
        public void constructor_should_throw_when_heartbeatInterval_is_negative()
        {
            Action action = () => new ServerSettings(heartbeatInterval: TimeSpan.FromSeconds(-1));

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("heartbeatInterval");
        }

        [Test]
        public void constructor_should_throw_when_heartbeatTimeout_is_negative()
        {
            Action action = () => new ServerSettings(heartbeatTimeout: TimeSpan.FromSeconds(-1));

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("heartbeatTimeout");
        }

        [Test]
        public void constructor_with_heartbeatInterval_should_initialize_instance()
        {
            var heartbeatInterval = TimeSpan.FromSeconds(123);

            var subject = new ServerSettings(heartbeatInterval: heartbeatInterval);

            subject.HeartbeatInterval.Should().Be(heartbeatInterval);
            subject.HeartbeatTimeout.Should().Be(__defaults.HeartbeatTimeout);
        }

        [Test]
        public void constructor_with_heartbeatTimeout_should_initialize_instance()
        {
            var heartbeatTimeout = TimeSpan.FromSeconds(123);

            var subject = new ServerSettings(heartbeatTimeout: heartbeatTimeout);

            subject.HeartbeatInterval.Should().Be(subject.HeartbeatInterval);
            subject.HeartbeatTimeout.Should().Be(heartbeatTimeout);
        }

        [Test]
        public void With_heartbeatInterval_should_return_expected_result()
        {
            var oldHeartbeatInterval = TimeSpan.FromSeconds(1);
            var newHeartbeatInterval = TimeSpan.FromSeconds(2);
            var subject = new ServerSettings(heartbeatInterval: oldHeartbeatInterval);

            var result = subject.With(heartbeatInterval: newHeartbeatInterval);

            result.HeartbeatInterval.Should().Be(newHeartbeatInterval);
            result.HeartbeatTimeout.Should().Be(subject.HeartbeatTimeout);
        }

        [Test]
        public void With_heartbeatTimeout_should_return_expected_result()
        {
            var oldHeartbeatTimeout = TimeSpan.FromSeconds(1);
            var newHeartbeatTimeout = TimeSpan.FromSeconds(2);
            var subject = new ServerSettings(heartbeatTimeout: oldHeartbeatTimeout);

            var result = subject.With(heartbeatTimeout: newHeartbeatTimeout);

            result.HeartbeatInterval.Should().Be(subject.HeartbeatInterval);
            result.HeartbeatTimeout.Should().Be(newHeartbeatTimeout);
        }
    }
}