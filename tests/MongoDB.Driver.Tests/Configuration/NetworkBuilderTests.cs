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
using System.Net.Sockets;
using FluentAssertions;
using MongoDB.Driver.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class NetworkBuilderTests
{
    [Fact]
    public void BufferSizes_should_default_to_MongoDefaults()
    {
        var subject = new NetworkBuilder();

        subject.ReceiveBufferSize.Should().Be(MongoDefaults.TcpReceiveBufferSize);
        subject.SendBufferSize.Should().Be(MongoDefaults.TcpSendBufferSize);
    }

    [Fact]
    public void BufferSizes_should_be_settable()
    {
        var subject = new NetworkBuilder();

        subject.ReceiveBufferSize = 1024;
        subject.SendBufferSize = 2048;

        subject.ReceiveBufferSize.Should().Be(1024);
        subject.SendBufferSize.Should().Be(2048);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReceiveBufferSize_should_throw_when_not_positive(int value)
    {
        var subject = new NetworkBuilder();

        var exception = Record.Exception(() => subject.ReceiveBufferSize = value);

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("ReceiveBufferSize");
        subject.ReceiveBufferSize.Should().Be(MongoDefaults.TcpReceiveBufferSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SendBufferSize_should_throw_when_not_positive(int value)
    {
        var subject = new NetworkBuilder();

        var exception = Record.Exception(() => subject.SendBufferSize = value);

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("SendBufferSize");
        subject.SendBufferSize.Should().Be(MongoDefaults.TcpSendBufferSize);
    }

    [Fact]
    public void SocketConfigurator_should_default_to_null()
    {
        var subject = new NetworkBuilder();

        subject.SocketConfigurator.Should().BeNull();
    }

    [Fact]
    public void SocketConfigurator_should_be_settable()
    {
        var configurator = new Action<Socket>(_ => { });
        var subject = new NetworkBuilder();

        subject.SocketConfigurator = configurator;

        subject.SocketConfigurator.Should().BeSameAs(configurator);
    }
}
