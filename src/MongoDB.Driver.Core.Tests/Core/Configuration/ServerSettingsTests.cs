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
using System.Net.Sockets;
using FluentAssertions;
using MongoDB.Driver.Core.Configuration;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Configuration
{
    [TestFixture]
    public class ServerSettingsTests
    {
        [Test]
        public void Constructor_initializes_instance()
        {
            var subject = new ServerSettings();
            subject.HeartbeatInterval.Should().Be(TimeSpan.FromSeconds(10));
            subject.HeartbeatTimeout.Should().Be(TimeSpan.FromSeconds(10));
        }

        [Test]
        public void WithHeartbeatInterval_returns_new_instance_if_value_is_not_equal()
        {
            var oldHeartbeatInterval = TimeSpan.FromSeconds(1);
            var newHeartbeatInterval = TimeSpan.FromSeconds(2);
            var subject1 = new ServerSettings().WithHeartbeatInterval(oldHeartbeatInterval);
            var subject2 = subject1.WithHeartbeatInterval(newHeartbeatInterval);
            subject2.Should().NotBeSameAs(subject1);
            subject2.HeartbeatInterval.Should().Be(newHeartbeatInterval);
        }

        [Test]
        public void WithHeartbeatInterval_returns_same_instance_if_value_is_equal()
        {
            var subject1 = new ServerSettings();
            var subject2 = subject1.WithHeartbeatInterval(subject1.HeartbeatInterval);
            subject2.Should().BeSameAs(subject1);
        }

        [Test]
        public void WithHeartbeatTimeout_returns_new_instance_if_value_is_not_equal()
        {
            var oldHeartbeatTimeout = TimeSpan.FromSeconds(1);
            var newHeartbeatTimeout = TimeSpan.FromSeconds(2);
            var subject1 = new ServerSettings().WithHeartbeatTimeout(oldHeartbeatTimeout);
            var subject2 = subject1.WithHeartbeatTimeout(newHeartbeatTimeout);
            subject2.Should().NotBeSameAs(subject1);
            subject2.HeartbeatTimeout.Should().Be(newHeartbeatTimeout);
        }

        [Test]
        public void WithHeartbeatTimeout_returns_same_instance_if_value_is_equal()
        {
            var subject1 = new ServerSettings();
            var subject2 = subject1.WithHeartbeatTimeout(subject1.HeartbeatTimeout);
            subject2.Should().BeSameAs(subject1);
        }
    }
}