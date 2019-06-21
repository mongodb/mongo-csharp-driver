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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit.Sdk;
using System.Collections;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Specifications.connection_string
{
    public class TestRunner
    {
        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var definition = testCase.Test;
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
            if (authValue != null)
            {
                connectionString.DatabaseName.Should().Be(ValueToString(authValue["db"]));
                connectionString.Username.Should().Be(ValueToString(authValue["username"]));
                connectionString.Password.Should().Be(ValueToString(authValue["password"]));
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

        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            private static readonly string[] __ignoredTestNames = new string[]
            {
                "invalid-uris.json:Missing delimiting slash between hosts and options"
            };

            protected override string PathPrefix => "MongoDB.Driver.Core.Tests.Specifications.connection_string.tests.";

            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                return base.CreateTestCases(document)
                    .Where(test => !__ignoredTestNames.Any(ignoredName => test.Name.EndsWith(ignoredName)));
            }
        }
    }
}
