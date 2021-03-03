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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Authentication;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.auth
{
    public class AuthTestRunner
    {
        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var definition = testCase.Test;
            JsonDrivenHelper.EnsureAllFieldsAreValid(definition, "description", "uri", "valid", "credential");

            MongoCredential mongoCredential = null;
            Exception parseException = null;
            try
            {
                var connectionString = (string)definition["uri"];
                mongoCredential = MongoClientSettings.FromConnectionString(connectionString).Credential;
            }
            catch (Exception ex)
            {
                parseException = ex;
            }

            if (parseException == null)
            {
                AssertValid(mongoCredential, definition);
            }
            else
            {
                AssertInvalid(parseException, definition);
            }
        }

        private void AssertValid(MongoCredential mongoCredential, BsonDocument definition)
        {
            if (!definition["valid"].ToBoolean())
            {
                throw new AssertionException($"The connection string '{definition["uri"]}' should be invalid.");
            }

            var expectedCredential = definition["credential"] as BsonDocument;
            if (expectedCredential == null)
            {
                mongoCredential.Should().BeNull();
            }
            else
            {
                JsonDrivenHelper.EnsureAllFieldsAreValid(expectedCredential, "username", "password", "source", "mechanism", "mechanism_properties");
                mongoCredential.Username.Should().Be(ValueToString(expectedCredential["username"]));
#pragma warning disable 618
                mongoCredential.Password.Should().Be(ValueToString(expectedCredential["password"]));
#pragma warning restore 618
                mongoCredential.Source.Should().Be(ValueToString(expectedCredential["source"]));
                mongoCredential.Mechanism.Should().Be(ValueToString(expectedCredential["mechanism"]));

                var expectedMechanismProperties = expectedCredential["mechanism_properties"];
                if (mongoCredential.Mechanism == GssapiAuthenticator.MechanismName)
                {
                    var gssapiAuthenticator = (GssapiAuthenticator)mongoCredential.ToAuthenticator(serverApi: null);
                    if (expectedMechanismProperties.IsBsonNull)
                    {
                        var serviceName = gssapiAuthenticator._mechanism_serviceName();
                        serviceName.Should().Be("mongodb"); // The default is "mongodb".
                        var canonicalizeHostName = gssapiAuthenticator._mechanism_canonicalizeHostName();
                        canonicalizeHostName.Should().BeFalse(); // The default is "false".
                    }
                    else
                    {
                        foreach (var expectedMechanismProperty in expectedMechanismProperties.AsBsonDocument)
                        {
                            var mechanismName = expectedMechanismProperty.Name;
                            switch (mechanismName)
                            {
                                case "SERVICE_NAME":
                                    var serviceName = gssapiAuthenticator._mechanism_serviceName();
                                    serviceName.Should().Be(ValueToString(expectedMechanismProperty.Value));
                                    break;
                                case "CANONICALIZE_HOST_NAME":
                                    var canonicalizeHostName = gssapiAuthenticator._mechanism_canonicalizeHostName();
                                    canonicalizeHostName.Should().Be(expectedMechanismProperty.Value.ToBoolean());
                                    break;
                                default:
                                    throw new Exception($"Invalid mechanism property '{mechanismName}'.");
                            }
                        }
                    }
                }
                else
                {
                    var actualMechanismProperties = mongoCredential._mechanismProperties();
                    if (expectedMechanismProperties.IsBsonNull)
                    {
                        actualMechanismProperties.Should().BeEmpty();
                    }
                    else
                    {
                        var authMechanismProperties = new BsonDocument(actualMechanismProperties.Select(kv => new BsonElement(kv.Key, BsonValue.Create(kv.Value))));
                        authMechanismProperties.Should().BeEquivalentTo(expectedMechanismProperties.AsBsonDocument);
                    }
                }
            }
        }

        private void AssertInvalid(Exception ex, BsonDocument definition)
        {
            if (definition["valid"].ToBoolean())
            {
                throw new AssertionException($"The connection string '{definition["uri"]}' should be valid.", ex);
            }
        }

        private string ValueToString(BsonValue value)
        {
            return value == BsonNull.Value ? null : value.ToString();
        }

        // nested types
        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.auth.tests.";
        }
    }

    internal static class GssapiAuthenticatorReflector
    {
        public static bool _mechanism_canonicalizeHostName(this GssapiAuthenticator obj)
        {
            var mechanism = _mechanism(obj);
            return (bool)Reflector.GetFieldValue(mechanism, "_canonicalizeHostName");
        }

        public static string _mechanism_serviceName(this GssapiAuthenticator obj)
        {
            var mechanism = _mechanism(obj);
            return (string)Reflector.GetFieldValue(mechanism, "_serviceName");
        }

        private static object _mechanism(GssapiAuthenticator obj)
        {
            return Reflector.GetFieldValue(obj, nameof(_mechanism));
        }
    }

    internal static class MongoCredentialReflector
    {
        public static Dictionary<string, object> _mechanismProperties(this MongoCredential obj)
        {
            return (Dictionary<string, object>)Reflector.GetFieldValue(obj, nameof(_mechanismProperties));
        }
    }
}
