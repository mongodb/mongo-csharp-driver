/* Copyright 2010-2016 MongoDB Inc.
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
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
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
        [InlineData("ReplicaSetName", true)]
        [InlineData("LocalThreshold", true)]
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
            var connectionMode = ConnectionMode.Direct;
            var connectTimeout = TimeSpan.FromSeconds(1);
            var credentials = new[] { MongoCredential.CreateMongoCRCredential("source", "username", "password") };
            var guidRepresentation = GuidRepresentation.Standard;
            var heartbeatInterval = TimeSpan.FromSeconds(7);
            var heartbeatTimeout = TimeSpan.FromSeconds(8);
            var ipv6 = false;
            var localThreshold = TimeSpan.FromMilliseconds(20);
            var maxConnectionIdleTime = TimeSpan.FromSeconds(2);
            var maxConnectionLifeTime = TimeSpan.FromSeconds(3);
            var maxConnectionPoolSize = 50;
            var minConnectionPoolSize = 5;
            var replicaSetName = "abc";
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

            switch (notEqualFieldName)
            {
                case "ApplicationName": applicationName = "app2"; break;
                case "ConnectionMode": connectionMode = ConnectionMode.ReplicaSet; break;
                case "ConnectTimeout": connectTimeout = TimeSpan.FromSeconds(99); break;
                case "Credentials": credentials = new[] { MongoCredential.CreateMongoCRCredential("different", "different", "different") }; break;
                case "HeartbeatInterval": heartbeatInterval = TimeSpan.FromSeconds(99); break;
                case "HeartbeatTimeout": heartbeatTimeout = TimeSpan.FromSeconds(99); break;
                case "IPv6": ipv6 = !ipv6; break;
                case "LocalThreshold": localThreshold = TimeSpan.FromMilliseconds(99); break;
                case "MaxConnectionIdleTime": maxConnectionIdleTime = TimeSpan.FromSeconds(99); break;
                case "MaxConnectionLifeTime": maxConnectionLifeTime = TimeSpan.FromSeconds(99); break;
                case "MaxConnectionPoolSize": maxConnectionPoolSize = 99; break;
                case "MinConnectionPoolSize": minConnectionPoolSize = 99; break;
                case "ReplicaSetName": replicaSetName = "different"; break;
                case "Servers": servers = new[] { new MongoServerAddress("different") }; break;
                case "ServerSelectionTimeout": serverSelectionTimeout = TimeSpan.FromSeconds(98); break;
                case "SocketTimeout": socketTimeout = TimeSpan.FromSeconds(99); break;
                case "SslSettings": sslSettings.CheckCertificateRevocation = !sslSettings.CheckCertificateRevocation; break;
                case "UseSsl": useSsl = !useSsl; break;
                case "VerifySslCertificate": verifySslCertificate = !verifySslCertificate; break;
                case "WaitQueueSize": waitQueueSize = 99; break;
                case "WaitQueueTimeout": waitQueueTimeout = TimeSpan.FromSeconds(99); break;
            }

            var clientSettings = new MongoClientSettings
            {
                ApplicationName = applicationName,
                ConnectionMode = connectionMode,
                ConnectTimeout = connectTimeout,
                Credentials = credentials,
                GuidRepresentation = guidRepresentation,
                HeartbeatInterval = heartbeatInterval,
                HeartbeatTimeout = heartbeatTimeout,
                IPv6 = ipv6,
                MaxConnectionIdleTime = maxConnectionIdleTime,
                MaxConnectionLifeTime = maxConnectionLifeTime,
                MaxConnectionPoolSize = maxConnectionPoolSize,
                MinConnectionPoolSize = minConnectionPoolSize,
                ReplicaSetName = replicaSetName,
                LocalThreshold = localThreshold,
                Servers = servers,
                ServerSelectionTimeout = serverSelectionTimeout,
                SocketTimeout = socketTimeout,
                SslSettings = sslSettings,
                UseSsl = useSsl,
                VerifySslCertificate = verifySslCertificate,
                WaitQueueSize = waitQueueSize,
                WaitQueueTimeout = waitQueueTimeout
            };

            return clientSettings.ToClusterKey();
        }
    }
}