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
using System.Net;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Authentication.Gssapi;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Core.Misc;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
using Xunit.Sdk;
using Reflector = MongoDB.Bson.TestHelpers.Reflector;

namespace MongoDB.Driver.Tests.Specifications.auth
{
    [Category("Authentication")]
    public class AuthTestRunner
    {
        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var definition = testCase.Test;
            JsonDrivenHelper.EnsureAllFieldsAreValid(definition, "description", "uri", "valid", "credential");

            MongoCredential mongoCredential = null;
            Exception parseException = null;

            var connectionString = (string)definition["uri"];
            if (connectionString.Contains("CANONICALIZE_HOST_NAME"))
            {
                // have to skip CANONICALIZE_HOST_NAME tests. Not implemented yet. See: https://jira.mongodb.org/browse/CSHARP-3796
                throw new SkipException("Test skipped because CANONICALIZE_HOST_NAME is not supported.");
            }

            try
            {
                mongoCredential = MongoClientSettings.FromConnectionString(connectionString).Credential;
            }
            catch (Exception ex)
            {
                parseException = ex;
            }

            IAuthenticator authenticator = null;
            if (mongoCredential != null && parseException == null && !SkipActualAuthenticatorCreating(testCase.Name))
            {
                var dummyEndpoint = new DnsEndPoint("localhost", 27017);

                parseException = Record.Exception(
                    () =>
                    {
                        var saslContext = new SaslContext
                        {
                            Mechanism = mongoCredential.Mechanism,
                            Identity = mongoCredential.Identity,
                            IdentityEvidence = mongoCredential.Evidence,
                            MechanismProperties = mongoCredential._mechanismProperties(),
                            EndPoint = dummyEndpoint,
                            ClusterEndPoints = [dummyEndpoint],
                        };

                        if (mongoCredential.Mechanism?.ToUpper() == OidcSaslMechanism.MechanismName)
                        {
                            var environmentVariablesProviderMock = new Mock<IEnvironmentVariableProvider>();
                            environmentVariablesProviderMock
                                .Setup(p => p.GetEnvironmentVariable("OIDC_TOKEN_FILE")).Returns("dummy_file_path");
                            var mechanism = OidcSaslMechanism.Create(saslContext, SystemClock.Instance, environmentVariablesProviderMock.Object);
                            authenticator = new SaslAuthenticator(mechanism, null);
                            return;
                        }

                        authenticator = mongoCredential.ToAuthenticator([dummyEndpoint], null);
                    });
            }
            if (parseException == null)
            {
                AssertValid(authenticator, mongoCredential, definition);
            }
            else
            {
                AssertInvalid(parseException, definition);
            }
        }

        private void AssertValid(IAuthenticator authenticator, MongoCredential mongoCredential, BsonDocument definition)
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
                switch (mongoCredential.Mechanism)
                {
                    case GssapiSaslMechanism.MechanismName:
                        {
                            var saslAuthenticator = (SaslAuthenticator)authenticator;
                            var gssapiMechanism = (GssapiSaslMechanism)saslAuthenticator.Mechanism;
                            if (expectedMechanismProperties.IsBsonNull)
                            {
                                gssapiMechanism.ServiceName.Should().Be("mongodb"); // The default is "mongodb".
                                gssapiMechanism.CanonicalizeHostName.Should().BeFalse(); // The default is "false".
                            }
                            else
                            {
                                foreach (var expectedMechanismProperty in expectedMechanismProperties.AsBsonDocument)
                                {
                                    var mechanismName = expectedMechanismProperty.Name;
                                    switch (mechanismName)
                                    {
                                        case "SERVICE_NAME":
                                            gssapiMechanism.ServiceName.Should().Be(ValueToString(expectedMechanismProperty.Value));
                                            break;
                                        case "CANONICALIZE_HOST_NAME":
                                            gssapiMechanism.CanonicalizeHostName.Should().Be(expectedMechanismProperty.Value.ToBoolean());
                                            break;
                                        default:
                                            throw new Exception($"Invalid mechanism property '{mechanismName}'.");
                                    }
                                }
                            }
                            break;
                        }
                    case OidcSaslMechanism.MechanismName:
                        {
                            var saslAuthenticator = (SaslAuthenticator)authenticator;
                            var oidcMechanism = (OidcSaslMechanism)saslAuthenticator.Mechanism;
                            foreach (var expectedMechanismProperty in expectedMechanismProperties.AsBsonDocument)
                            {
                                var mechanismName = expectedMechanismProperty.Name;
                                switch (mechanismName)
                                {
                                    case OidcConfiguration.EnvironmentMechanismPropertyName:
                                        oidcMechanism.Configuration.Environment.Should().Be(expectedMechanismProperty.Value.ToString());
                                        break;
                                    case OidcConfiguration.TokenResourceMechanismPropertyName:
                                        oidcMechanism.Configuration.TokenResource.Should().Be(expectedMechanismProperty.Value.ToString());
                                        break;
                                    default:
                                        throw new Exception($"Invalid mechanism property '{mechanismName}'.");
                                }
                            }
                        }
                        break;
                    default:
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

                            break;
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

        private bool SkipActualAuthenticatorCreating(string testCaseName) =>
            // should be addressed in https://jira.mongodb.org/browse/CSHARP-4503
            testCaseName.Contains("MONGODB-AWS");

        private string ValueToString(BsonValue value)
        {
            return value == BsonNull.Value ? null : value.ToString();
        }

        // nested types
        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.auth.tests.legacy";
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
