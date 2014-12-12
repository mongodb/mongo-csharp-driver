/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver.Communication;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Communication.Security
{
    [TestFixture]
    public class ClusterKeyTests
    {
        [Test]
        public void Constructor_with_MongoClientSettings_parameter_should_copy_relevant_values()
        {
            var credentials = new[] { MongoCredential.CreateMongoCRCredential("source", "username", "password") };
            var servers = new[] { new MongoServerAddress("localhost") };

            var sslSettings = new SslSettings
            {
                CheckCertificateRevocation = true,
                EnabledSslProtocols = SslProtocols.Ssl3
            };

            var clientSettings = new MongoClientSettings
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectTimeout = TimeSpan.FromSeconds(1),
                Credentials = credentials,
                GuidRepresentation = GuidRepresentation.Standard,
                IPv6 = true,
                MaxConnectionIdleTime = TimeSpan.FromSeconds(2),
                MaxConnectionLifeTime = TimeSpan.FromSeconds(3),
                MaxConnectionPoolSize = 10,
                MinConnectionPoolSize = 5,
                ReplicaSetName = "rs",
                SecondaryAcceptableLatency = TimeSpan.FromMilliseconds(20),
                Servers = servers,
                SocketTimeout = TimeSpan.FromSeconds(4),
                SslSettings = sslSettings,
                UseSsl = true,
                VerifySslCertificate = true,
                WaitQueueSize = 20,
                WaitQueueTimeout = TimeSpan.FromSeconds(5)
            };

            var subject = new ClusterKey(clientSettings);

            subject.ConnectionMode.Should().Be(clientSettings.ConnectionMode);
            subject.ConnectTimeout.Should().Be(clientSettings.ConnectTimeout);
            subject.Credentials.Should().Equal(clientSettings.Credentials);
            subject.IPv6.Should().Be(clientSettings.IPv6);
            subject.MaxConnectionIdleTime.Should().Be(clientSettings.MaxConnectionIdleTime);
            subject.MaxConnectionLifeTime.Should().Be(clientSettings.MaxConnectionLifeTime);
            subject.MaxConnectionPoolSize.Should().Be(clientSettings.MaxConnectionPoolSize);
            subject.MinConnectionPoolSize.Should().Be(clientSettings.MinConnectionPoolSize);
            subject.ReplicaSetName.Should().Be(clientSettings.ReplicaSetName);
            subject.SecondaryAcceptableLatency.Should().Be(clientSettings.SecondaryAcceptableLatency);
            subject.Servers.Should().Equal(clientSettings.Servers);
            subject.SocketTimeout.Should().Be(clientSettings.SocketTimeout);
            subject.SslSettings.Should().Be(clientSettings.SslSettings);
            subject.UseSsl.Should().Be(clientSettings.UseSsl);
            subject.VerifySslCertificate.Should().Be(clientSettings.VerifySslCertificate);
            subject.WaitQueueSize.Should().Be(clientSettings.WaitQueueSize);
            subject.WaitQueueTimeout.Should().Be(clientSettings.WaitQueueTimeout);
        }

        [Test]
        public void Constructor_with_MongoServerSettings_parameter_should_copy_relevant_values()
        {
            var credentials = new[] { MongoCredential.CreateMongoCRCredential("source", "username", "password") };
            var servers = new[] { new MongoServerAddress("localhost") };

            var sslSettings = new SslSettings
            {
                CheckCertificateRevocation = true,
                EnabledSslProtocols = SslProtocols.Ssl3
            };

            var serverSettings = new MongoServerSettings
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectTimeout = TimeSpan.FromSeconds(1),
                Credentials = credentials,
                GuidRepresentation = GuidRepresentation.Standard,
                IPv6 = true,
                MaxConnectionIdleTime = TimeSpan.FromSeconds(2),
                MaxConnectionLifeTime = TimeSpan.FromSeconds(3),
                MaxConnectionPoolSize = 10,
                MinConnectionPoolSize = 5,
                ReplicaSetName = "rs",
                SecondaryAcceptableLatency = TimeSpan.FromMilliseconds(20),
                Servers = servers,
                SocketTimeout = TimeSpan.FromSeconds(4),
                SslSettings = sslSettings,
                UseSsl = true,
                VerifySslCertificate = true,
                WaitQueueSize = 20,
                WaitQueueTimeout = TimeSpan.FromSeconds(5)
            };

            var subject = new ClusterKey(serverSettings);

            subject.ConnectionMode.Should().Be(serverSettings.ConnectionMode);
            subject.ConnectTimeout.Should().Be(serverSettings.ConnectTimeout);
            subject.Credentials.Should().Equal(serverSettings.Credentials);
            subject.IPv6.Should().Be(serverSettings.IPv6);
            subject.MaxConnectionIdleTime.Should().Be(serverSettings.MaxConnectionIdleTime);
            subject.MaxConnectionLifeTime.Should().Be(serverSettings.MaxConnectionLifeTime);
            subject.MaxConnectionPoolSize.Should().Be(serverSettings.MaxConnectionPoolSize);
            subject.MinConnectionPoolSize.Should().Be(serverSettings.MinConnectionPoolSize);
            subject.ReplicaSetName.Should().Be(serverSettings.ReplicaSetName);
            subject.SecondaryAcceptableLatency.Should().Be(serverSettings.SecondaryAcceptableLatency);
            subject.Servers.Should().Equal(serverSettings.Servers);
            subject.SocketTimeout.Should().Be(serverSettings.SocketTimeout);
            subject.SslSettings.Should().Be(serverSettings.SslSettings);
            subject.UseSsl.Should().Be(serverSettings.UseSsl);
            subject.VerifySslCertificate.Should().Be(serverSettings.VerifySslCertificate);
            subject.WaitQueueSize.Should().Be(serverSettings.WaitQueueSize);
            subject.WaitQueueTimeout.Should().Be(serverSettings.WaitQueueTimeout);
        }

        [Test]
        public void Equals_should_return_true_if_all_fields_are_equal()
        {
            var subject1 = CreateSubject();
            var subject2 = CreateSubject();
            subject1.Should().NotBeSameAs(subject2);
            subject1.Equals(subject2).Should().BeTrue();
            subject1.GetHashCode().Should().Be(subject2.GetHashCode());
        }

        [TestCase("ConnectionMode", true)]
        [TestCase("ConnectTimeout", true)]
        [TestCase("Credentials", false)]
        [TestCase("IPv6", true)]
        [TestCase("MaxConnectionIdleTime", true)]
        [TestCase("MaxConnectionLifeTime", true)]
        [TestCase("MaxConnectionPoolSize", true)]
        [TestCase("MinConnectionPoolSize", true)]
        [TestCase("ReplicaSetName", true)]
        [TestCase("SecondaryAcceptableLatency", true)]
        [TestCase("Servers", false)]
        [TestCase("SocketTimeout", true)]
        [TestCase("SslSettings", true)]
        [TestCase("UseSsl", true)]
        [TestCase("VerifySslCertificate", true)]
        [TestCase("WaitQueueSize", true)]
        [TestCase("WaitQueueTimeout", true)]
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
            var connectionMode = ConnectionMode.Direct;
            var connectTimeout = TimeSpan.FromSeconds(1);
            var credentials = new[] { MongoCredential.CreateMongoCRCredential("source", "username", "password") };
            var guidRepresentation = GuidRepresentation.Standard;
            var ipv6 = false;
            var maxConnectionIdleTime = TimeSpan.FromSeconds(2);
            var maxConnectionLifeTime = TimeSpan.FromSeconds(3);
            var maxConnectionPoolSize = 50;
            var minConnectionPoolSize = 5;
            var replicaSetName = "abc";
            var secondaryAcceptableLatency = TimeSpan.FromMilliseconds(20);
            var servers = new[] { new MongoServerAddress("localhost") };
            var socketTimeout = TimeSpan.FromSeconds(4);
            var sslSettings = new SslSettings
            {
                CheckCertificateRevocation = true,
                EnabledSslProtocols = SslProtocols.Ssl3
            };
            var useSsl = false;
            var verifySslCertificate = false;
            var waitQueueSize = 20;
            var waitQueueTimeout = TimeSpan.FromSeconds(5);

            switch (notEqualFieldName)
            {
                case "ConnectionMode": connectionMode = ConnectionMode.ReplicaSet; break;
                case "ConnectTimeout": connectTimeout = TimeSpan.FromSeconds(99); break;
                case "Credentials": credentials = new[] { MongoCredential.CreateMongoCRCredential("different", "different", "different") }; break;
                case "IPv6": ipv6 = !ipv6; break;
                case "MaxConnectionIdleTime": maxConnectionIdleTime = TimeSpan.FromSeconds(99); break;
                case "MaxConnectionLifeTime": maxConnectionLifeTime = TimeSpan.FromSeconds(99); break;
                case "MaxConnectionPoolSize": maxConnectionPoolSize = 99; break;
                case "MinConnectionPoolSize": minConnectionPoolSize = 99; break;
                case "ReplicaSetName": replicaSetName = "different"; break;
                case "SecondaryAcceptableLatency": secondaryAcceptableLatency = TimeSpan.FromMilliseconds(99); break;
                case "Servers": servers = new[] { new MongoServerAddress("different") }; break;
                case "SocketTimeout": socketTimeout = TimeSpan.FromSeconds(99); break;
                case "SslSettings": sslSettings.CheckCertificateRevocation = !sslSettings.CheckCertificateRevocation; break;
                case "UseSsl": useSsl = !useSsl; break;
                case "VerifySslCertificate": verifySslCertificate = !verifySslCertificate; break;
                case "WaitQueueSize": waitQueueSize = 99; break;
                case "WaitQueueTimeout": waitQueueTimeout = TimeSpan.FromSeconds(99); break;
            }

            var clientSettings = new MongoClientSettings
            {
                ConnectionMode = connectionMode,
                ConnectTimeout = connectTimeout,
                Credentials = credentials,
                GuidRepresentation = guidRepresentation,
                IPv6 = ipv6,
                MaxConnectionIdleTime = maxConnectionIdleTime,
                MaxConnectionLifeTime = maxConnectionLifeTime,
                MaxConnectionPoolSize = maxConnectionPoolSize,
                MinConnectionPoolSize = minConnectionPoolSize,
                ReplicaSetName = replicaSetName,
                SecondaryAcceptableLatency = secondaryAcceptableLatency,
                Servers = servers,
                SocketTimeout = socketTimeout,
                SslSettings = sslSettings,
                UseSsl = useSsl,
                VerifySslCertificate = verifySslCertificate,
                WaitQueueSize = waitQueueSize,
                WaitQueueTimeout = waitQueueTimeout
            };

            return new ClusterKey(clientSettings);
        }
    }
}