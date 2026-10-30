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
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Core.Configuration;
using Xunit;
using ClusterBuilder = MongoDB.Driver.Configuration.ClusterBuilder;

namespace MongoDB.Driver.Tests.Configuration;

public class ClusterBuilderTests
{
    [Fact]
    public void SrvAllowedHostsSuffix_should_default_to_null()
    {
        var subject = new ClusterBuilder();

        subject.SrvAllowedHostsSuffix.Should().BeNull();
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData(".example.com")]
    [InlineData("sub.example.com")]
    public void SrvAllowedHostsSuffix_should_accept_valid_values(string value)
    {
        var subject = new ClusterBuilder();

        subject.SrvAllowedHostsSuffix = value;

        subject.SrvAllowedHostsSuffix.Should().Be(value);
    }

    [Fact]
    public void SrvAllowedHostsSuffix_should_accept_null_after_being_set()
    {
        var subject = new ClusterBuilder { SrvAllowedHostsSuffix = "example.com" };

        subject.SrvAllowedHostsSuffix = null;

        subject.SrvAllowedHostsSuffix.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("notallowedsingle")]
    public void SrvAllowedHostsSuffix_should_throw_for_invalid_values(string value)
    {
        var subject = new ClusterBuilder { SrvAllowedHostsSuffix = "example.com" };

        var exception = Record.Exception(() => subject.SrvAllowedHostsSuffix = value);

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("SrvAllowedHostsSuffix");
        subject.SrvAllowedHostsSuffix.Should().Be("example.com");
    }

    private static readonly EndPoint Host1 = new DnsEndPoint("host1", 27017);
    private static readonly EndPoint Host2 = new DnsEndPoint("host2", 27017);

    [Fact]
    public void Constructor_should_initialize_defaults()
    {
        var subject = new ClusterBuilder();

        subject.DirectConnection.Should().BeFalse();
        subject.LoadBalanced.Should().BeFalse();
        subject.ReplicaSetName.Should().BeNull();
        subject.Scheme.Should().Be(ConnectionStringScheme.MongoDB);
        subject.Servers.Should().BeNull();
        subject.SrvAllowedHostsSuffix.Should().BeNull();
        subject.SrvMaxHosts.Should().Be(0);
        subject.SrvServiceName.Should().Be("mongodb");
    }

    [Fact]
    public void Servers_should_accept_dns_and_ip_end_points()
    {
        var endPoints = new EndPoint[] { Host1, new IPEndPoint(IPAddress.Loopback, 27018) };

        var subject = new ClusterBuilder { Servers = endPoints };

        subject.Servers.Should().Equal(endPoints);
    }

    [Fact]
    public void Servers_should_be_a_copy_of_the_supplied_end_points()
    {
        var endPoints = new List<EndPoint> { Host1 };

        var subject = new ClusterBuilder { Servers = endPoints };
        endPoints.Add(Host2);

        subject.Servers.Should().Equal(Host1);
    }

    [Fact]
    public void Servers_should_throw_when_value_is_null()
    {
        var exception = Record.Exception(() => new ClusterBuilder { Servers = null });

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("Servers");
    }

    [Theory]
    [MemberData(nameof(InvalidServers))]
    public void Servers_should_throw_for_invalid_values(string description, EndPoint[] servers)
    {
        var exception = Record.Exception(() => new ClusterBuilder { Servers = servers });

        exception.Should().BeOfType<ArgumentException>(because: description)
            .Subject.ParamName.Should().Be("Servers");
    }

    public static IEnumerable<object[]> InvalidServers =>
    [
        ["empty", Array.Empty<EndPoint>()],
        ["null element", new EndPoint[] { null }],
        ["unsupported end point type", new EndPoint[] { new CustomEndPoint() }],
        ["dns end point with an address family", new EndPoint[] { new DnsEndPoint("host1", 27017, AddressFamily.InterNetworkV6) }]
    ];

    [Theory]
    [InlineData(null)]
    [InlineData("rs0")]
    public void ReplicaSetName_should_accept_valid_values(string value)
    {
        var subject = new ClusterBuilder { ReplicaSetName = value };

        subject.ReplicaSetName.Should().Be(value);
    }

    [Fact]
    public void ReplicaSetName_should_throw_when_value_is_empty()
    {
        var exception = Record.Exception(() => new ClusterBuilder { ReplicaSetName = "" });

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("ReplicaSetName");
    }

    [Fact]
    public void SrvMaxHosts_should_throw_when_value_is_negative()
    {
        var subject = new ClusterBuilder();

        var exception = Record.Exception(() => subject.SrvMaxHosts = -1);

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("SrvMaxHosts");
    }

    [Fact]
    public void SrvServiceName_should_throw_when_value_is_empty()
    {
        var subject = new ClusterBuilder();

        var exception = Record.Exception(() => subject.SrvServiceName = "");

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("SrvServiceName");
    }

    private sealed class CustomEndPoint : EndPoint
    {
    }
}
