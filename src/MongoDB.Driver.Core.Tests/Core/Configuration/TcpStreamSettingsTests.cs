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
    public class TcpStreamSettingsTests
    {
        [Test]
        public void Constructor_initializes_instance()
        {
            var subject = new TcpStreamSettings();
            subject.AddressFamily.Should().Be(AddressFamily.InterNetwork);
            subject.ReadTimeout.Should().Be(null);
            subject.ReceiveBufferSize.Should().Be(64 * 1024);
            subject.SendBufferSize.Should().Be(64 * 1024);
            subject.WriteTimeout.Should().Be(null);
        }

        [Test]
        public void WithAddressFamily_returns_new_instance_if_value_is_not_equal()
        {
            var oldSetting = AddressFamily.InterNetwork;
            var newSetting = AddressFamily.InterNetworkV6;
            var subject1 = new TcpStreamSettings().WithAddressFamily(oldSetting);
            var subject2 = subject1.WithAddressFamily(newSetting);
            subject2.Should().NotBeSameAs(subject1);
            subject1.AddressFamily.Should().Be(oldSetting);
            subject2.AddressFamily.Should().Be(newSetting);
        }

        [Test]
        public void WithAddressFamily_returns_same_instance_if_value_is_equal()
        {
            var subject1 = new TcpStreamSettings();
            var subject2 = subject1.WithAddressFamily(AddressFamily.InterNetwork);
            subject2.Should().BeSameAs(subject1);
        }

        [Test]
        public void WithReadTimeout_returns_new_instance_if_value_is_not_equal()
        {
            var oldSetting = (TimeSpan?)null;
            var newSetting = TimeSpan.FromMinutes(1);
            var subject1 = new TcpStreamSettings().WithReadTimeout(oldSetting);
            var subject2 = subject1.WithReadTimeout(newSetting);
            subject2.Should().NotBeSameAs(subject1);
            subject1.ReadTimeout.Should().Be(oldSetting);
            subject2.ReadTimeout.Should().Be(newSetting);
        }

        [Test]
        public void WithReadTimeout_returns_same_instance_if_value_is_equal()
        {
            var subject1 = new TcpStreamSettings();
            var subject2 = subject1.WithReadTimeout(null);
            subject2.Should().BeSameAs(subject1);
        }

        [Test]
        public void WithReceiveBufferSize_returns_new_instance_if_value_is_not_equal()
        {
            var oldSetting = 10;
            var newSetting = 13;
            var subject1 = new TcpStreamSettings().WithReceiveBufferSize(oldSetting);
            var subject2 = subject1.WithReceiveBufferSize(newSetting);
            subject2.Should().NotBeSameAs(subject1);
            subject1.ReceiveBufferSize.Should().Be(oldSetting);
            subject2.ReceiveBufferSize.Should().Be(newSetting);
        }

        [Test]
        public void WithReceiveBufferSize_returns_same_instance_if_value_is_equal()
        {
            var subject1 = new TcpStreamSettings();
            var subject2 = subject1.WithReceiveBufferSize(64 * 1024);
            subject2.Should().BeSameAs(subject1);
        }

        [Test]
        public void WithSendBufferSize_returns_new_instance_if_value_is_not_equal()
        {
            var oldSetting = 10;
            var newSetting = 13;
            var subject1 = new TcpStreamSettings().WithSendBufferSize(oldSetting);
            var subject2 = subject1.WithSendBufferSize(newSetting);
            subject2.Should().NotBeSameAs(subject1);
            subject1.SendBufferSize.Should().Be(oldSetting);
            subject2.SendBufferSize.Should().Be(newSetting);
        }

        [Test]
        public void WithSendBufferSize_returns_same_instance_if_value_is_equal()
        {
            var subject1 = new TcpStreamSettings();
            var subject2 = subject1.WithSendBufferSize(64 * 1024);
            subject2.Should().BeSameAs(subject1);
        }

        [Test]
        public void WithWriteTimeout_returns_new_instance_if_value_is_not_equal()
        {
            var oldSetting = (TimeSpan?)null;
            var newSetting = TimeSpan.FromMinutes(1);
            var subject1 = new TcpStreamSettings().WithWriteTimeout(oldSetting);
            var subject2 = subject1.WithWriteTimeout(newSetting);
            subject2.Should().NotBeSameAs(subject1);
            subject1.WriteTimeout.Should().Be(oldSetting);
            subject2.WriteTimeout.Should().Be(newSetting);
        }

        [Test]
        public void WithWriteTimeout_returns_same_instance_if_value_is_equal()
        {
            var subject1 = new TcpStreamSettings();
            var subject2 = subject1.WithWriteTimeout(null);
            subject2.Should().BeSameAs(subject1);
        }
    }
}