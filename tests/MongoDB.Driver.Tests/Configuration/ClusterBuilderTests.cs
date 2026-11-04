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
    private static readonly EndPoint Host1 = new DnsEndPoint("host1", 27017);
    private static readonly EndPoint Host2 = new DnsEndPoint("host2", 27017);
    private static readonly EndPoint SrvHost = new DnsEndPoint("cluster0.example.com", ConnectionString.DefaultSrvPort);

    [Fact]
    public void SrvAllowedHostsSuffix_should_default_to_null()
    {
        var subject = new ClusterBuilder(ConnectionStringScheme.MongoDB, [Host1]);

        subject.SrvAllowedHostsSuffix.Should().BeNull();
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData(".example.com")]
    [InlineData("sub.example.com")]
    public void SrvAllowedHostsSuffix_should_accept_valid_values(string value)
    {
        var subject = new ClusterBuilder(ConnectionStringScheme.MongoDB, [Host1]);

        subject.SrvAllowedHostsSuffix = value;

        subject.SrvAllowedHostsSuffix.Should().Be(value);
    }

    [Fact]
    public void SrvAllowedHostsSuffix_should_accept_null_after_being_set()
    {
        var subject = new ClusterBuilder(ConnectionStringScheme.MongoDB, [Host1]) { SrvAllowedHostsSuffix = "example.com" };

        subject.SrvAllowedHostsSuffix = null;

        subject.SrvAllowedHostsSuffix.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("notallowedsingle")]
    public void SrvAllowedHostsSuffix_should_throw_for_invalid_values(string value)
    {
        var subject = new ClusterBuilder(ConnectionStringScheme.MongoDB, [Host1]) { SrvAllowedHostsSuffix = "example.com" };

        var exception = Record.Exception(() => subject.SrvAllowedHostsSuffix = value);

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("SrvAllowedHostsSuffix");
        subject.SrvAllowedHostsSuffix.Should().Be("example.com");
    }

    [Fact]
    public void Constructor_should_initialize_defaults()
    {
        var subject = new ClusterBuilder(ConnectionStringScheme.MongoDB, [Host1]);

        subject.DirectConnection.Should().BeFalse();
        subject.LoadBalanced.Should().BeFalse();
        subject.ReplicaSetName.Should().BeNull();
        subject.SrvAllowedHostsSuffix.Should().BeNull();
        subject.SrvMaxHosts.Should().Be(0);
        subject.SrvServiceName.Should().Be("mongodb");
    }

    [Fact]
    public void Constructor_should_set_scheme_and_servers()
    {
        var servers = new EndPoint[] { Host1, new IPEndPoint(IPAddress.Loopback, 27018) };

        var subject = new ClusterBuilder(ConnectionStringScheme.MongoDBPlusSrv, servers);

        subject.Scheme.Should().Be(ConnectionStringScheme.MongoDBPlusSrv);
        subject.Servers.Should().Equal(servers);
    }

    [Fact]
    public void Constructor_should_throw_when_servers_is_null()
    {
        var exception = Record.Exception(() => new ClusterBuilder(ConnectionStringScheme.MongoDB, null));

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("servers");
    }

    [Fact]
    public void Target_properties_should_be_settable()
    {
        var subject = new ClusterBuilder(ConnectionStringScheme.MongoDB, [Host1]);

        subject.DirectConnection = true;
        subject.LoadBalanced = true;
        subject.ReplicaSetName = "rs0";

        subject.DirectConnection.Should().BeTrue();
        subject.LoadBalanced.Should().BeTrue();
        subject.ReplicaSetName.Should().Be("rs0");
    }

    [Fact]
    public void ReplicaSetName_should_throw_when_value_is_empty()
    {
        var exception = Record.Exception(() => new ClusterBuilder(ConnectionStringScheme.MongoDB, [Host1]) { ReplicaSetName = "" });

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("ReplicaSetName");
    }

    [Fact]
    public void SrvMaxHosts_should_throw_when_value_is_negative()
    {
        var subject = new ClusterBuilder(ConnectionStringScheme.MongoDB, [Host1]);

        var exception = Record.Exception(() => subject.SrvMaxHosts = -1);

        exception.Should().BeOfType<ArgumentOutOfRangeException>()
            .Subject.ParamName.Should().Be("SrvMaxHosts");
    }

    [Fact]
    public void SrvServiceName_should_throw_when_value_is_empty()
    {
        var subject = new ClusterBuilder(ConnectionStringScheme.MongoDB, [Host1]);

        var exception = Record.Exception(() => subject.SrvServiceName = "");

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("SrvServiceName");
    }

    [Theory]
    [MemberData(nameof(ValidConfigurations))]
    public void Validate_should_accept_valid_configuration(string description, Func<ClusterBuilder> create)
    {
        var subject = create();

        var exception = Record.Exception(() => subject.Validate());

        exception.Should().BeNull(because: description);
    }

    public static IEnumerable<object[]> ValidConfigurations =>
    [
        ["single server", () => Create()],
        ["replica set", () => Create([Host1, Host2], b => b.ReplicaSetName = "rs")],
        ["direct connection", () => Create(b => b.DirectConnection = true)],
        ["load balanced", () => Create(b => b.LoadBalanced = true)],
        ["srv", () => CreateSrv()],
        ["srv with max hosts", () => CreateSrv(b => b.SrvMaxHosts = 2)],
        ["srv with service name", () => CreateSrv(b => b.SrvServiceName = "custom")],
        ["srv with allowed hosts suffix", () => CreateSrv(b => b.SrvAllowedHostsSuffix = ".example.com")]
    ];

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void Validate_should_throw_for_invalid_configuration(string expectedMessage, Func<ClusterBuilder> create)
    {
        var subject = create();

        var exception = Record.Exception(() => subject.Validate());

        exception.Should().BeOfType<InvalidOperationException>();
        exception.Message.Should().Contain(expectedMessage);
    }

    public static IEnumerable<object[]> InvalidConfigurations =>
    [
        ["At least one server must be specified", () => new ClusterBuilder(ConnectionStringScheme.MongoDB, [])],
        [
            "Servers must be specified as DnsEndPoint or IPEndPoint",
            () => new ClusterBuilder(ConnectionStringScheme.MongoDB, [new UnixDomainSocketEndPoint("/tmp/mongodb.sock")])
        ],
        [
            "Only 1 host is allowed when using the mongodb+srv scheme",
            () => new ClusterBuilder(ConnectionStringScheme.MongoDBPlusSrv, [SrvHost, new DnsEndPoint("cluster1.example.com", 53)])
        ],
        [
            "must be a DNS name",
            () => new ClusterBuilder(ConnectionStringScheme.MongoDBPlusSrv, [new IPEndPoint(IPAddress.Loopback, 53)])
        ],
        ["SRV cannot be used with direct connections", () => CreateSrv(b => b.DirectConnection = true)],
        [
            "Multiple host names cannot be used with direct connections",
            () => Create([Host1, Host2], b => b.DirectConnection = true)
        ],
        ["SrvMaxHosts can only be used with the mongodb+srv scheme", () => Create(b => b.SrvMaxHosts = 1)],
        [
            "Specifying SrvMaxHosts when connecting to a replica set is invalid",
            () => CreateSrv(b =>
            {
                b.SrvMaxHosts = 1;
                b.ReplicaSetName = "rs";
            })
        ],
        ["SrvServiceName is only allowed with the mongodb+srv scheme", () => Create(b => b.SrvServiceName = "custom")],
        [
            "SrvAllowedHostsSuffix is only allowed with the mongodb+srv scheme",
            () => Create(b => b.SrvAllowedHostsSuffix = ".example.com")
        ],
        [
            "Load balanced mode cannot be used with multiple host names",
            () => Create([Host1, Host2], b => b.LoadBalanced = true)
        ],
        [
            "ReplicaSetName cannot be used with load balanced mode",
            () => Create(b =>
            {
                b.LoadBalanced = true;
                b.ReplicaSetName = "rs";
            })
        ],
        [
            "SrvMaxHosts cannot be used with load balanced mode",
            () => CreateSrv(b =>
            {
                b.LoadBalanced = true;
                b.SrvMaxHosts = 1;
            })
        ],
        [
            "Load balanced mode cannot be used with direct connection",
            () => Create(b =>
            {
                b.LoadBalanced = true;
                b.DirectConnection = true;
            })
        ]
    ];

    private static ClusterBuilder Create(Action<ClusterBuilder> configure = null)
        => Create([Host1], configure);

    private static ClusterBuilder Create(EndPoint[] servers, Action<ClusterBuilder> configure = null)
    {
        var builder = new ClusterBuilder(ConnectionStringScheme.MongoDB, servers);
        configure?.Invoke(builder);
        return builder;
    }

    private static ClusterBuilder CreateSrv(Action<ClusterBuilder> configure = null)
    {
        var builder = new ClusterBuilder(ConnectionStringScheme.MongoDBPlusSrv, [SrvHost]);
        configure?.Invoke(builder);
        return builder;
    }
}
