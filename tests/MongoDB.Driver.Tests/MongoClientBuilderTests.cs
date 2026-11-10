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
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
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

    public static IEnumerable<object[]> ConnectionStringsMatchingMongoClientSettings =>
    [
        ["mongodb://host1"],
        ["mongodb://host1:27018,host2/?replicaSet=rs"],
        ["mongodb://host1/?directConnection=true"],
        ["mongodb://host1/?loadBalanced=true"],
        ["mongodb://host1/?appName=app&connectTimeoutMS=1000&heartbeatFrequencyMS=2000&heartbeatTimeoutMS=3000&localThresholdMS=20&ipv6=true"],
        ["mongodb://host1/?maxPoolSize=50&minPoolSize=5&maxConnecting=3&maxIdleTimeMS=1000&maxLifeTimeMS=60000&waitQueueTimeoutMS=500"],
        ["mongodb://host1/?serverSelectionTimeoutMS=5000&retryReads=false&retryWrites=false&serverMonitoringMode=poll"],
        ["mongodb://host1/?readConcernLevel=majority&w=majority&wtimeoutMS=100&journal=true"],
        ["mongodb://host1/?readPreference=secondary&readPreferenceTags=dc:east&maxStalenessSeconds=120"],
        ["mongodb://host1/?tls=true"],
        ["mongodb://host1/?tls=true&tlsInsecure=true"],
        ["mongodb://host1/?compressors=zlib&zlibCompressionLevel=3"],
        ["mongodb://host1/?proxyHost=proxy&proxyPort=1080&proxyUsername=u&proxyPassword=p"],
        ["mongodb://user:pass@host1/db?authSource=other"],
        ["mongodb://user:pass@host1/?authMechanism=SCRAM-SHA-256"],
        ["mongodb://user@host1/?authMechanism=GSSAPI&authMechanismProperties=SERVICE_NAME:other,CANONICALIZE_HOST_NAME:true"],
        ["mongodb://host1/?authSource=admin"]
    ];

    [Theory]
    [MemberData(nameof(ConnectionStringsMatchingMongoClientSettings))]
    public void FromConnectionString_should_produce_the_settings_of_MongoClientSettings(string connectionString)
    {
        var expected = MongoClientSettings.FromConnectionString(connectionString);

        var result = MongoClientBuilder.FromConnectionString(connectionString).ToMongoClientSettings();

        result.AllowInsecureTls.Should().Be(expected.AllowInsecureTls);
        result.ApplicationName.Should().Be(expected.ApplicationName);
        result.Compressors.Should().Equal(expected.Compressors);
        result.ConnectTimeout.Should().Be(expected.ConnectTimeout);
        result.Credential.Should().Be(expected.Credential);
        result.DirectConnection.Should().Be(expected.DirectConnection);
        result.EnableOverloadRetargeting.Should().Be(expected.EnableOverloadRetargeting);
        result.HeartbeatInterval.Should().Be(expected.HeartbeatInterval);
        result.HeartbeatTimeout.Should().Be(expected.HeartbeatTimeout);
        result.IPv6.Should().Be(expected.IPv6);
        result.LoadBalanced.Should().Be(expected.LoadBalanced);
        result.LocalThreshold.Should().Be(expected.LocalThreshold);
        result.MaxAdaptiveRetries.Should().Be(expected.MaxAdaptiveRetries);
        result.MaxConnecting.Should().Be(expected.MaxConnecting);
        result.MaxConnectionIdleTime.Should().Be(expected.MaxConnectionIdleTime);
        result.MaxConnectionLifeTime.Should().Be(expected.MaxConnectionLifeTime);
        result.MaxConnectionPoolSize.Should().Be(expected.MaxConnectionPoolSize);
        result.MinConnectionPoolSize.Should().Be(expected.MinConnectionPoolSize);
        result.ReadConcern.Should().Be(expected.ReadConcern);
        result.ReadPreference.Should().Be(expected.ReadPreference);
        result.ReplicaSetName.Should().Be(expected.ReplicaSetName);
        result.RetryReads.Should().Be(expected.RetryReads);
        result.RetryWrites.Should().Be(expected.RetryWrites);
        result.Scheme.Should().Be(expected.Scheme);
        result.Servers.Should().Equal(expected.Servers);
        result.ServerMonitoringMode.Should().Be(expected.ServerMonitoringMode);
        result.ServerSelectionTimeout.Should().Be(expected.ServerSelectionTimeout);
        result.Socks5ProxySettings.Should().Be(expected.Socks5ProxySettings);
        result.SrvAllowedHostsSuffix.Should().Be(expected.SrvAllowedHostsSuffix);
        result.SrvMaxHosts.Should().Be(expected.SrvMaxHosts);
        result.SrvServiceName.Should().Be(expected.SrvServiceName);
        result.UseTls.Should().Be(expected.UseTls);
        result.WaitQueueTimeout.Should().Be(expected.WaitQueueTimeout);
        result.WriteConcern.Should().Be(expected.WriteConcern);
    }

    [Fact]
    public void FromConnectionString_should_keep_defaults_for_options_that_are_not_specified()
    {
        var expected = MongoClientBuilder.FromEndPoint("host1").ToMongoClientSettings();

        var result = MongoClientBuilder.FromConnectionString("mongodb://host1").ToMongoClientSettings();

        result.Should().Be(expected);
    }

    [Fact]
    public void FromConnectionString_should_apply_the_options_to_the_builders()
    {
        var subject = MongoClientBuilder.FromConnectionString(
            "mongodb://user:pass@host1,host2/?replicaSet=rs&tls=true&maxPoolSize=7&retryWrites=false&appName=app&heartbeatFrequencyMS=2000");

        subject.Cluster().Servers.Should().Equal(new DnsEndPoint("host1", 27017), new DnsEndPoint("host2", 27017));
        subject.Cluster().ReplicaSetName.Should().Be("rs");
        subject.Cluster().Scheme.Should().Be(ConnectionStringScheme.MongoDB);
        subject.Tls().Enabled.Should().BeTrue();
        subject.ConnectionPool().MaxSize.Should().Be(7);
        subject.Operations().RetryWrites.Should().BeFalse();
        subject.ClientMetadata().ApplicationName.Should().Be("app");
        subject.ServerMonitoring().HeartbeatInterval.Should().Be(TimeSpan.FromSeconds(2));
        subject.Authentication().Credential.Username.Should().Be("user");
    }

    [Fact]
    public void FromConnectionString_should_allow_options_to_be_overridden_in_code()
    {
        var subject = MongoClientBuilder.FromConnectionString("mongodb://host1/?retryWrites=false&maxPoolSize=7")
            .Operations(operations => operations.RetryWrites = true)
            .ConnectionPool(pool => pool.MaxSize = 9);

        var result = subject.ToMongoClientSettings();

        result.RetryWrites.Should().BeTrue();
        result.MaxConnectionPoolSize.Should().Be(9);
    }

    [Fact]
    public void FromConnectionString_should_not_create_a_credential_without_authentication_settings()
    {
        var subject = MongoClientBuilder.FromConnectionString("mongodb://host1/?authSource=admin");

        subject.Authentication().Credential.Should().BeNull();
    }

    [Theory]
    [InlineData("connectTimeoutMS=0")]
    [InlineData("socketTimeoutMS=0")]
    public void FromConnectionString_should_treat_a_zero_timeout_as_infinite(string option)
    {
        var subject = MongoClientBuilder.FromConnectionString($"mongodb://host1/?{option}");

        var network = subject.Network();
        (option.StartsWith("connect") ? network.ConnectTimeout : network.SocketTimeout).Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void FromConnectionString_should_treat_a_zero_max_pool_size_as_no_limit()
    {
        var subject = MongoClientBuilder.FromConnectionString("mongodb://host1/?maxPoolSize=0");

        subject.ConnectionPool().MaxSize.Should().Be(int.MaxValue);
    }

    [Fact]
    public void FromConnectionString_should_apply_srv_options_and_txt_records()
    {
        var resolver = new TxtOnlyDnsResolver("replicaSet=rs0&authSource=admin");
        var connectionString = new ConnectionString("mongodb+srv://cluster0.example.com/?srvMaxHosts=0&appName=app", false, resolver);

        var subject = MongoClientBuilder.FromConnectionString(connectionString);

        var cluster = subject.Cluster();
        cluster.Scheme.Should().Be(ConnectionStringScheme.MongoDBPlusSrv);
        cluster.Servers.Should().Equal(new DnsEndPoint("cluster0.example.com", ConnectionString.DefaultSrvPort));
        cluster.ReplicaSetName.Should().Be("rs0");
        subject.Tls().Enabled.Should().BeTrue();
        subject.ClientMetadata().ApplicationName.Should().Be("app");
        resolver.SrvQueries.Should().Be(0);
    }

    [Fact]
    public void FromConnectionString_should_apply_srv_service_name_and_allowed_hosts_suffix()
    {
        var resolver = new TxtOnlyDnsResolver();
        var connectionString = new ConnectionString(
            "mongodb+srv://cluster0.example.com/?srvServiceName=custom&srvMaxHosts=2&srvAllowedHostsSuffix=.example.com", false, resolver);

        var cluster = MongoClientBuilder.FromConnectionString(connectionString).Cluster();

        cluster.SrvServiceName.Should().Be("custom");
        cluster.SrvMaxHosts.Should().Be(2);
        cluster.SrvAllowedHostsSuffix.Should().Be(".example.com");
    }

    [Fact]
    public void FromConnectionString_should_throw_when_connection_string_is_null()
    {
        var exception = Record.Exception(() => MongoClientBuilder.FromConnectionString((ConnectionString)null));

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("connectionString");
    }

    [Fact]
    public void FromConnectionString_should_throw_when_connection_string_is_invalid()
    {
        var exception = Record.Exception(() => MongoClientBuilder.FromConnectionString("mongodb://host1/?maxPoolSize=abc"));

        exception.Should().BeOfType<MongoConfigurationException>();
    }

    [Fact]
    public void FromConnectionString_should_not_enable_tls_for_tlsInsecure_but_Validate_should_reject_it()
    {
        var subject = MongoClientBuilder.FromConnectionString("mongodb://host1/?tlsInsecure=true");

        subject.Tls().Enabled.Should().BeFalse();
        subject.Tls().AllowInsecure.Should().BeTrue();
        var exception = Record.Exception(() => subject.Validate());
        exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void Build_should_validate_connection_string_options_combined_with_code()
    {
        var subject = MongoClientBuilder.FromConnectionString("mongodb://host1/?maxPoolSize=10")
            .ConnectionPool(pool => pool.MinSize = 20);

        var exception = Record.Exception(() => subject.Build());

        exception.Should().BeOfType<InvalidOperationException>()
            .Subject.Message.Should().Contain("MaxSize must be greater than or equal to MinSize");
    }

    private class TxtOnlyDnsResolver(string txt = null) : IDnsResolver
    {
        public int SrvQueries { get; private set; }

        public List<SrvRecord> ResolveSrvRecords(string service, CancellationToken cancellation)
        {
            SrvQueries++;
            throw new InvalidOperationException("The seed list must not be resolved.");
        }

        public List<TxtRecord> ResolveTxtRecords(string domainName, CancellationToken cancellation)
            => txt == null ? [] : [new TxtRecord([txt])];

        public Task<List<SrvRecord>> ResolveSrvRecordsAsync(string service, CancellationToken cancellation) => throw new NotImplementedException();
        public Task<List<TxtRecord>> ResolveTxtRecordsAsync(string domainName, CancellationToken cancellation) => throw new NotImplementedException();
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

    [Fact]
    public void Build_should_return_a_client_with_frozen_settings()
    {
        var subject = MongoClientBuilder.FromEndPoint("localhost:27017")
            .Operations(operations => operations.RetryWrites = false);

        using var client = (MongoClient)subject.Build();

        client.Settings.IsFrozen.Should().BeTrue();
        client.Settings.RetryWrites.Should().BeFalse();
        client.Settings.Server.Should().Be(new MongoServerAddress("localhost", 27017));
    }

    [Fact]
    public void Build_should_not_consume_the_builder()
    {
        var subject = MongoClientBuilder.FromEndPoint("localhost:27017");

        using var client1 = (MongoClient)subject.Build();
        subject.Operations(operations => operations.RetryReads = false);
        using var client2 = (MongoClient)subject.Build();

        client1.Should().NotBeSameAs(client2);
        client1.Settings.RetryReads.Should().BeTrue();
        client2.Settings.RetryReads.Should().BeFalse();
    }

    [Fact]
    public void Build_should_throw_when_settings_are_invalid()
    {
        var subject = MongoClientBuilder.FromEndPoint("localhost:27017")
            .Cluster(cluster => cluster.LoadBalanced = true)
            .Cluster(cluster => cluster.DirectConnection = true);

        var exception = Record.Exception(() => subject.Build());

        exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void ToMongoClientSettings_should_map_defaults()
    {
        var subject = MongoClientBuilder.FromEndPoint("localhost:27017");
        var expected = new MongoClientSettings();

        var result = subject.ToMongoClientSettings();

        result.AllowInsecureTls.Should().Be(expected.AllowInsecureTls);
        result.ApplicationName.Should().Be(expected.ApplicationName);
        result.AutoEncryptionOptions.Should().BeNull();
        result.ClusterConfigurator.Should().BeNull();
        result.Compressors.Should().BeEmpty();
        result.ConnectTimeout.Should().Be(expected.ConnectTimeout);
        result.Credential.Should().BeNull();
        result.DirectConnection.Should().BeFalse();
        result.EnableOverloadRetargeting.Should().BeFalse();
        result.HeartbeatInterval.Should().Be(expected.HeartbeatInterval);
        result.HeartbeatTimeout.Should().Be(expected.HeartbeatTimeout);
        result.IPv6.Should().BeFalse();
        result.LibraryInfo.Should().BeNull();
        result.LoadBalanced.Should().BeFalse();
        result.LocalThreshold.Should().Be(expected.LocalThreshold);
        result.LoggingSettings.Should().BeNull();
        result.MaxAdaptiveRetries.Should().Be(expected.MaxAdaptiveRetries);
        result.MaxConnecting.Should().Be(expected.MaxConnecting);
        result.MaxConnectionIdleTime.Should().Be(expected.MaxConnectionIdleTime);
        result.MaxConnectionLifeTime.Should().Be(expected.MaxConnectionLifeTime);
        result.MaxConnectionPoolSize.Should().Be(expected.MaxConnectionPoolSize);
        result.MinConnectionPoolSize.Should().Be(expected.MinConnectionPoolSize);
        result.ReadConcern.Should().Be(expected.ReadConcern);
        result.ReadPreference.Should().Be(expected.ReadPreference);
        result.ReplicaSetName.Should().BeNull();
        result.RetryReads.Should().BeTrue();
        result.RetryWrites.Should().BeTrue();
        result.Scheme.Should().Be(ConnectionStringScheme.MongoDB);
        result.ServerApi.Should().BeNull();
        result.Servers.Should().Equal(new MongoServerAddress("localhost", 27017));
        result.ServerMonitoringMode.Should().Be(expected.ServerMonitoringMode);
        result.ServerSelectionTimeout.Should().Be(expected.ServerSelectionTimeout);
        result.Socks5ProxySettings.Should().BeNull();
        result.SrvAllowedHostsSuffix.Should().BeNull();
        result.SrvMaxHosts.Should().Be(0);
        result.SrvServiceName.Should().Be(expected.SrvServiceName);
        result.SslSettings.Should().BeNull();
        result.TracingOptions.Should().BeNull();
        result.TranslationOptions.Should().BeNull();
        result.UseTls.Should().BeFalse();
        result.WaitQueueTimeout.Should().Be(expected.WaitQueueTimeout);
        result.WriteConcern.Should().Be(expected.WriteConcern);
    }

    [Fact]
    public void ToMongoClientSettings_should_map_configured_values()
    {
        var credential = MongoCredential.FromComponents(null, "admin", null, "user", new PasswordEvidence("password"));
        var libraryInfo = new LibraryInfo("lib", "1.0");
        var readPreference = ReadPreference.Secondary;
        var readConcern = ReadConcern.Majority;
        var writeConcern = WriteConcern.WMajority;
        var serverApi = new ServerApi(ServerApiVersion.V1);
        var subject = MongoClientBuilder.FromEndPoints(["host1:27017", "host2:27018"])
            .Authentication(authentication => authentication.Credential = credential)
            .ClientMetadata(metadata =>
            {
                metadata.ApplicationName = "app";
                metadata.LibraryInfo = libraryInfo;
            })
            .Cluster(cluster =>
            {
                cluster.ReplicaSetName = "rs";
            })
            .ConnectionPool(pool =>
            {
                pool.MaxConnecting = 3;
                pool.MaxConnectionIdleTime = TimeSpan.FromMinutes(2);
                pool.MaxConnectionLifeTime = TimeSpan.FromMinutes(3);
                pool.MaxSize = 50;
                pool.MinSize = 5;
                pool.WaitQueueTimeout = TimeSpan.FromSeconds(7);
            })
            .Network(network =>
            {
                network.ConnectTimeout = TimeSpan.FromSeconds(4);
                network.IPv6 = true;
                network.SocketTimeout = TimeSpan.FromSeconds(5);
            })
            .Operations(operations =>
            {
                operations.EnableOverloadRetargeting = true;
                operations.LocalThreshold = TimeSpan.FromMilliseconds(30);
                operations.MaxAdaptiveRetries = 4;
                operations.ReadConcern = readConcern;
                operations.ReadPreference = readPreference;
                operations.RetryReads = false;
                operations.RetryWrites = false;
                operations.ServerApi = serverApi;
                operations.ServerSelectionTimeout = TimeSpan.FromSeconds(9);
                operations.WriteConcern = writeConcern;
            })
            .ServerMonitoring(monitoring =>
            {
                monitoring.HeartbeatInterval = TimeSpan.FromSeconds(11);
                monitoring.HeartbeatTimeout = TimeSpan.FromSeconds(12);
                monitoring.Mode = ServerMonitoringMode.Poll;
            })
            .Translation(translation =>
            {
                translation.CompatibilityLevel = ServerVersion.Server70;
                translation.EnableClientSideProjections = true;
            });

        var result = subject.ToMongoClientSettings();

        result.Credential.Should().BeSameAs(credential);
        result.ApplicationName.Should().Be("app");
        result.LibraryInfo.Should().BeSameAs(libraryInfo);
        result.ReplicaSetName.Should().Be("rs");
        result.Servers.Should().Equal(new MongoServerAddress("host1", 27017), new MongoServerAddress("host2", 27018));
        result.MaxConnecting.Should().Be(3);
        result.MaxConnectionIdleTime.Should().Be(TimeSpan.FromMinutes(2));
        result.MaxConnectionLifeTime.Should().Be(TimeSpan.FromMinutes(3));
        result.MaxConnectionPoolSize.Should().Be(50);
        result.MinConnectionPoolSize.Should().Be(5);
        result.WaitQueueTimeout.Should().Be(TimeSpan.FromSeconds(7));
        result.ConnectTimeout.Should().Be(TimeSpan.FromSeconds(4));
        result.IPv6.Should().BeTrue();
        result.SocketTimeout.Should().Be(TimeSpan.FromSeconds(5));
        result.EnableOverloadRetargeting.Should().BeTrue();
        result.LocalThreshold.Should().Be(TimeSpan.FromMilliseconds(30));
        result.MaxAdaptiveRetries.Should().Be(4);
        result.ReadConcern.Should().BeSameAs(readConcern);
        result.ReadPreference.Should().BeSameAs(readPreference);
        result.RetryReads.Should().BeFalse();
        result.RetryWrites.Should().BeFalse();
        result.ServerApi.Should().BeSameAs(serverApi);
        result.ServerSelectionTimeout.Should().Be(TimeSpan.FromSeconds(9));
        result.WriteConcern.Should().BeSameAs(writeConcern);
        result.HeartbeatInterval.Should().Be(TimeSpan.FromSeconds(11));
        result.HeartbeatTimeout.Should().Be(TimeSpan.FromSeconds(12));
        result.ServerMonitoringMode.Should().Be(ServerMonitoringMode.Poll);
        result.TranslationOptions.CompatibilityLevel.Should().Be(ServerVersion.Server70);
        result.TranslationOptions.EnableClientSideProjections.Should().BeTrue();
    }

    [Fact]
    public void ToMongoClientSettings_should_map_direct_connection_and_load_balanced()
    {
        var direct = MongoClientBuilder.FromEndPoint("host1")
            .Cluster(cluster => cluster.DirectConnection = true)
            .ToMongoClientSettings();
        var loadBalanced = MongoClientBuilder.FromEndPoint("host1")
            .Cluster(cluster => cluster.LoadBalanced = true)
            .ToMongoClientSettings();

        direct.DirectConnection.Should().BeTrue();
        loadBalanced.LoadBalanced.Should().BeTrue();
    }

    [Fact]
    public void ToMongoClientSettings_should_map_srv_options()
    {
        var subject = MongoClientBuilder.FromSrv("cluster0.example.com")
            .Cluster(cluster =>
            {
                cluster.SrvAllowedHostsSuffix = ".example.com";
                cluster.SrvMaxHosts = 2;
                cluster.SrvServiceName = "custom";
            });

        var result = subject.ToMongoClientSettings();

        result.Scheme.Should().Be(ConnectionStringScheme.MongoDBPlusSrv);
        result.Server.Should().Be(new MongoServerAddress("cluster0.example.com", ConnectionString.DefaultSrvPort));
        result.SrvAllowedHostsSuffix.Should().Be(".example.com");
        result.SrvMaxHosts.Should().Be(2);
        result.SrvServiceName.Should().Be("custom");
    }

    [Fact]
    public void ToMongoClientSettings_should_map_ip_end_points()
    {
        var subject = MongoClientBuilder.FromEndPoints([new IPEndPoint(IPAddress.Loopback, 27018)]);

        var result = subject.ToMongoClientSettings();

        result.Servers.Should().Equal(new MongoServerAddress("127.0.0.1", 27018));
    }

    [Fact]
    public void ToMongoClientSettings_should_map_tls()
    {
        RemoteCertificateValidationCallback validationCallback = (_, _, _, _) => true;
        LocalCertificateSelectionCallback selectionCallback = (_, _, _, _, _) => null;
        var subject = MongoClientBuilder.FromEndPoint("host1")
            .Tls(tls =>
            {
                tls.AllowedProtocols = SslProtocols.Tls12;
                tls.CheckCertificateRevocation = true;
                tls.ClientCertificateSelectionCallback = selectionCallback;
                tls.Enabled = true;
                tls.ServerCertificateValidationCallback = validationCallback;
            });

        var result = subject.ToMongoClientSettings();

        result.UseTls.Should().BeTrue();
        result.AllowInsecureTls.Should().BeFalse();
        result.SslSettings.EnabledSslProtocols.Should().Be(SslProtocols.Tls12);
        result.SslSettings.CheckCertificateRevocation.Should().BeTrue();
        result.SslSettings.ClientCertificateSelectionCallback.Should().BeSameAs(selectionCallback);
        result.SslSettings.ServerCertificateValidationCallback.Should().BeSameAs(validationCallback);
    }

    [Fact]
    public void ToMongoClientSettings_should_map_allow_insecure_tls()
    {
        var subject = MongoClientBuilder.FromEndPoint("host1")
            .Tls(tls =>
            {
                tls.Enabled = true;
                tls.AllowInsecure = true;
            });

        var result = subject.ToMongoClientSettings();

        result.UseTls.Should().BeTrue();
        result.AllowInsecureTls.Should().BeTrue();
        result.SslSettings.CheckCertificateRevocation.Should().BeFalse();
    }

    [Fact]
    public void ToMongoClientSettings_should_leave_ssl_settings_null_when_tls_is_disabled()
    {
        var result = MongoClientBuilder.FromEndPoint("host1").ToMongoClientSettings();

        result.UseTls.Should().BeFalse();
        result.SslSettings.Should().BeNull();
    }

    [Fact]
    public void ToMongoClientSettings_should_map_logging_and_tracing()
    {
        var tracing = new TracingOptions();
        var subject = MongoClientBuilder.FromEndPoint("host1")
            .Diagnostics(diagnostics => diagnostics.Tracing = tracing);

        var result = subject.ToMongoClientSettings();

        result.TracingOptions.Should().BeSameAs(tracing);
    }

    [Fact]
    public void ToMongoClientSettings_should_map_auto_encryption()
    {
        var subject = MongoClientBuilder.FromEndPoint("host1")
            .AutoEncryption(autoEncryption =>
            {
                autoEncryption.KeyVaultNamespace = CollectionNamespace.FromFullName("keyvault.datakeys");
                autoEncryption.RegisterKmsProvider("local", new Dictionary<string, object> { { "key", new byte[96] } });
            });

        var result = subject.ToMongoClientSettings();

        result.AutoEncryptionOptions.KeyVaultNamespace.FullName.Should().Be("keyvault.datakeys");
    }

    [Fact]
    public void ToMongoClientSettings_should_throw_when_auto_encryption_is_incomplete()
    {
        var subject = MongoClientBuilder.FromEndPoint("host1")
            .AutoEncryption(autoEncryption => autoEncryption.KeyVaultNamespace = CollectionNamespace.FromFullName("keyvault.datakeys"));

        var exception = Record.Exception(() => subject.ToMongoClientSettings());

        exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void Build_should_deliver_events_to_subscribers()
    {
        var events = new List<ClusterOpeningEvent>();
        var subject = MongoClientBuilder.FromEndPoint("localhost:27017")
            .Diagnostics(diagnostics => diagnostics.Subscribe<ClusterOpeningEvent>(events.Add));

        using var client = subject.Build() as IDisposable;

        events.Should().HaveCount(1);
    }

    [Fact]
    public void Build_should_not_deliver_events_to_subscribers_added_after_the_client_was_built()
    {
        var events = new List<ClusterClosingEvent>();
        var subject = MongoClientBuilder.FromEndPoint("localhost:27017");
        using var client = subject.Build() as IDisposable;

        subject.Diagnostics(diagnostics => diagnostics.Subscribe<ClusterClosingEvent>(events.Add));
        client.Dispose();

        events.Should().BeEmpty();
    }

    [Fact]
    public void GetSubscribers_should_return_a_snapshot()
    {
        var subject = MongoClientBuilder.FromEndPoint("host1")
            .Diagnostics(diagnostics => diagnostics.Subscribe<ClusterOpeningEvent>(_ => { }));
        var snapshot = subject.Diagnostics().GetSubscribers();

        subject.Diagnostics(diagnostics => diagnostics.Subscribe<ClusterClosingEvent>(_ => { }));

        snapshot.Should().HaveCount(1);
        subject.Diagnostics().GetSubscribers().Should().HaveCount(2);
    }

    [Fact]
    public void ToMongoClientSettings_should_configure_cluster_only_when_needed()
    {
        var withoutExtras = MongoClientBuilder.FromEndPoint("host1").ToMongoClientSettings();
        var withMaintenanceInterval = MongoClientBuilder.FromEndPoint("host1")
            .ConnectionPool(pool => pool.MaintenanceInterval = TimeSpan.FromSeconds(10))
            .ToMongoClientSettings();
        var withSocketConfigurator = MongoClientBuilder.FromEndPoint("host1")
            .Network(network => network.SocketConfigurator = _ => { })
            .ToMongoClientSettings();

        withoutExtras.ClusterConfigurator.Should().BeNull();
        withMaintenanceInterval.ClusterConfigurator.Should().NotBeNull();
        withSocketConfigurator.ClusterConfigurator.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void Validate_should_throw_for_invalid_configuration(string expectedMessage, Func<MongoClientBuilder> create)
    {
        var subject = create();

        var exception = Record.Exception(() => subject.Validate());

        exception.Should().BeOfType<InvalidOperationException>();
        exception.Message.Should().Contain(expectedMessage);
    }

    // The rules of each builder are tested on the builder itself, here it is only checked that they are applied
    // along with the rules that span builders.
    public static IEnumerable<object[]> InvalidConfigurations =>
    [
        [
            "MONGODB-X509 authentication mechanism requires TLS",
            () => MongoClientBuilder.FromEndPoint("host1").Authentication(a => a.UseX509Credential())
        ],
        [
            "TLS options are configured, but TLS is not enabled",
            () => MongoClientBuilder.FromEndPoint("host1").Tls(tls => tls.AllowInsecure = true)
        ],
        [
            "Load balanced mode cannot be used with direct connection",
            () => MongoClientBuilder.FromEndPoint("host1").Cluster(cluster =>
            {
                cluster.LoadBalanced = true;
                cluster.DirectConnection = true;
            })
        ],
        [
            "MaxSize must be greater than or equal to MinSize",
            () => MongoClientBuilder.FromEndPoint("host1").ConnectionPool(pool =>
            {
                pool.MaxSize = 5;
                pool.MinSize = 10;
            })
        ]
    ];

    [Fact]
    public void Validate_should_accept_valid_configurations()
    {
        var valid = new[]
        {
            MongoClientBuilder.FromEndPoint("host1"),
            MongoClientBuilder.FromEndPoints(["host1", "host2"]).Cluster(cluster => cluster.ReplicaSetName = "rs"),
            MongoClientBuilder.FromSrv("cluster0.example.com").Cluster(cluster => cluster.SrvMaxHosts = 2),
            MongoClientBuilder.FromEndPoint("host1").Tls(tls => tls.Enabled = true).Authentication(a => a.UseX509Credential())
        };

        foreach (var builder in valid)
        {
            builder.Validate();
        }
    }
}
