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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
using SrvRecord = MongoDB.Driver.Core.Misc.SrvRecord;
using TxtRecord = MongoDB.Driver.Core.Misc.TxtRecord;

namespace MongoDB.Driver.Tests.Specifications.initial_dns_seedlist_discovery.prose_tests;

public class SrvPartsTests
{
    // https://github.com/mongodb/specifications/blob/master/source/initial-dns-seedlist-discovery/tests/README.md#1-allow-srvs-with-fewer-than-3--separated-parts
    [Theory]
    [InlineData("mongodb+srv://localhost")]
    [InlineData("mongodb+srv://mongo.local")]
    public void Allow_srv_with_fewer_than_3_parts(string uri)
    {
        var exception = Record.Exception(() => new ConnectionString(uri));

        exception.Should().BeNull();
    }

    // https://github.com/mongodb/specifications/blob/master/source/initial-dns-seedlist-discovery/tests/README.md#2-throw-when-return-address-does-not-end-with-srv-domain
    [Theory]
    [InlineData("mongodb+srv://localhost", "localhost.mongodb")]
    [InlineData("mongodb+srv://mongo.local", "test_1.evil.local")]
    [InlineData("mongodb+srv://blogs.mongodb.com", "blogs.evil.com")]
    public void Throw_when_return_address_does_not_end_with_srv_domain(string uri, string resolvedHost)
    {
        var connectionString = new ConnectionString(uri, true, MockDnsResolver(resolvedHost, 2090));
        var exception = Record.Exception(() => connectionString.Resolve());

        exception.Should().BeOfType<MongoConfigurationException>().
            Which.Message.Should().Be("Hosts in the SRV record must have the same parent domain as the seed host.");
    }

    // https://github.com/mongodb/specifications/blob/master/source/initial-dns-seedlist-discovery/tests/README.md#3-throw-when-return-address-is-identical-to-srv-hostname
    [Theory]
    [InlineData("mongodb+srv://localhost", "localhost")]
    [InlineData("mongodb+srv://mongo.local", "mongo.local")]
    public void Throw_when_return_address_is_identical_to_srv_hostname(string uri, string resolvedHost)
    {
        var connectionString = new ConnectionString(uri, true, MockDnsResolver(resolvedHost, 2090));
        var exception = Record.Exception(() => connectionString.Resolve());

        exception.Should().BeOfType<MongoConfigurationException>().
            Which.Message.Should().Be("Hosts in the SRV record must have the same parent domain as the seed host.");
    }

    // https://github.com/mongodb/specifications/blob/master/source/initial-dns-seedlist-discovery/tests/README.md#4-throw-when-return-address-does-not-contain--separating-shared-part-of-domain
    [Theory]
    [InlineData("mongodb+srv://localhost", "test_1.cluster_1localhost")]
    [InlineData("mongodb+srv://mongo.local", "test_1.my_hostmongo.local")]
    [InlineData("mongodb+srv://blogs.mongodb.com", "cluster.testmongodb.com")]
    public void Throw_when_return_address_does_not_contain_dot_separating_shared_part_of_domain(string uri, string resolvedHost)
    {
        var connectionString = new ConnectionString(uri, true, MockDnsResolver(resolvedHost, 2090));
        var exception = Record.Exception(() => connectionString.Resolve());

        exception.Should().BeOfType<MongoConfigurationException>().
            Which.Message.Should().Be("Hosts in the SRV record must have the same parent domain as the seed host.");
    }

    // https://github.com/mongodb/specifications/blob/master/source/initial-dns-seedlist-discovery/tests/README.md#10-accept-a-mixed-case-returned-address-with-srvallowedhostssuffix
    [Fact]
    public void Accept_mixed_case_return_address_with_srvAllowedHostsSuffix()
    {
        var uri = "mongodb+srv://blogs.mongodb.com/?srvAllowedHostsSuffix=.mongodb.com";
        var connectionString = new ConnectionString(uri, true, MockDnsResolver("CLUSTER.MONGODB.COM.", 2090));

        var resolved = connectionString.Resolve();

        resolved.Hosts.Should().ContainSingle()
            .Which.Should().BeOfType<DnsEndPoint>()
            .Subject.Host.Should().Be("cluster.mongodb.com");
    }

    // https://github.com/mongodb/specifications/blob/master/source/initial-dns-seedlist-discovery/tests/README.md#12-accept-a-reserved-single-label-as-srvallowedhostssuffix
    [Fact]
    public void Accept_reserved_single_label_srvAllowedHostsSuffix()
    {
        var uri = "mongodb+srv://cluster.localhost/?srvAllowedHostsSuffix=localhost";
        var connectionString = new ConnectionString(uri, true, MockDnsResolver("db.cluster.localhost", 2090));

        var resolved = connectionString.Resolve();

        resolved.Hosts.Should().ContainSingle()
            .Which.Should().BeOfType<DnsEndPoint>()
            .Subject.Host.Should().Be("db.cluster.localhost");
    }

    // https://github.com/mongodb/specifications/blob/master/source/initial-dns-seedlist-discovery/tests/README.md#15-accept-an-underscore-in-srvallowedhostssuffix
    [Fact]
    public void Accept_underscore_in_srvAllowedHostsSuffix()
    {
        var uri = "mongodb+srv://blogs.my_domain.net/?srvAllowedHostsSuffix=.my_domain.net";
        var connectionString = new ConnectionString(uri, true, MockDnsResolver("cluster.my_domain.net", 2090));

        var resolved = connectionString.Resolve();

        resolved.Hosts.Should().ContainSingle()
            .Which.Should().BeOfType<DnsEndPoint>()
            .Subject.Host.Should().Be("cluster.my_domain.net");
    }

    [Trait("Category", "Integration")]
    [Theory]
    [InlineData("mongodb+srv://test1.test.build.10gen.cc", "localhost.test.build.10gen.cc", true, true)]
    [InlineData("mongodb+srv://test1.test.build.10gen.cc", "localhost.test.build.10gen.cc", false, true)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "[localhost.test.build.10gen.cc].mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "[localhost.test.build.10gen.cc].mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc/mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc/mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc/.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc/.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc#.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc#.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc?.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc?.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc\\.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc\\.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc:27017.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc:27017.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc,x.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc,x.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc\0.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com", "localhost.test.build.10gen.cc\0.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "[localhost.test.build.10gen.cc].mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "[localhost.test.build.10gen.cc].mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc/mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc/mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc/.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc/.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc#.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc#.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc?.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc?.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc\\.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc\\.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc:27017.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc:27017.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc,x.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc,x.mongodb.com", false, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc\0.mongodb.com", true, false)]
    [InlineData("mongodb+srv://blogs.mongodb.com?srvAllowedHostsSuffix=.mongodb.com", "localhost.test.build.10gen.cc\0.mongodb.com", false, false)]
    public async Task Do_not_connect_host_outside_SRV_domain_through_crafted_returned_address(string connectionString, string resolvedHost, bool async, bool shouldSucceed)
    {
        RequireServer.Check().Tls();

        var originalDnsResolver = DnsClientWrapper.Instance;
        // TODO: this is a workaround to set the IDnsResolver, we should implement a way to set services per mongoClient instead.
        Reflector.SetStaticFieldValue(typeof(DnsClientWrapper), "__instance", MockDnsResolver(resolvedHost, 27017));
        try
        {
            var url = new MongoUrl(connectionString);

            var eventCapturer = new EventCapturer().Capture<ServerHeartbeatSucceededEvent>();
            var mongoClientSettings = MongoClientSettings.FromUrl(url);
            mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(2000);
            mongoClientSettings.AllowInsecureTls = true;
            mongoClientSettings.MinConnectionPoolSize = 0;
            mongoClientSettings.ClusterConfigurator = cluster =>
            {
                cluster.Subscribe(eventCapturer);
            };

            using var mongoClient = DriverTestConfiguration.CreateMongoClient(mongoClientSettings);
            var db = mongoClient.GetDatabase("admin");
            var pingException = await Record.ExceptionAsync(async () =>
                _ = async ? await db.RunCommandAsync<BsonDocument>("{ ping : 1 }") : db.RunCommand<BsonDocument>("{ ping : 1 }"));

            if (shouldSucceed)
            {
                pingException.Should().BeNull();
            }
            else
            {
                eventCapturer.Events.Should().BeEmpty();
                pingException.Should().NotBeNull();
            }

        }
        finally
        {
            Reflector.SetStaticFieldValue(typeof(DnsClientWrapper), "__instance", originalDnsResolver);
        }
    }

    private IDnsResolver MockDnsResolver(string dnsEndPointString, int port)
    {
        var dnsResolverMock = new Mock<IDnsResolver>();
        var srvResponse = new List<SrvRecord> { new SrvRecord(new DnsEndPoint(dnsEndPointString, port), TimeSpan.MaxValue) };
        var txtResponse = new List<TxtRecord>();

        dnsResolverMock.Setup(resolver => resolver.ResolveSrvRecords(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(srvResponse);
        dnsResolverMock.Setup(resolver => resolver.ResolveSrvRecordsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(srvResponse);

        dnsResolverMock.Setup(resolver => resolver.ResolveTxtRecords(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(txtResponse);
        dnsResolverMock.Setup(resolver => resolver.ResolveTxtRecordsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(txtResponse);

        return dnsResolverMock.Object;
    }
}
