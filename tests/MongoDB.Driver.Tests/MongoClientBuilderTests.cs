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
using FluentAssertions;
using MongoDB.Driver.Core.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests;

public class MongoClientBuilderTests
{
    public static IEnumerable<object[]> ConfigureMethods =>
    [
        [nameof(MongoClientBuilder.Authentication), new Action<MongoClientBuilder>(b => b.Authentication(null))],
        [nameof(MongoClientBuilder.AutoEncryption), new Action<MongoClientBuilder>(b => b.AutoEncryption(null))],
        [nameof(MongoClientBuilder.ClientMetadata), new Action<MongoClientBuilder>(b => b.ClientMetadata(null))],
        [nameof(MongoClientBuilder.Cluster), new Action<MongoClientBuilder>(b => b.Cluster(null))],
        [nameof(MongoClientBuilder.ConnectionPool), new Action<MongoClientBuilder>(b => b.ConnectionPool(null))],
        [nameof(MongoClientBuilder.Diagnostics), new Action<MongoClientBuilder>(b => b.Diagnostics(null))],
        [nameof(MongoClientBuilder.Network), new Action<MongoClientBuilder>(b => b.Network(null))],
        [nameof(MongoClientBuilder.Operations), new Action<MongoClientBuilder>(b => b.Operations(null))],
        [nameof(MongoClientBuilder.ServerMonitoring), new Action<MongoClientBuilder>(b => b.ServerMonitoring(null))],
        [nameof(MongoClientBuilder.Tls), new Action<MongoClientBuilder>(b => b.Tls(null))],
        [nameof(MongoClientBuilder.Translation), new Action<MongoClientBuilder>(b => b.Translation(null))]
    ];

    [Theory]
    [MemberData(nameof(ConfigureMethods))]
    public void Configure_method_should_throw_when_configure_is_null(string methodName, Action<MongoClientBuilder> callWithNull)
    {
        var subject = MongoClientBuilder.FromEndPoints(["localhost"]);

        var exception = Record.Exception(() => callWithNull(subject));

        exception.Should().BeOfType<ArgumentNullException>(because: methodName)
            .Subject.ParamName.Should().Be("configure");
    }

    [Fact]
    public void FromEndPoint_with_end_point_should_discover_the_topology()
    {
        var endPoint = new DnsEndPoint("host1", 27018);

        var subject = MongoClientBuilder.FromEndPoint(endPoint);

        var cluster = subject.Cluster();
        cluster.Servers.Should().Equal(endPoint);
        cluster.DirectConnection.Should().BeFalse();
        cluster.LoadBalanced.Should().BeFalse();
        cluster.ReplicaSetName.Should().BeNull();
        cluster.Scheme.Should().Be(ConnectionStringScheme.MongoDB);
    }

    [Theory]
    [InlineData("host1", "host1", 27017)]
    [InlineData("HOST1:27018", "host1", 27018)]
    public void FromEndPoint_with_string_should_parse_the_address(string endPoint, string expectedHost, int expectedPort)
    {
        var subject = MongoClientBuilder.FromEndPoint(endPoint);

        subject.Cluster().Servers.Should().Equal(new DnsEndPoint(expectedHost, expectedPort));
    }

    [Theory]
    [InlineData("host1:notaport")]
    [InlineData("host1:0")]
    [InlineData("")]
    public void FromEndPoint_with_string_should_throw_for_an_invalid_address(string endPoint)
    {
        var exception = Record.Exception(() => MongoClientBuilder.FromEndPoint(endPoint));

        exception.Should().BeAssignableTo<ArgumentException>()
            .Subject.ParamName.Should().Be("endPoint");
    }

    [Fact]
    public void FromEndPoints_with_end_points_should_discover_the_topology()
    {
        var endPoints = new EndPoint[] { new DnsEndPoint("host1", 27017), new IPEndPoint(IPAddress.Loopback, 27018) };

        var subject = MongoClientBuilder.FromEndPoints(endPoints);

        var cluster = subject.Cluster();
        cluster.Servers.Should().Equal(endPoints);
        cluster.DirectConnection.Should().BeFalse();
        cluster.LoadBalanced.Should().BeFalse();
        cluster.ReplicaSetName.Should().BeNull();
        cluster.Scheme.Should().Be(ConnectionStringScheme.MongoDB);
    }

    [Fact]
    public void FromEndPoints_with_enumerable_should_use_the_end_points()
    {
        var endPoints = new List<EndPoint> { new DnsEndPoint("host1", 27017), new DnsEndPoint("host2", 27017) };

        var subject = MongoClientBuilder.FromEndPoints(endPoints);

        subject.Cluster().Servers.Should().Equal(endPoints);
    }

    [Fact]
    public void FromEndPoints_with_strings_should_parse_the_addresses()
    {
        var subject = MongoClientBuilder.FromEndPoints(["host1", "HOST2:27018", "[::1]:27019"]);

        subject.Cluster().Servers.Should().Equal(
            new DnsEndPoint("host1", 27017),
            new DnsEndPoint("host2", 27018),
            new IPEndPoint(IPAddress.IPv6Loopback, 27019));
    }

    [Theory]
    [MemberData(nameof(InvalidEndPointsFactories))]
    public void Factories_should_throw_for_invalid_end_points(string description, Func<MongoClientBuilder> create, string expectedParamName)
    {
        var exception = Record.Exception(() => create());

        exception.Should().BeAssignableTo<ArgumentException>(because: description)
            .Subject.ParamName.Should().Be(expectedParamName);
    }

    public static IEnumerable<object[]> InvalidEndPointsFactories =>
    [
        ["FromEndPoints with an invalid address", () => MongoClientBuilder.FromEndPoints(["host1:notaport"]), "endPoints"],
        ["FromEndPoints with a zero port", () => MongoClientBuilder.FromEndPoints(["host1:0"]), "endPoints"],
        ["FromEndPoints with an empty address", () => MongoClientBuilder.FromEndPoints([""]), "endPoints"]
    ];

    [Fact]
    public void FromSrv_should_configure_the_srv_scheme()
    {
        var subject = MongoClientBuilder.FromSrv("Cluster0.Example.com");

        var cluster = subject.Cluster();
        cluster.Scheme.Should().Be(ConnectionStringScheme.MongoDBPlusSrv);
        cluster.Servers.Should().Equal(new DnsEndPoint("cluster0.example.com", ConnectionString.DefaultSrvPort));
        cluster.DirectConnection.Should().BeFalse();
        cluster.LoadBalanced.Should().BeFalse();
        cluster.ReplicaSetName.Should().BeNull();
    }

    [Fact]
    public void FromSrv_should_allow_srv_options_to_be_set()
    {
        var subject = MongoClientBuilder.FromSrv("cluster0.example.com")
            .Cluster(cluster => cluster.SrvMaxHosts = 3);

        subject.Cluster().SrvMaxHosts.Should().Be(3);
    }

    [Theory]
    [InlineData("cluster0.example.com:27017", "host")]
    [InlineData("127.0.0.1", "host")]
    [InlineData("", "host")]
    [InlineData(null, "host")]
    public void FromSrv_should_throw_for_invalid_host(string host, string expectedParamName)
    {
        var exception = Record.Exception(() => MongoClientBuilder.FromSrv(host));

        exception.Should().BeAssignableTo<ArgumentException>()
            .Subject.ParamName.Should().Be(expectedParamName);
    }
}
