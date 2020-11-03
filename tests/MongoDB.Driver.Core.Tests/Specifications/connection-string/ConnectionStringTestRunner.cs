/* Copyright 2015-present MongoDB Inc.
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
using System.Net;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Specifications.connection_string
{
    public class ConnectionStringTestRunner
    {
        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var definition = testCase.Test;
            JsonDrivenHelper.EnsureAllFieldsAreValid(definition, "valid", "options", "hosts", "auth", "description", "uri", "warning");

            ConnectionString connectionString = null;
            Exception parseException = null;
            try
            {
                connectionString = new ConnectionString((string)definition["uri"]);
            }
            catch (Exception ex)
            {
                parseException = ex;
            }

            if (parseException == null)
            {
                AssertValid(connectionString, definition);
            }
            else
            {
                AssertInvalid(parseException, definition);
            }
        }

        private void AssertBoolean(bool? value, BsonValue expectedValue)
        {
            value.Should().Be(expectedValue.IsBsonNull ? (bool?)null : expectedValue.ToBoolean());
        }

        private void AssertEnum<T>(T? value, BsonValue expectedValue) where T : struct
        {
            value.Should().Be(expectedValue.IsBsonNull ? null : (T?)Enum.Parse(typeof(T), expectedValue.AsString, true));
        }

        private void AssertOptions(ConnectionString connectionString, BsonDocument definition)
        {
            if (definition.TryGetValue("options", out var options) && !options.IsBsonNull)
            {
                foreach (var expectedOption in options.AsBsonDocument.Elements)
                {
                    var lowerName = expectedOption.Name.ToLowerInvariant();
                    switch (lowerName)
                    {
                        case "appname":
                            connectionString.ApplicationName.Should().Be(expectedOption.Value.AsString);
                            break;
                        case "authmechanism":
                            connectionString.AuthMechanism.Should().Be(expectedOption.Value.AsString);
                            break;
                        case "authmechanismproperties":
                            var authMechanismProperties = new BsonDocument(connectionString.AuthMechanismProperties.Select(kv => new BsonElement(kv.Key, ConvertToBsonValue(kv.Value))));
                            authMechanismProperties.Should().BeEquivalentTo(expectedOption.Value.AsBsonDocument);
                            break;
                        case "authsource":
                            connectionString.AuthSource.Should().Be(expectedOption.Value.AsString);
                            break;
                        case "compressors":
                            var compressors = new BsonArray(connectionString.Compressors.Select(c => CompressorTypeMapper.ToServerName(c.Type)));
                            var expectedCompressors = RemoveUnsupportedCompressors(expectedOption.Value.AsBsonArray);
                            compressors.Should().Be(expectedCompressors);
                            break;
                        case "connecttimeoutms":
                            AssertTimeSpan(connectionString.ConnectTimeout, expectedOption.Value);
                            break;
                        case "directconnection":
                            AssertBoolean(connectionString.DirectConnection, expectedOption.Value);
                            break;
                        case "heartbeatfrequencyms":
                            AssertTimeSpan(connectionString.HeartbeatInterval, expectedOption.Value);
                            break;
                        case "journal":
                            AssertBoolean(connectionString.Journal, expectedOption.Value);
                            break;
                        case "localthresholdms":
                            AssertTimeSpan(connectionString.LocalThreshold, expectedOption.Value);
                            break;
                        case "maxidletimems":
                            AssertTimeSpan(connectionString.MaxIdleTime, expectedOption.Value);
                            break;
                        case "maxstalenessseconds":
                            AssertTimeSpan(connectionString.MaxStaleness, expectedOption.Value, false);
                            break;
                        case "readconcernlevel":
                            AssertEnum(connectionString.ReadConcernLevel, expectedOption.Value);
                            break;
                        case "readpreference":
                            AssertEnum(connectionString.ReadPreference, expectedOption.Value);
                            break;
                        case "readpreferencetags":
                            var readPreferenceTags = ConvertReadPreferenceTagsToBsonArray(connectionString.ReadPreferenceTags);
                            readPreferenceTags.Should().Be(expectedOption.Value.AsBsonArray);
                            break;
                        case "replicaset":
                            connectionString.ReplicaSet.Should().Be(expectedOption.Value.AsString);
                            break;
                        case "retrywrites":
                            AssertBoolean(connectionString.RetryWrites, expectedOption.Value);
                            break;
                        case "serverselectiontimeoutms":
                            AssertTimeSpan(connectionString.ServerSelectionTimeout, expectedOption.Value);
                            break;
                        case "sockettimeoutms":
                            AssertTimeSpan(connectionString.SocketTimeout, expectedOption.Value);
                            break;
                        case "ssl":
#pragma warning disable 618
                            AssertBoolean(connectionString.Ssl, expectedOption.Value);
#pragma warning restore 618
                            break;
                        case "tls":
                            AssertBoolean(connectionString.Tls, expectedOption.Value);
                            break;
                        case "tlsdisablecertificaterevocationcheck":
                            AssertBoolean(connectionString.TlsDisableCertificateRevocationCheck, expectedOption.Value);
                            break;
                        case "tlsinsecure":
                            AssertBoolean(connectionString.TlsInsecure, expectedOption.Value);
                            break;
                        case "w":
                            var expectedW = WriteConcern.WValue.Parse(expectedOption.Value.ToString());
                            connectionString.W.Should().Be(expectedW);
                            break;
                        case "wtimeoutms":
                            AssertTimeSpan(connectionString.WTimeout, expectedOption.Value);
                            break;
                        case "zlibcompressionlevel":
                            var zlibCompressionLevel = GetZlibCompressionLevel(connectionString);
                            zlibCompressionLevel.Should().Be(expectedOption.Value.ToInt32());
                            break;
                        default:
                            throw new NotSupportedException($"Unsupported option {expectedOption.Value} in {lowerName}.");
                    }
                }
            }
        }

        private void AssertTimeSpan(TimeSpan? value, BsonValue expectedValue, bool ms = true)
        {
            var expectedResult = ms ? TimeSpan.FromMilliseconds(expectedValue.AsInt32) : TimeSpan.FromSeconds(expectedValue.AsInt32);

            value.Should().Be(expectedValue.IsBsonNull ? (TimeSpan?)null : expectedResult);
        }

        private void AssertValid(ConnectionString connectionString, BsonDocument definition)
        {
            if (!definition["valid"].ToBoolean())
            {
                throw new AssertionException($"The connection string '{definition["uri"]}' should be invalid.");
            }

            var hostsValue = definition["hosts"] as BsonArray;
            if (hostsValue != null)
            {
                var expectedEndPoints = hostsValue
                    .Select(x => ConvertExpectedHostToEndPoint((BsonDocument)x))
                    .ToList();

                var missing = expectedEndPoints.Except(connectionString.Hosts, EndPointHelper.EndPointEqualityComparer);
                missing.Any().Should().Be(false);

                var additions = connectionString.Hosts.Except(expectedEndPoints, EndPointHelper.EndPointEqualityComparer);
                additions.Any().Should().Be(false);
            }

            var authValue = definition["auth"] as BsonDocument;
            if (authValue != null)
            {
                JsonDrivenHelper.EnsureAllFieldsAreValid(authValue, "db", "username", "password");

                connectionString.DatabaseName.Should().Be(ValueToString(authValue["db"]));
                connectionString.Username.Should().Be(ValueToString(authValue["username"]));
                connectionString.Password.Should().Be(ValueToString(authValue["password"]));
            }

            AssertOptions(connectionString, definition);
        }

        private void AssertInvalid(Exception ex, BsonDocument definition)
        {
            // we will assume warnings are allowed to be errors...
            if (definition["valid"].ToBoolean() && !definition["warning"].ToBoolean())
            {
                throw new AssertionException($"The connection string '{definition["uri"]}' should be valid.", ex);
            }
        }

        private BsonArray ConvertReadPreferenceTagsToBsonArray(IReadOnlyList<TagSet> tagSets)
        {
            var tagSetsArray = new BsonArray();
            foreach (var tagSet in tagSets)
            {
                if (!tagSet.IsEmpty)
                {
                    var tagSetDocument = new BsonDocument();
                    foreach (var tag in tagSet.Tags)
                    {
                        tagSetDocument.Add(tag.Name, tag.Value);
                    }
                    tagSetsArray.Add(tagSetDocument);
                }
            }
            return tagSetsArray;
        }

        private BsonValue ConvertToBsonValue(string value)
        {
            if (long.TryParse(value, out var @long))
            {
                return @long;
            }
            else if (bool.TryParse(value, out var @bool))
            {
                return @bool;
            }
            else if (decimal.TryParse(value, out var @decimal))
            {
                return @decimal;
            }
            else if (DateTime.TryParse(value, out var dateTime))
            {
                return dateTime;
            }
            else
            {
                return value;
            }
        }

        private int GetZlibCompressionLevel(ConnectionString connectionString)
        {
            return (int)connectionString
                .Compressors
                .First(c => c.Type == CompressorType.Zlib)
                .Properties
                .Single(kv => kv.Key == "Level")
                .Value;
        }

        private string ValueToString(BsonValue value)
        {
            if (value == BsonNull.Value)
            {
                return null;
            }

            return value.ToString();
        }

        private EndPoint ConvertExpectedHostToEndPoint(BsonDocument expectedHost)
        {
            var port = expectedHost["port"];
            if (port.IsBsonNull)
            {
                port = 27017;
            }
            if (expectedHost["type"] == "ipv4" || expectedHost["type"] == "ip_literal")
            {
                return new IPEndPoint(
                    IPAddress.Parse(ValueToString(expectedHost["host"])),
                    port.ToInt32());
            }
            else if (expectedHost["type"] == "hostname")
            {
                return new DnsEndPoint(
                    ValueToString(expectedHost["host"]),
                    port.ToInt32());
            }
            else if (expectedHost["type"] == "unix")
            {
                throw new SkipException("Test skipped because unix host types are not supported.");
            }

            throw new AssertionException($"Unknown host type {expectedHost["type"]}.");
        }

        private BsonArray RemoveUnsupportedCompressors(BsonArray compressors)
        {
            bool isSupported(BsonValue value) =>
                Enum.TryParse<CompressorType>(value.AsString, ignoreCase: true, out var type) &&
                CompressorSource.IsCompressorSupported(type);
            return new BsonArray(compressors.Where(isSupported));
        }

        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            private static readonly string[] __ignoredTestNames =
            {
                "invalid-uris.json:Missing delimiting slash between hosts and options",
                // Not supported readConcernLevel options are not allowed for parsing
                "concern-options.json:Arbitrary string readConcernLevel does not cause a warning",
                // tlsAllowInvalidCertificates and tlsAllowInvalidHostnames are not supported
                "tls-options.json:tlsAllowInvalidCertificates and tlsInsecure both present (and false) raises an error",
                "tls-options.json:tlsAllowInvalidCertificates and tlsInsecure both present (and true) raises an error",
                "tls-options.json:tlsAllowInvalidHostnames and tlsInsecure both present (and false) raises an error",
                "tls-options.json:tlsAllowInvalidHostnames and tlsInsecure both present (and true) raises an error",
                "tls-options.json:tlsInsecure and tlsAllowInvalidCertificates both present (and true) raises an error",
                "tls-options.json:tlsInsecure and tlsAllowInvalidCertificates both present (and false) raises an error",
                "tls-options.json:tlsInsecure and tlsAllowInvalidHostnames both present (and false) raises an error",
                "tls-options.json:tlsInsecure and tlsAllowInvalidHostnames both present (and true) raises an error",
                "tls-options.json:tlsAllowInvalidCertificates is parsed correctly",
                "tls-options.json:tlsAllowInvalidHostnames is parsed correctly",
                // tlsCAFile, tlsCertificateKeyFile and tlsCertificateKeyFilePassword=hunter are not supported",
                "tls-options.json:Valid required tls options are parsed correctly",
                "tls-options.json:Valid tlsCertificateKeyFilePassword is parsed correctly",
                // tlsDisableOCSPEndpointCheck not supported
                "tls-options.json:tlsDisableOCSPEndpointCheck can be set to true",
                "tls-options.json:tlsDisableOCSPEndpointCheck can be set to false",
                 // tlsAllowInvalidCertificateNames and tlsDisableOCSPEndpointCheck not supported in any order
                 "tls-options.json:tlsDisableOCSPEndpointCheck and tlsAllowInvalidCertificates both present (and true) raises an error",
                 "tls-options.json:tlsDisableOCSPEndpointCheck=true and tlsAllowInvalidCertificates=false raises an error",
                 "tls-options.json:tlsDisableOCSPEndpointCheck=false and tlsAllowInvalidCertificates=true raises an error",
                 "tls-options.json:tlsDisableOCSPEndpointCheck and tlsAllowInvalidCertificates both present (and false) raises an error",
                 "tls-options.json:tlsAllowInvalidCertificates and tlsDisableOCSPEndpointCheck both present (and true) raises an error",
                 "tls-options.json:tlsAllowInvalidCertificates=true and tlsDisableOCSPEndpointCheck=false raises an error",
                 "tls-options.json:tlsAllowInvalidCertificates=false and tlsDisableOCSPEndpointCheck=true raises an error",
                 "tls-options.json:tlsAllowInvalidCertificates and tlsDisableOCSPEndpointCheck both present (and false) raises an error",
                 // tlsDisableOCSPEndpointCheck and tlsInsecure not supported together in any order
                 "tls-options.json:tlsDisableOCSPEndpointCheck and tlsInsecure both present (and true) raises an error",
                 "tls-options.json:tlsDisableOCSPEndpointCheck=true and tlsInsecure=false raises an error",
                 "tls-options.json:tlsDisableOCSPEndpointCheck=false and tlsInsecure=true raises an error",
                 "tls-options.json:tlsDisableOCSPEndpointCheck and tlsInsecure both present (and false) raises an error",
                 "tls-options.json:tlsInsecure and tlsDisableOCSPEndpointCheck both present (and true) raises an error",
                 "tls-options.json:tlsInsecure=true and tlsDisableOCSPEndpointCheck=false raises an error",
                 "tls-options.json:tlsInsecure=false and tlsDisableOCSPEndpointCheck=true raises an error",
                 "tls-options.json:tlsInsecure and tlsDisableOCSPEndpointCheck both present (and false) raises an error",
                 // tlsDisableOCSPEndpointCheck and tlsDisableCertificateRevocationCheck not supported together in any order
                "tls-options.json:tlsDisableOCSPEndpointCheck and tlsDisableCertificateRevocationCheck both present (and true) raises an error",
                "tls-options.json:tlsDisableOCSPEndpointCheck=true and tlsDisableCertificateRevocationCheck=false raises an error",
                "tls-options.json:tlsDisableOCSPEndpointCheck=false and tlsDisableCertificateRevocationCheck=true raises an error",
                "tls-options.json:tlsDisableOCSPEndpointCheck and tlsDisableCertificateRevocationCheck both present (and false) raises an error",
                "tls-options.json:tlsDisableCertificateRevocationCheck and tlsDisableOCSPEndpointCheck both present (and true) raises an error",
                "tls-options.json:tlsDisableCertificateRevocationCheck=true and tlsDisableOCSPEndpointCheck=false raises an error",
                "tls-options.json:tlsDisableCertificateRevocationCheck=false and tlsDisableOCSPEndpointCheck=true raises an error",
                "tls-options.json:tlsDisableCertificateRevocationCheck and tlsDisableOCSPEndpointCheck both present (and false) raises an error",
                // tlsAllowInvalidCertificates and tlsDisableCertificateRevocationCheck not supported in any order
                "tls-options.json:tlsAllowInvalidCertificates and tlsDisableCertificateRevocationCheck both present (and true) raises an error",
                "tls-options.json:tlsAllowInvalidCertificates=true and tlsDisableCertificateRevocationCheck=false raises an error",
                "tls-options.json:tlsAllowInvalidCertificates=false and tlsDisableCertificateRevocationCheck=true raises an error",
                "tls-options.json:tlsAllowInvalidCertificates and tlsDisableCertificateRevocationCheck both present (and false) raises an error",
                "tls-options.json:tlsDisableCertificateRevocationCheck and tlsAllowInvalidCertificates both present (and true) raises an error",
                "tls-options.json:tlsDisableCertificateRevocationCheck=true and tlsAllowInvalidCertificates=false raises an error",
                "tls-options.json:tlsDisableCertificateRevocationCheck=false and tlsAllowInvalidCertificates=true raises an error",
                "tls-options.json:tlsDisableCertificateRevocationCheck and tlsAllowInvalidCertificates both present (and false) raises an error",
                // the .Net driver is not single-threaded. serverSelectionTryOnce is not supported
                "single-threaded-options.json:Valid options specific to single-threaded drivers are parsed correctly"
            };

            protected override string[] PathPrefixes =>
                new[]
                {
                    "MongoDB.Driver.Core.Tests.Specifications.connection_string.tests.",
                    "MongoDB.Driver.Core.Tests.Specifications.uri_options.tests."
                };

            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                return base.CreateTestCases(document)
                    .Where(test => !__ignoredTestNames.Any(ignoredName => test.Name.EndsWith(ignoredName)));
            }
        }
    }
}
