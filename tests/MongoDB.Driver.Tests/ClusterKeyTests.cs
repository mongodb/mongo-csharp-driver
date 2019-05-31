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
using System.Linq;
using System.Security.Authentication;
using FluentAssertions;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClusterKeyTests
    {
        [Fact]
        public void Equals_should_return_true_if_all_fields_are_equal()
        {
            var subject1 = CreateSubject();
            var subject2 = CreateSubject();
            subject1.Should().NotBeSameAs(subject2);
            subject1.Equals(subject2).Should().BeTrue();
            subject1.GetHashCode().Should().Be(subject2.GetHashCode());
        }

        [Theory]
        [InlineData("ApplicationName", true)]
        [InlineData("ClusterConfigurator", true)]
        [InlineData("Compressors", true)]
        [InlineData("ConnectionMode", true)]
        [InlineData("ConnectTimeout", true)]
        [InlineData("Credentials", false)]
        [InlineData("HeartbeatInterval", true)]
        [InlineData("HeartbeatTimeout", true)]
        [InlineData("IPv6", true)]
        [InlineData("MaxConnectionIdleTime", true)]
        [InlineData("MaxConnectionLifeTime", true)]
        [InlineData("MaxConnectionPoolSize", true)]
        [InlineData("MinConnectionPoolSize", true)]
        [InlineData("ReceiveBufferSize", true)]
        [InlineData("ReplicaSetName", true)]
        [InlineData("LocalThreshold", true)]
        [InlineData("Scheme", true)]
        [InlineData("SdamLogFileName", true)]
        [InlineData("SendBufferSize", true)]
        [InlineData("Servers", false)]
        [InlineData("ServerSelectionTimeout", true)]
        [InlineData("SocketTimeout", true)]
        [InlineData("SslSettings", true)]
        [InlineData("UseSsl", true)]
        [InlineData("VerifySslCertificate", true)]
        [InlineData("WaitQueueSize", true)]
        [InlineData("WaitQueueTimeout", true)]
        public void Equals_should_return_false_if_any_field_is_not_equal(string notEqualFieldName, bool expectEqualHashCode)
        {
            var subject1 = CreateSubject();
            var subject2 = CreateSubject(notEqualFieldName);
            subject1.Should().NotBeSameAs(subject2);
            subject1.Equals(subject2).Should().BeFalse();
            subject1.GetHashCode().Equals(subject2.GetHashCode()).Should().Be(expectEqualHashCode);
        }

        private ClusterKey CreateSubject(string notEqualFieldName = null)
        {
            var applicationName = "app1";
            var clusterConfigurator = new Action<ClusterBuilder>(b => { });
            var compressors = new CompressorConfiguration[0];
            var connectionMode = ConnectionMode.Direct;
            var connectTimeout = TimeSpan.FromSeconds(1);
#pragma warning disable 618
            var credentials = new List<MongoCredential> { MongoCredential.CreateMongoCRCredential("source", "username", "password") };
#pragma warning restore 618
            var heartbeatInterval = TimeSpan.FromSeconds(7);
            var heartbeatTimeout = TimeSpan.FromSeconds(8);
            var ipv6 = false;
            var localThreshold = TimeSpan.FromMilliseconds(20);
            var maxConnectionIdleTime = TimeSpan.FromSeconds(2);
            var maxConnectionLifeTime = TimeSpan.FromSeconds(3);
            var maxConnectionPoolSize = 50;
            var minConnectionPoolSize = 5;
            var receiveBufferSize = 1;
            var replicaSetName = "abc";
            var scheme = ConnectionStringScheme.MongoDB;
            var sdamLogFileName = "stdout";
            var sendBufferSize = 1;
            var servers = new[] { new MongoServerAddress("localhost") };
            var serverSelectionTimeout = TimeSpan.FromSeconds(6);
            var socketTimeout = TimeSpan.FromSeconds(4);
            var sslSettings = new SslSettings
            {
                CheckCertificateRevocation = true,
                EnabledSslProtocols = SslProtocols.Tls
            };
            var useSsl = false;
            var verifySslCertificate = false;
            var waitQueueSize = 20;
            var waitQueueTimeout = TimeSpan.FromSeconds(5);

            if (notEqualFieldName != null)
            {
                switch (notEqualFieldName)
                {
                    case "ApplicationName": applicationName = "app2"; break;
                    case "ClusterConfigurator": clusterConfigurator = new Action<ClusterBuilder>(b => { }); break;
                    case "Compressors": compressors = new[] { new CompressorConfiguration(CompressorType.Zlib) }; break;
                    case "ConnectionMode": connectionMode = ConnectionMode.ReplicaSet; break;
                    case "ConnectTimeout": connectTimeout = TimeSpan.FromSeconds(99); break;
#pragma warning disable 618
                    case "Credentials": credentials = new List<MongoCredential> { MongoCredential.CreateMongoCRCredential("different", "different", "different") }; break;
#pragma warning restore 618
                    case "HeartbeatInterval": heartbeatInterval = TimeSpan.FromSeconds(99); break;
                    case "HeartbeatTimeout": heartbeatTimeout = TimeSpan.FromSeconds(99); break;
                    case "IPv6": ipv6 = !ipv6; break;
                    case "LocalThreshold": localThreshold = TimeSpan.FromMilliseconds(99); break;
                    case "MaxConnectionIdleTime": maxConnectionIdleTime = TimeSpan.FromSeconds(99); break;
                    case "MaxConnectionLifeTime": maxConnectionLifeTime = TimeSpan.FromSeconds(99); break;
                    case "MaxConnectionPoolSize": maxConnectionPoolSize = 99; break;
                    case "MinConnectionPoolSize": minConnectionPoolSize = 99; break;
                    case "ReceiveBufferSize": receiveBufferSize = 2; break;
                    case "ReplicaSetName": replicaSetName = "different"; break;
                    case "Scheme": scheme = ConnectionStringScheme.MongoDBPlusSrv; break;
                    case "SdamLogFileName": sdamLogFileName = "different"; break;
                    case "SendBufferSize": sendBufferSize = 2; break;
                    case "Servers": servers = new[] { new MongoServerAddress("different") }; break;
                    case "ServerSelectionTimeout": serverSelectionTimeout = TimeSpan.FromSeconds(98); break;
                    case "SocketTimeout": socketTimeout = TimeSpan.FromSeconds(99); break;
                    case "SslSettings": sslSettings.CheckCertificateRevocation = !sslSettings.CheckCertificateRevocation; break;
                    case "UseSsl": useSsl = !useSsl; break;
                    case "VerifySslCertificate": verifySslCertificate = !verifySslCertificate; break;
                    case "WaitQueueSize": waitQueueSize = 99; break;
                    case "WaitQueueTimeout": waitQueueTimeout = TimeSpan.FromSeconds(99); break;
                    default: throw new ArgumentException($"Invalid field name: \"{notEqualFieldName}\".", nameof(notEqualFieldName));
                }
            }

            return new ClusterKey(
                applicationName,
                clusterConfigurator,
                compressors,
                connectionMode,
                connectTimeout,
                credentials,
                heartbeatInterval,
                heartbeatTimeout,
                ipv6,
                localThreshold,
                maxConnectionIdleTime,
                maxConnectionLifeTime,
                maxConnectionPoolSize,
                minConnectionPoolSize,
                receiveBufferSize,
                replicaSetName,
                scheme,
                sdamLogFileName,
                sendBufferSize,
                servers,
                serverSelectionTimeout,
                socketTimeout,
                sslSettings,
                useSsl,
                verifySslCertificate,
                waitQueueSize,
                waitQueueTimeout);
        }
    }
}