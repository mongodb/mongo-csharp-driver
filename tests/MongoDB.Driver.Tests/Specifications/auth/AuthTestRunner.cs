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
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Authentication;
using Xunit;
using MongoDB.Driver.Core.Authentication.Oidc;
using System.Net;
using System.Threading;
using MongoDB.Driver.Core.Authentication.External;
using Moq;

namespace MongoDB.Driver.Tests.Specifications.auth
{
    [Trait("Category", "Authentication")]
    public class AuthTestRunner
    {
        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            if (testCase.Name.Contains("with aws device (MONGODB-OIDC)"))
            {
                RequireEnvironment.Check().EnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE"); // required for OIDC aws device
            }

            var definition = testCase.Test;
            JsonDrivenHelper.EnsureAllFieldsAreValid(definition, "description", "uri", "valid", "callback", "credential");

            MongoCredential mongoCredential = null;
            Exception parseException = null;
            try
            {
                var connectionString = (string)definition["uri"];
                mongoCredential = MongoClientSettings.FromConnectionString(connectionString).Credential;
                if (definition.TryGetValue("callback", out var callbacks))
                {
                    foreach (var callback in callbacks.AsBsonArray)
                    {
                        switch (callback.AsString)
                        {
                            case "oidcRequest":
                                IRequestCallbackProvider requestCallback = Mock.Of<IRequestCallbackProvider>();
                                mongoCredential = mongoCredential.WithMechanismProperty(MongoOidcAuthenticator.RequestCallbackName, requestCallback);
                                break;
                            case "oidcRefresh":
                                IRefreshCallbackProvider refreshCallback = Mock.Of<IRefreshCallbackProvider>();
                                mongoCredential = mongoCredential.WithMechanismProperty(MongoOidcAuthenticator.RefreshCallbackName, refreshCallback);
                                break;
                            default: throw new NotSupportedException($"Not supported callback type: {callback.AsString}.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                parseException = ex;
            }

            var dummyEndpoint = new DnsEndPoint("localhost", 27017);
            IAuthenticator authenticator = null;
            if (parseException == null && !SkipActualAuthenticatorCreating(testCase.Name))
            {
                parseException = Record.Exception(() => authenticator = mongoCredential?.ToAuthenticator(dummyEndpoint, serverApi: null));
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
                    case GssapiAuthenticator.MechanismName:
                        {
                            var gssapiAuthenticator = (GssapiAuthenticator)authenticator;
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
                            break;
                        }
                    case MongoOidcAuthenticator.MechanismName:
                        {
                            var oidcAuthenticator = (MongoOidcAuthenticator)authenticator;
                            foreach (var expectedMechanismProperty in expectedMechanismProperties.AsBsonDocument)
                            {
                                var mechanismName = expectedMechanismProperty.Name;
                                switch (mechanismName)
                                {
                                    case MongoOidcAuthenticator.RequestCallbackName:
                                        {
                                            var inputConfiguration = oidcAuthenticator._mechanism_oidsCredentialsProvider_inputConfiguration();
                                            (inputConfiguration.RequestCallbackProvider != null).Should().Be(expectedMechanismProperty.Value.ToBoolean());
                                        }
                                        break;
                                    case MongoOidcAuthenticator.RefreshCallbackName:
                                        {
                                            var inputConfiguration = oidcAuthenticator._mechanism_oidsCredentialsProvider_inputConfiguration();
                                            (inputConfiguration.RefreshCallbackProvider != null).Should().Be(expectedMechanismProperty.Value.ToBoolean());
                                        }
                                        break;
                                    case MongoOidcAuthenticator.ProviderName:
                                        {
                                            var provider = oidcAuthenticator._mechanism_providerWorkflowCredentialsProvider();
                                            var providerName = expectedMechanismProperty.Value.ToString();
                                            switch (providerName)
                                            {
                                                case "aws": provider.Should().BeOfType<OidcAuthenticationCredentialsProviderAdapter<OidcCredentials>>(); break;
                                                case "azure": provider.Should().BeOfType<OidcAuthenticationCredentialsProviderAdapter<AzureCredentials>>(); break;
                                                case "gcp": provider.Should().BeOfType<OidcAuthenticationCredentialsProviderAdapter<GcpCredentials>>(); break;
                                                default: throw new ArgumentException($"Unsupported device name {providerName}.");
                                            }
                                        }
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
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.auth.tests.legacy.";
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

    internal static class OidcAuthenticatorReflector
    {
        public static OidcInputConfiguration _mechanism_oidsCredentialsProvider_inputConfiguration(this MongoOidcAuthenticator obj)
        {
            var mechanism = _mechanism(obj);
            var credentialsProvider = _oidsCredentialsProvider(mechanism);
            return (OidcInputConfiguration)Reflector.GetFieldValue(credentialsProvider, "_inputConfiguration");
        }

        public static IExternalAuthenticationCredentialsProvider<OidcCredentials> _mechanism_providerWorkflowCredentialsProvider(this MongoOidcAuthenticator obj)
        {
            var mechanism = _mechanism(obj);
            return (IExternalAuthenticationCredentialsProvider<OidcCredentials>)Reflector.GetFieldValue(mechanism, "_providerWorkflowCredentialsProvider");
        }

        private static IOidcExternalAuthenticationCredentialsProvider _oidsCredentialsProvider(object mechanism) =>
            (IOidcExternalAuthenticationCredentialsProvider)Reflector.GetFieldValue(mechanism, nameof(_oidsCredentialsProvider));

        private static object _mechanism(MongoOidcAuthenticator obj) => Reflector.GetFieldValue(obj, nameof(_mechanism));
    }

    internal static class MongoCredentialReflector
    {
        public static Dictionary<string, object> _mechanismProperties(this MongoCredential obj)
        {
            return (Dictionary<string, object>)Reflector.GetFieldValue(obj, nameof(_mechanismProperties));
        }
    }
}
