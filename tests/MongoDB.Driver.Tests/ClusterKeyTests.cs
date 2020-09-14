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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClusterKeyTests
    {
        private const string Key1 = "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";
        private const string Key2 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

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
        [InlineData("AllowInsecureTls", true)]
        [InlineData("ApplicationName", true)]
        [InlineData("ClusterConfigurator", true)]
        [InlineData("Compressors", true)]
        [InlineData("ConnectionMode", true)]
        [InlineData("ConnectTimeout", true)]
        [InlineData("Credentials", false)]
        [InlineData("DirectConnection", true)]
        [InlineData("HeartbeatInterval", true)]
        [InlineData("HeartbeatTimeout", true)]
        [InlineData("IPv6", true)]
        [InlineData("KmsProviders", true)]
        [InlineData("MaxConnectionIdleTime", true)]
        [InlineData("MaxConnectionLifeTime", true)]
        [InlineData("MaxConnectionPoolSize", true)]
        [InlineData("MinConnectionPoolSize", true)]
        [InlineData("ReceiveBufferSize", true)]
        [InlineData("ReplicaSetName", true)]
        [InlineData("LocalThreshold", true)]
        [InlineData("SchemaMap", true)]
        [InlineData("Scheme", true)]
        [InlineData("SdamLogFileName", true)]
        [InlineData("SendBufferSize", true)]
        [InlineData("Servers", false)]
        [InlineData("ServerSelectionTimeout", true)]
        [InlineData("SocketTimeout", true)]
        [InlineData("SslSettings", true)]
        [InlineData("UseTls", true)]
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

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public void Equals_should_return_true_if_kms_providers_have_different_records_count(
            bool skipTheLastMainRecord,
            bool skipTheLastNestedRecord)
        {
            var kmsProvider1 = GetKmsProviders();
            var kmsProvider2 = GetKmsProviders(skipTheLastMainRecord: skipTheLastMainRecord, skipTheLastNestedRecord: skipTheLastNestedRecord);

            var subject1 = CreateSubjectWith(kmsProvidersValue: kmsProvider1);
            var subject2 = CreateSubjectWith(kmsProvidersValue: kmsProvider2);
            subject1.Should().NotBe(subject2);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void Equals_should_return_true_if_kms_providers_have_the_same_items_but_with_different_order(
            bool withReverseInMainKeys,
            bool withReverseInNestedKeys)
        {
            var kmsProviders1 = GetKmsProviders();
            var kmsProviders2 = GetKmsProviders(withReverseInMainKeys: withReverseInMainKeys, withReverseInNestedKeys: withReverseInNestedKeys);

            var subject1 = CreateSubjectWith(kmsProvidersValue: kmsProviders1);
            var subject2 = CreateSubjectWith(kmsProvidersValue: kmsProviders2);
            subject1.Should().Be(subject2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_return_true_if_schema_maps_have_the_same_items_but_with_different_order(
            [Values(false, true)] bool withReverse)
        {
            var schemaMap1 = GetSchemaMaps();
            var schemaMap2 = GetSchemaMaps(withReverse: withReverse);

            var subject1 = CreateSubjectWith(schemaMapValue: schemaMap1);
            var subject2 = CreateSubjectWith(schemaMapValue: schemaMap2);
            subject1.Should().Be(subject2);
        }

        [Theory]
#pragma warning disable CS0618 // Type or member is obsolete
        [InlineData(ConnectionModeSwitch.NotSet, "directConnection", false)]
        [InlineData(ConnectionModeSwitch.NotSet, "connect", false)]
        [InlineData(ConnectionModeSwitch.UseConnectionMode, "directConnection", true)]
        [InlineData(ConnectionModeSwitch.UseConnectionMode, "connect", false)]
        [InlineData(ConnectionModeSwitch.UseDirectConnection, "directConnection", false)]
        [InlineData(ConnectionModeSwitch.UseDirectConnection, "connect", true)]
        public void Property_getter_shoud_throw_when_connectionModeSwitch_is_unexpected(ConnectionModeSwitch connectionModeSwitch, string property, bool shouldFail)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var subject = CreateSubjectWith(connectionModeSwitch: connectionModeSwitch);

            Exception exception;
            switch (property)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                case "connect": exception = Record.Exception(() => subject.ConnectionMode); break;
#pragma warning restore CS0618 // Type or member is obsolete
                case "directConnection": exception = Record.Exception(() => subject.DirectConnection); break;
                default: throw new Exception($"Unexpected property {property}.");
            }

            if (shouldFail)
            {
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        // private methods
        private ClusterKey CreateSubject(string notEqualFieldName = null)
        {
            var allowInsecureTls = true;
            var applicationName = "app1";
            var clusterConfigurator = new Action<ClusterBuilder>(b => { });
#pragma warning disable CS0618 // Type or member is obsolete
            var connectionModeSwitch = ConnectionModeSwitch.UseConnectionMode;
#pragma warning restore CS0618 // Type or member is obsolete
            var compressors = new CompressorConfiguration[0];
#pragma warning disable CS0618 // Type or member is obsolete
            var connectionMode = ConnectionMode.Direct;
#pragma warning restore CS0618 // Type or member is obsolete
            var connectTimeout = TimeSpan.FromSeconds(1);
#pragma warning disable 618
            var credentials = new List<MongoCredential> { MongoCredential.CreateMongoCRCredential("source", "username", "password") };
#pragma warning restore 618
            bool? directConnection = null;
            var heartbeatInterval = TimeSpan.FromSeconds(7);
            var heartbeatTimeout = TimeSpan.FromSeconds(8);
            var ipv6 = false;
            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var localThreshold = TimeSpan.FromMilliseconds(20);
            var maxConnectionIdleTime = TimeSpan.FromSeconds(2);
            var maxConnectionLifeTime = TimeSpan.FromSeconds(3);
            var maxConnectionPoolSize = 50;
            var minConnectionPoolSize = 5;
            var receiveBufferSize = 1;
            var replicaSetName = "abc";
            var schemaMap = new Dictionary<string, BsonDocument>();
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
            var useTls = false;
            var waitQueueSize = 20;
            var waitQueueTimeout = TimeSpan.FromSeconds(5);

            if (notEqualFieldName != null)
            {
                switch (notEqualFieldName)
                {
                    case "AllowInsecureTls": allowInsecureTls = !allowInsecureTls; break;
                    case "ApplicationName": applicationName = "app2"; break;
                    case "ClusterConfigurator": clusterConfigurator = new Action<ClusterBuilder>(b => { }); break;
                    case "Compressors": compressors = new[] { new CompressorConfiguration(CompressorType.Zlib) }; break;
#pragma warning disable CS0618 // Type or member is obsolete
                    case "ConnectionMode":
                        {
                            connectionMode = ConnectionMode.ReplicaSet;
                            connectionModeSwitch = ConnectionModeSwitch.UseConnectionMode;
                            directConnection = null; // reset
                        } break;
#pragma warning restore CS0618 // Type or member is obsolete
                    case "ConnectTimeout": connectTimeout = TimeSpan.FromSeconds(99); break;
#pragma warning disable CS0618 // Type or member is obsolete
                    case "Credentials": credentials = new List<MongoCredential> { MongoCredential.CreateMongoCRCredential("different", "different", "different") }; break;
                    case "DirectConnection":
                        {
                            directConnection = true;
                            connectionModeSwitch = ConnectionModeSwitch.UseDirectConnection;
                            connectionMode = ConnectionMode.Automatic; // reset
#pragma warning restore CS0618 // Type or member is obsolete
                        }
                        break;
                    case "HeartbeatInterval": heartbeatInterval = TimeSpan.FromSeconds(99); break;
                    case "HeartbeatTimeout": heartbeatTimeout = TimeSpan.FromSeconds(99); break;
                    case "IPv6": ipv6 = !ipv6; break;
                    case "KmsProviders": kmsProviders.Add("local", new Dictionary<string, object>() { { "key", "test" } }); break;
                    case "LocalThreshold": localThreshold = TimeSpan.FromMilliseconds(99); break;
                    case "MaxConnectionIdleTime": maxConnectionIdleTime = TimeSpan.FromSeconds(99); break;
                    case "MaxConnectionLifeTime": maxConnectionLifeTime = TimeSpan.FromSeconds(99); break;
                    case "MaxConnectionPoolSize": maxConnectionPoolSize = 99; break;
                    case "MinConnectionPoolSize": minConnectionPoolSize = 99; break;
                    case "ReceiveBufferSize": receiveBufferSize = 2; break;
                    case "ReplicaSetName": replicaSetName = "different"; break;
                    case "SchemaMap": schemaMap.Add("db.coll", new BsonDocument()); break;
                    case "Scheme": scheme = ConnectionStringScheme.MongoDBPlusSrv; break;
                    case "SdamLogFileName": sdamLogFileName = "different"; break;
                    case "SendBufferSize": sendBufferSize = 2; break;
                    case "Servers": servers = new[] { new MongoServerAddress("different") }; break;
                    case "ServerSelectionTimeout": serverSelectionTimeout = TimeSpan.FromSeconds(98); break;
                    case "SocketTimeout": socketTimeout = TimeSpan.FromSeconds(99); break;
                    case "SslSettings": sslSettings.CheckCertificateRevocation = !sslSettings.CheckCertificateRevocation; break;
                    case "UseTls": useTls = !useTls; break;
                    case "WaitQueueSize": waitQueueSize = 99; break;
                    case "WaitQueueTimeout": waitQueueTimeout = TimeSpan.FromSeconds(99); break;
                    default: throw new ArgumentException($"Invalid field name: \"{notEqualFieldName}\".", nameof(notEqualFieldName));
                }
            }

            return new ClusterKey(
                allowInsecureTls,
                applicationName,
                clusterConfigurator,
                compressors,
                connectionMode,
                connectionModeSwitch,
                connectTimeout,
                credentials,
                directConnection,
                heartbeatInterval,
                heartbeatTimeout,
                ipv6,
                kmsProviders,
                localThreshold,
                maxConnectionIdleTime,
                maxConnectionLifeTime,
                maxConnectionPoolSize,
                minConnectionPoolSize,
                receiveBufferSize,
                replicaSetName,
                schemaMap,
                scheme,
                sdamLogFileName,
                sendBufferSize,
                servers,
                serverSelectionTimeout,
                socketTimeout,
                sslSettings,
                useTls,
                waitQueueSize,
                waitQueueTimeout);
        }

        internal ClusterKey CreateSubjectWith(
            Dictionary<string, IReadOnlyDictionary<string, object>> kmsProvidersValue = null,
            Dictionary<string, BsonDocument> schemaMapValue = null,
#pragma warning disable CS0618 // Type or member is obsolete
            ConnectionModeSwitch connectionModeSwitch = ConnectionModeSwitch.UseConnectionMode)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var allowInsecureTls = true;
            var applicationName = "app1";
            var clusterConfigurator = new Action<ClusterBuilder>(b => { });
            var compressors = new CompressorConfiguration[0];
#pragma warning disable CS0618 // Type or member is obsolete
            var connectionMode = connectionModeSwitch != ConnectionModeSwitch.UseConnectionMode ? ConnectionMode.Automatic : ConnectionMode.Direct;
#pragma warning restore CS0618 // Type or member is obsolete
            var connectTimeout = TimeSpan.FromSeconds(1);
#pragma warning disable 618
            var credentials = new List<MongoCredential> { MongoCredential.CreateMongoCRCredential("source", "username", "password") };
#pragma warning restore 618
            bool? directConnection = null;
            var heartbeatInterval = TimeSpan.FromSeconds(7);
            var heartbeatTimeout = TimeSpan.FromSeconds(8);
            var ipv6 = false;
            var kmsProviders = kmsProvidersValue ?? new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var localThreshold = TimeSpan.FromMilliseconds(20);
            var maxConnectionIdleTime = TimeSpan.FromSeconds(2);
            var maxConnectionLifeTime = TimeSpan.FromSeconds(3);
            var maxConnectionPoolSize = 50;
            var minConnectionPoolSize = 5;
            var receiveBufferSize = 1;
            var replicaSetName = "abc";
            var schemaMap = schemaMapValue ?? new Dictionary<string, BsonDocument>();
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
            var useTls = false;
            var waitQueueSize = 20;
            var waitQueueTimeout = TimeSpan.FromSeconds(5);

            return new ClusterKey(
                allowInsecureTls,
                applicationName,
                clusterConfigurator,
                compressors,
                connectionMode,
                connectionModeSwitch,
                connectTimeout,
                credentials,
                directConnection,
                heartbeatInterval,
                heartbeatTimeout,
                ipv6,
                kmsProviders,
                localThreshold,
                maxConnectionIdleTime,
                maxConnectionLifeTime,
                maxConnectionPoolSize,
                minConnectionPoolSize,
                receiveBufferSize,
                replicaSetName,
                schemaMap,
                scheme,
                sdamLogFileName,
                sendBufferSize,
                servers,
                serverSelectionTimeout,
                socketTimeout,
                sslSettings,
                useTls,
                waitQueueSize,
                waitQueueTimeout);
        }

        private Dictionary<string, IReadOnlyDictionary<string, object>> GetKmsProviders(
            bool withReverseInMainKeys = false,
            bool withReverseInNestedKeys = false,
            bool skipTheLastMainRecord = false,
            bool skipTheLastNestedRecord = false)
        {
            var options1 = new Dictionary<string, object>
            {
                { "key1", new BsonBinaryData(Convert.FromBase64String(Key1)).Bytes },
            };
            if (!skipTheLastNestedRecord)
            {
                options1.Add("key2", new BsonBinaryData(Convert.FromBase64String(Key2)).Bytes);
            }

            var options2 = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            };

            Dictionary<string, IReadOnlyDictionary<string, object>> kmsProviders = null;
            if (withReverseInNestedKeys)
            {
                kmsProviders =
                    new Dictionary<string, IReadOnlyDictionary<string, object>>()
                    {
                        { "options1", options1.Reverse().ToDictionary(k => k.Key, v => v.Value) },
                    };

                if (!skipTheLastMainRecord)
                {
                    kmsProviders.Add("options2", options2.Reverse().ToDictionary(k => k.Key, v => v.Value));
                }
            }
            else
            {
                kmsProviders =
                    new Dictionary<string, IReadOnlyDictionary<string, object>>()
                    {
                        { "options1", options1 },
                    };

                if (!skipTheLastMainRecord)
                {
                    kmsProviders.Add("options2", options2);
                }
            }

            return withReverseInMainKeys
                ? kmsProviders.Reverse().ToDictionary(k => k.Key, v => v.Value)
                : kmsProviders;
        }

        private Dictionary<string, BsonDocument> GetSchemaMaps(
            bool withReverse = false,
            bool skipLastRecord = false)
        {
            var options = new Dictionary<string, BsonDocument>
            {
                { "key1", new BsonDocument("a", 1) }
            };
            if (!skipLastRecord)
            {
                options.Add("key2", new BsonDocument("b", 2));
            }

            return withReverse
                ? options.Reverse().ToDictionary(k => k.Key, v => v.Value)
                : options;
        }
    }
}
