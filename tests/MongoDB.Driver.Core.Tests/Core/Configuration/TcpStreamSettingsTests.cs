/* Copyright 2013-2016 MongoDB Inc.
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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Configuration
{
    public class TcpStreamSettingsTests
    {
        private static readonly TcpStreamSettings __defaults = new TcpStreamSettings();

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new TcpStreamSettings();

            subject.AddressFamily.Should().Be(AddressFamily.InterNetwork);
            subject.ConnectTimeout.Should().Be(Timeout.InfiniteTimeSpan);
            subject.ReadTimeout.Should().Be(null);
            subject.ReceiveBufferSize.Should().Be(64 * 1024);
            subject.SendBufferSize.Should().Be(64 * 1024);
            subject.SocketConfigurator.Should().BeNull();
            subject.WriteTimeout.Should().Be(null);
        }

        [Fact]
        public void constructor_should_throw_when_connectTimeout_is_negative()
        {
            Action action = () => new TcpStreamSettings(connectTimeout: TimeSpan.FromSeconds(-1));

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("connectTimeout");
        }

        [Fact]
        public void constructor_should_throw_when_readTimeout_is_negative()
        {
            Action action = () => new TcpStreamSettings(readTimeout: TimeSpan.FromSeconds(-1));

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("readTimeout");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_receiveBufferSize_is_negative_or_zero(
            [Values(-1, 0)]
            int receiveBufferSize)
        {
            Action action = () => new TcpStreamSettings(receiveBufferSize: receiveBufferSize);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("receiveBufferSize");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_sendBufferSize_is_negative_or_zero(
            [Values(-1, 0)]
            int sendBufferSize)
        {
            Action action = () => new TcpStreamSettings(sendBufferSize: sendBufferSize);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("sendBufferSize");
        }

        [Fact]
        public void constructor_should_throw_when_writeTimeout_is_negative()
        {
            Action action = () => new TcpStreamSettings(writeTimeout: TimeSpan.FromSeconds(-1));

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("writeTimeout");
        }

        [Fact]
        public void constructor_with_addressFamily_should_initialize_instance()
        {
            var addressFamily = AddressFamily.InterNetworkV6;

            var subject = new TcpStreamSettings(addressFamily: addressFamily);

            subject.AddressFamily.Should().Be(addressFamily);
            subject.ConnectTimeout.Should().Be(__defaults.ConnectTimeout);
            subject.ReadTimeout.Should().Be(__defaults.ReadTimeout);
            subject.ReceiveBufferSize.Should().Be(__defaults.ReceiveBufferSize);
            subject.SendBufferSize.Should().Be(__defaults.SendBufferSize);
            subject.SocketConfigurator.Should().Be(__defaults.SocketConfigurator);
            subject.WriteTimeout.Should().Be(__defaults.WriteTimeout);
        }

        [Fact]
        public void constructor_with_connectTimeout_should_initialize_instance()
        {
            var connectTimeout = TimeSpan.FromSeconds(123);

            var subject = new TcpStreamSettings(connectTimeout: connectTimeout);

            subject.AddressFamily.Should().Be(__defaults.AddressFamily);
            subject.ConnectTimeout.Should().Be(connectTimeout);
            subject.ReadTimeout.Should().Be(__defaults.ReadTimeout);
            subject.ReceiveBufferSize.Should().Be(__defaults.ReceiveBufferSize);
            subject.SendBufferSize.Should().Be(__defaults.SendBufferSize);
            subject.SocketConfigurator.Should().Be(__defaults.SocketConfigurator);
            subject.WriteTimeout.Should().Be(__defaults.WriteTimeout);
        }

        [Fact]
        public void constructor_with_readTimeout_should_initialize_instance()
        {
            var readTimeout = TimeSpan.FromSeconds(123);

            var subject = new TcpStreamSettings(readTimeout: readTimeout);

            subject.AddressFamily.Should().Be(__defaults.AddressFamily);
            subject.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            subject.ReadTimeout.Should().Be(readTimeout);
            subject.ReceiveBufferSize.Should().Be(__defaults.ReceiveBufferSize);
            subject.SendBufferSize.Should().Be(__defaults.SendBufferSize);
            subject.SocketConfigurator.Should().Be(__defaults.SocketConfigurator);
            subject.WriteTimeout.Should().Be(__defaults.WriteTimeout);
        }

        [Fact]
        public void constructor_with_receiveBufferSize_should_initialize_instance()
        {
            var receiveBufferSize = 123;

            var subject = new TcpStreamSettings(receiveBufferSize: receiveBufferSize);

            subject.AddressFamily.Should().Be(__defaults.AddressFamily);
            subject.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            subject.ReadTimeout.Should().Be(subject.ReadTimeout);
            subject.ReceiveBufferSize.Should().Be(receiveBufferSize);
            subject.SendBufferSize.Should().Be(__defaults.SendBufferSize);
            subject.SocketConfigurator.Should().Be(__defaults.SocketConfigurator);
            subject.WriteTimeout.Should().Be(__defaults.WriteTimeout);
        }

        [Fact]
        public void constructor_with_sendBufferSize_should_initialize_instance()
        {
            var sendBufferSize = 123;

            var subject = new TcpStreamSettings(sendBufferSize: sendBufferSize);

            subject.AddressFamily.Should().Be(__defaults.AddressFamily);
            subject.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            subject.ReadTimeout.Should().Be(subject.ReadTimeout);
            subject.ReceiveBufferSize.Should().Be(subject.ReceiveBufferSize);
            subject.SendBufferSize.Should().Be(sendBufferSize);
            subject.SocketConfigurator.Should().Be(__defaults.SocketConfigurator);
            subject.WriteTimeout.Should().Be(__defaults.WriteTimeout);
        }

        [Fact]
        public void constructor_with_socketConfigurator_should_initialize_instance()
        {
            Action<Socket> socketConfigurator = s => { };

            var subject = new TcpStreamSettings(socketConfigurator: socketConfigurator);

            subject.AddressFamily.Should().Be(__defaults.AddressFamily);
            subject.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            subject.ReadTimeout.Should().Be(subject.ReadTimeout);
            subject.ReceiveBufferSize.Should().Be(subject.ReceiveBufferSize);
            subject.SendBufferSize.Should().Be(subject.SendBufferSize);
            subject.SocketConfigurator.Should().Be(socketConfigurator);
            subject.WriteTimeout.Should().Be(__defaults.WriteTimeout);
        }

        [Fact]
        public void constructor_with_writeTimeout_should_initialize_instance()
        {
            var writeTimeout = TimeSpan.FromSeconds(123);

            var subject = new TcpStreamSettings(writeTimeout: writeTimeout);

            subject.AddressFamily.Should().Be(__defaults.AddressFamily);
            subject.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            subject.ReadTimeout.Should().Be(subject.ReadTimeout);
            subject.ReceiveBufferSize.Should().Be(__defaults.ReceiveBufferSize);
            subject.SendBufferSize.Should().Be(__defaults.SendBufferSize);
            subject.SocketConfigurator.Should().Be(__defaults.SocketConfigurator);
            subject.WriteTimeout.Should().Be(writeTimeout);
        }

        [Fact]
        public void With_addressFamily_should_return_expected_result()
        {
            var oldAddressFamily = AddressFamily.InterNetwork;
            var newAddressFamily = AddressFamily.InterNetworkV6;
            var subject = new TcpStreamSettings(addressFamily: oldAddressFamily);

            var result = subject.With(addressFamily: newAddressFamily);

            result.AddressFamily.Should().Be(newAddressFamily);
            result.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            result.ReadTimeout.Should().Be(subject.ReadTimeout);
            result.ReceiveBufferSize.Should().Be(subject.ReceiveBufferSize);
            result.SendBufferSize.Should().Be(subject.SendBufferSize);
            result.SocketConfigurator.Should().Be(subject.SocketConfigurator);
            result.WriteTimeout.Should().Be(subject.WriteTimeout);
        }

        [Fact]
        public void With_connectTimeout_should_return_expected_result()
        {
            var oldConnectTimeout = TimeSpan.FromSeconds(1);
            var newConnectTimeout = TimeSpan.FromSeconds(2);
            var subject = new TcpStreamSettings(connectTimeout: oldConnectTimeout);

            var result = subject.With(connectTimeout: newConnectTimeout);

            result.AddressFamily.Should().Be(subject.AddressFamily);
            result.ConnectTimeout.Should().Be(newConnectTimeout);
            result.ReadTimeout.Should().Be(subject.ReadTimeout);
            result.ReceiveBufferSize.Should().Be(subject.ReceiveBufferSize);
            result.SendBufferSize.Should().Be(subject.SendBufferSize);
            result.SocketConfigurator.Should().Be(subject.SocketConfigurator);
            result.WriteTimeout.Should().Be(subject.WriteTimeout);
        }

        [Fact]
        public void With_readTimeout_should_return_expected_result()
        {
            var oldReadTimeout = TimeSpan.FromSeconds(1);
            var newOldTimeout = TimeSpan.FromSeconds(2);
            var subject = new TcpStreamSettings(readTimeout: oldReadTimeout);

            var result = subject.With(readTimeout: newOldTimeout);

            result.AddressFamily.Should().Be(subject.AddressFamily);
            result.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            result.ReadTimeout.Should().Be(newOldTimeout);
            result.ReceiveBufferSize.Should().Be(subject.ReceiveBufferSize);
            result.SendBufferSize.Should().Be(subject.SendBufferSize);
            result.SocketConfigurator.Should().Be(subject.SocketConfigurator);
            result.WriteTimeout.Should().Be(subject.WriteTimeout);
        }

        [Fact]
        public void With_receiveBufferSize_should_return_expected_result()
        {
            var oldReceiveBufferSize = 1;
            var newReceiveBufferSize = 2;
            var subject = new TcpStreamSettings(receiveBufferSize: oldReceiveBufferSize);

            var result = subject.With(receiveBufferSize: newReceiveBufferSize);

            result.AddressFamily.Should().Be(subject.AddressFamily);
            result.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            result.ReadTimeout.Should().Be(subject.ReadTimeout);
            result.ReceiveBufferSize.Should().Be(newReceiveBufferSize);
            result.SendBufferSize.Should().Be(subject.SendBufferSize);
            result.SocketConfigurator.Should().Be(subject.SocketConfigurator);
            result.WriteTimeout.Should().Be(subject.WriteTimeout);
        }

        [Fact]
        public void With_sendBufferSize_should_return_expected_result()
        {
            var oldSendBufferSize = 1;
            var newSendBufferSize = 2;
            var subject = new TcpStreamSettings(sendBufferSize: oldSendBufferSize);

            var result = subject.With(sendBufferSize: newSendBufferSize);

            result.AddressFamily.Should().Be(subject.AddressFamily);
            result.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            result.ReadTimeout.Should().Be(subject.ReadTimeout);
            result.ReceiveBufferSize.Should().Be(subject.ReceiveBufferSize);
            result.SendBufferSize.Should().Be(newSendBufferSize);
            result.SocketConfigurator.Should().Be(subject.SocketConfigurator);
            result.WriteTimeout.Should().Be(subject.WriteTimeout);
        }

        [Fact]
        public void With_socketConfigurator_should_return_expected_result()
        {
            Action<Socket> oldSocketConfigurator = null;
            Action<Socket> newSocketConfigurator = s => { };
            var subject = new TcpStreamSettings(socketConfigurator: oldSocketConfigurator);

            var result = subject.With(socketConfigurator: newSocketConfigurator);

            result.AddressFamily.Should().Be(subject.AddressFamily);
            result.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            result.ReadTimeout.Should().Be(subject.ReadTimeout);
            result.ReceiveBufferSize.Should().Be(subject.ReceiveBufferSize);
            result.SendBufferSize.Should().Be(subject.SendBufferSize);
            result.SocketConfigurator.Should().Be(newSocketConfigurator);
            result.WriteTimeout.Should().Be(subject.WriteTimeout);
        }

        [Fact]
        public void With_writeTimeout_should_return_expected_result()
        {
            var oldWriteTimeout = TimeSpan.FromSeconds(1);
            var newWriteTimeout = TimeSpan.FromSeconds(2);
            var subject = new TcpStreamSettings(writeTimeout: oldWriteTimeout);

            var result = subject.With(writeTimeout: newWriteTimeout);

            result.AddressFamily.Should().Be(subject.AddressFamily);
            result.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            result.ReadTimeout.Should().Be(subject.ReadTimeout);
            result.ReceiveBufferSize.Should().Be(subject.ReceiveBufferSize);
            result.SendBufferSize.Should().Be(subject.SendBufferSize);
            result.SocketConfigurator.Should().Be(subject.SocketConfigurator);
            result.WriteTimeout.Should().Be(newWriteTimeout);
        }
    }
}