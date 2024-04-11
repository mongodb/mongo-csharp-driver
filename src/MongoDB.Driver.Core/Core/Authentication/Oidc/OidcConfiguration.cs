﻿/* Copyright 2010-present MongoDB Inc.
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
        public const string CallbackMechanismPropertyName = "OIDC_CALLBACK";
        public const string EnvironmentMechanismPropertyName = "ENVIRONMENT";
        public const string TokenResourceMechanismPropertyName = "TOKEN_RESOURCE";

        private static readonly ISet<string> __supportedEnvironments = new HashSet<string> { "test", "azure" };
        private readonly int _hashCode;

        public OidcConfiguration(
            IEnumerable<EndPoint> endPoints,
            string principalName,
            IEnumerable<KeyValuePair<string, object>> authMechanismProperties)
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
                        case CallbackMechanismPropertyName:
                            Callback = GetProperty<IOidcCallback>(authorizationProperty);
                            break;
                        case EnvironmentMechanismPropertyName:
                            Environment = GetProperty<string>(authorizationProperty);
                            break;
                        case TokenResourceMechanismPropertyName:
                            TokenResource = GetProperty<string>(authorizationProperty);
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

            static T GetProperty<T>(KeyValuePair<string, object> property)
            {
                if (property.Value is T result)
                {
                    return result;
                }

                throw new ArgumentException($"Cannot read {property.Key} property as {typeof(T).Name}", property.Key);
            }
        }

        public IOidcCallback Callback { get; }
        public IEnumerable<EndPoint> EndPoints { get; }
        public string Environment { get; }
        public string PrincipalName { get; }
        public string TokenResource { get; }

        public override int GetHashCode() => _hashCode;

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                obj is OidcConfiguration other &&
                Environment == other.Environment &&
                PrincipalName == other.PrincipalName &&
                object.Equals(Callback, other.Callback) &&
                TokenResource == other.TokenResource &&
                EndPoints.SequenceEqual(other.EndPoints, EndPointHelper.EndPointEqualityComparer);
        }

        private int CalculateHashCode()
            => new Hasher()
                .Hash(Environment)
                .Hash(PrincipalName)
                .HashElements(EndPoints)
                .Hash(TokenResource)
                .GetHashCode();

        private void ValidateOptions()
        {
            if (Environment == null && Callback == null)
            {
                throw new ArgumentException($"{EnvironmentMechanismPropertyName} or {CallbackMechanismPropertyName} must be configured.");
            }

            if (Environment != null && Callback != null)
            {
                throw new ArgumentException($"{CallbackMechanismPropertyName} is mutually exclusive with {EnvironmentMechanismPropertyName}.");
            }

            if (Environment != null && !__supportedEnvironments.Contains(Environment))
            {
                throw new ArgumentException(
                    $"Not supported value of {EnvironmentMechanismPropertyName} mechanism property: {Environment}.",
                    EnvironmentMechanismPropertyName);
            }

            if (!string.IsNullOrEmpty(TokenResource) && Environment != "azure")
            {
                throw new ArgumentException(
                    $"{TokenResourceMechanismPropertyName} mechanism property supported only by azure environment.",
                    TokenResourceMechanismPropertyName);
            }

            if (Environment == "azure" && string.IsNullOrEmpty(TokenResource))
            {
                throw new ArgumentException(
                    $"{TokenResourceMechanismPropertyName} mechanism property is required by azure environment.",
                    TokenResourceMechanismPropertyName);
            }
        }
    }
}
