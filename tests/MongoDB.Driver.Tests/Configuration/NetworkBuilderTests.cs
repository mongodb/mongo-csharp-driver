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
using System.Threading;
using FluentAssertions;
using MongoDB.Driver.Compression;
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class NetworkBuilderTests
{
    [Fact]
    public void ConnectTimeout_should_default_to_MongoDefaults()
    {
        var subject = new NetworkBuilder();

        subject.ConnectTimeout.Should().Be(MongoDefaults.ConnectTimeout);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5000)]
    public void ConnectTimeout_should_accept_positive_values(int milliseconds)
    {
        var subject = new NetworkBuilder();
        var value = TimeSpan.FromMilliseconds(milliseconds);

        subject.ConnectTimeout = value;

        subject.ConnectTimeout.Should().Be(value);
    }

    [Fact]
    public void ConnectTimeout_should_accept_Timeout_InfiniteTimeSpan()
    {
        var subject = new NetworkBuilder();

        subject.ConnectTimeout = Timeout.InfiniteTimeSpan;

        subject.ConnectTimeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(-1000)]
    public void ConnectTimeout_should_throw_when_value_is_zero_or_negative_and_not_infinite(int milliseconds)
    {
        var subject = new NetworkBuilder();

        var exception = Record.Exception(() => subject.ConnectTimeout = TimeSpan.FromMilliseconds(milliseconds));

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be(nameof(NetworkBuilder.ConnectTimeout));
        subject.ConnectTimeout.Should().Be(MongoDefaults.ConnectTimeout);
    }

    [Fact]
    public void SocketTimeout_should_default_to_infinite()
    {
        var subject = new NetworkBuilder();

        subject.SocketTimeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5000)]
    [InlineData(-1)] // Timeout.InfiniteTimeSpan
    public void SocketTimeout_should_accept_infinite_and_positive_values(int milliseconds)
    {
        var subject = new NetworkBuilder();
        var value = TimeSpan.FromMilliseconds(milliseconds);

        subject.SocketTimeout = value;

        subject.SocketTimeout.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(-1000)]
    public void SocketTimeout_should_throw_when_value_is_zero_or_negative_and_not_infinite(int milliseconds)
    {
        var subject = new NetworkBuilder();

        var exception = Record.Exception(() => subject.SocketTimeout = TimeSpan.FromMilliseconds(milliseconds));

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be(nameof(NetworkBuilder.SocketTimeout));
        subject.SocketTimeout.Should().Be(Timeout.InfiniteTimeSpan);
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

    [Fact]
    public void Compressors_should_default_to_empty()
    {
        var subject = new NetworkBuilder();

        subject.Compressors.Should().BeEmpty();
    }

    [Fact]
    public void Compressors_should_be_settable()
    {
        var compressors = new[] { new CompressorConfiguration(CompressorType.Zlib), new CompressorConfiguration(CompressorType.Snappy) };
        var subject = new NetworkBuilder();

        subject.Compressors = compressors;

        subject.Compressors.Should().Equal(compressors);
    }

    [Fact]
    public void Compressors_should_throw_when_value_is_null()
    {
        var subject = new NetworkBuilder();

        var exception = Record.Exception(() => subject.Compressors = null);

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("Compressors");
    }

    [Fact]
    public void IPv6_should_default_to_false_and_be_settable()
    {
        var subject = new NetworkBuilder();
        subject.IPv6.Should().BeFalse();

        subject.IPv6 = true;

        subject.IPv6.Should().BeTrue();
    }

    [Fact]
    public void Socks5Proxy_should_default_to_null_and_be_settable()
    {
        var proxy = Socks5ProxySettings.Create("proxy", 1080, null, null);
        var subject = new NetworkBuilder();
        subject.Socks5Proxy.Should().BeNull();

        subject.Socks5Proxy = proxy;

        subject.Socks5Proxy.Should().BeSameAs(proxy);
    }

    [Fact]
    public void ApplyConnectionString_should_apply_the_options()
    {
        var subject = new NetworkBuilder();
        var connectionString = new ConnectionString(
            "mongodb://host1/?compressors=zlib&zlibCompressionLevel=3&connectTimeoutMS=1000&socketTimeoutMS=2000&ipv6=true" +
            "&proxyHost=proxy&proxyPort=1080&proxyUsername=u&proxyPassword=p");

        subject.ApplyConnectionString(connectionString);

        subject.Compressors.Should().HaveCount(1);
        subject.Compressors[0].Type.Should().Be(CompressorType.Zlib);
        subject.ConnectTimeout.Should().Be(TimeSpan.FromSeconds(1));
        subject.SocketTimeout.Should().Be(TimeSpan.FromSeconds(2));
        subject.IPv6.Should().BeTrue();
        subject.Socks5Proxy.Should().Be(Socks5ProxySettings.Create("proxy", 1080, "u", "p"));
    }

    [Fact]
    public void ApplyConnectionString_should_keep_defaults_when_options_are_not_specified()
    {
        var subject = new NetworkBuilder();

        subject.ApplyConnectionString(new ConnectionString("mongodb://host1"));

        subject.Compressors.Should().BeEmpty();
        subject.ConnectTimeout.Should().Be(MongoDefaults.ConnectTimeout);
        subject.IPv6.Should().BeFalse();
        subject.Socks5Proxy.Should().BeNull();
    }
}
