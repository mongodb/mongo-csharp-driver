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
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal sealed class OidcConfiguration
    {
        private readonly int _hashCode;

        public OidcConfiguration(
            IEnumerable<EndPoint> endPoints,
            string principalName,
            IEnumerable<KeyValuePair<string, object>> authMechanismProperties,
            IOidcKnownCallbackProviders oidcKnownCallbackProviders)
        {
            EndPoints = Ensure.IsNotNullOrEmpty(endPoints, nameof(endPoints));
            Ensure.IsNotNull(authMechanismProperties, nameof(authMechanismProperties));
            PrincipalName = principalName;

            if (authMechanismProperties != null)
            {
                foreach (var authorizationProperty in authMechanismProperties)
                {
                    switch (authorizationProperty.Key)
                    {
                        case MongoOidcAuthenticator.CallbackMechanismPropertyName:
                            {
                                Callback = GetProperty<IOidcCallback>(authorizationProperty);
                            }
                            break;
                        case MongoOidcAuthenticator.ProviderMechanismPropertyName:
                            {
                                ProviderName = GetProperty<string>(authorizationProperty);
                            }
                            break;
                        default:
                            throw new ArgumentException(
                                $"Unknown OIDC property '{authorizationProperty.Key}'.",
                                authorizationProperty.Key);
                    }
                }
            }

            ValidateOptions();
            _hashCode = CalculateHashCode();

            if (ProviderName != null)
            {
                Callback = ProviderName switch
                {
                    "aws" => oidcKnownCallbackProviders.Aws,
                    _ => throw new ArgumentException(
                        $"Not supported value of {MongoOidcAuthenticator.ProviderMechanismPropertyName} mechanism property: {ProviderName}.",
                        MongoOidcAuthenticator.ProviderMechanismPropertyName)
                };
            }

            static T GetProperty<T>(KeyValuePair<string, object> property)
            {
                if (property.Value is T result)
                {
                    return result;
                }

                throw new ArgumentException($"Cannot read {property.Key} property as {typeof(T).Name}", property.Key);
            }
        }

        public IEnumerable<EndPoint> EndPoints { get; }
        public string PrincipalName { get; }
        public string ProviderName { get; }
        public IOidcCallback Callback { get; }

        public override int GetHashCode() => _hashCode;

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                obj is OidcConfiguration other &&
                ProviderName == other.ProviderName &&
                PrincipalName == other.PrincipalName &&
                object.Equals(Callback, other.Callback) &&
                EndPoints.SequenceEqual(other.EndPoints, EndPointHelper.EndPointEqualityComparer);
        }

        private int CalculateHashCode()
            => new Hasher()
                .Hash(ProviderName)
                .Hash(PrincipalName)
                .HashElements(EndPoints)
                .GetHashCode();

        private void ValidateOptions()
        {
            if (ProviderName == null && Callback == null)
            {
                throw new ArgumentException($"{MongoOidcAuthenticator.ProviderMechanismPropertyName} or {MongoOidcAuthenticator.CallbackMechanismPropertyName} must be configured.");
            }

            if (ProviderName != null && Callback != null)
            {
                throw new ArgumentException($"{MongoOidcAuthenticator.CallbackMechanismPropertyName} is mutually exclusive with {MongoOidcAuthenticator.ProviderMechanismPropertyName}.");
            }
        }
    }
}
