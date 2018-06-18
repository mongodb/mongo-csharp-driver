/* Copyright 2018-present MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.auth
{
    public class TestRunner
    {
        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(BsonDocument definition)
        {
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

            var mongoCredential = MongoClientSettings.FromConnectionString(connectionString.ToString()).Credential;
            if (authValue != null)
            {
                mongoCredential.Source.Should().Be(ValueToString(authValue["db"]));
                mongoCredential.Username.Should().Be(ValueToString(authValue["username"]));
#pragma warning disable 618
                mongoCredential.Password.Should().Be(ValueToString(authValue["password"]));
#pragma warning restore 618
                
            }
        }

        private void AssertInvalid(Exception ex, BsonDocument definition)
        {
            // we will assume warnings are allowed to be errors...
            if (definition["valid"].ToBoolean() && !definition["warning"].ToBoolean())
            {
                throw new AssertionException($"The connection string '{definition["uri"]}' should be valid.", ex);
            }
        }

        private string ValueToString(BsonValue value)
        {
            return value == BsonNull.Value ? null : value.ToString();
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
                throw new SkipTestException("Test skipped because unix host types are not supported.");
            }

            throw new AssertionException($"Unknown host type {expectedHost["type"]}.");
        }


        private class TestCaseFactory : IEnumerable<object[]>
        {
            // TODO: remove these ignoredTestNames once the driver implements the underlying changes required
            private static readonly string[] __ignoredTestNames =
            {
                "connection-string: should recognize the mechanism (PLAIN)",
                "connection-string: should throw an exception if authSource is invalid (GSSAPI)",
                "connection-string: should throw an exception if authSource is invalid (MONGODB-X509)",
                "connection-string: should throw an exception if no username (GSSAPI)",
                "connection-string: should throw an exception if no username (PLAIN)",
                "connection-string: should throw an exception if no username (SCRAM-SHA-1)",
                "connection-string: should throw an exception if no username (SCRAM-SHA-256)",
                "connection-string: should throw an exception if no username is supplied (MONGODB-CR)",
                "connection-string: should throw an exception if supplied a password (MONGODB-X509)"                
            };

            public IEnumerator<object[]> GetEnumerator()
            {
#if NET45
                const string prefix = "MongoDB.Driver.Tests.Specifications.auth.tests.";
#else
                const string prefix = "MongoDB.Driver.Tests.Dotnet.Specifications.auth.tests.";
#endif
                var executingAssembly = typeof(TestCaseFactory).GetTypeInfo().Assembly;
                var runTestDefinitionParameters = executingAssembly
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .Select(path => new { Filename = path.Remove(0, prefix.Length).Remove(path.Length - prefix.Length - 5),
                                          Tests = (BsonArray)ReadDefinition(path)["tests"] })
                    .SelectMany(definition => definition.Tests
                        .Where(test => !__ignoredTestNames.Contains($"{definition.Filename}: {test["description"]}"))
                        .Select(test => new[] { test }));
                return runTestDefinitionParameters.GetEnumerator();
            }
            
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private static BsonDocument ReadDefinition(string path)
            {
                var executingAssembly = typeof(TestCaseFactory).GetTypeInfo().Assembly;
                using (var definitionStream = executingAssembly.GetManifestResourceStream(path))
                using (var definitionStringReader = new StreamReader(definitionStream))
                {
                    var definitionString = definitionStringReader.ReadToEnd();
                    return BsonDocument.Parse(definitionString);
                }
            }
        }
    }
}
