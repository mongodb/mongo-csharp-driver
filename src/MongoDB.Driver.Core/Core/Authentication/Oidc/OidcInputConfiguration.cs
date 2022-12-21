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
using System.Collections.Concurrent;
using System.Net;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal sealed class OidcInputConfiguration
    {
        private readonly EndPoint _endpoint;
        private readonly string _principalName;
        private readonly string _providerName;
        private readonly IRequestCallbackProvider _requestCallbackProvider;
        private readonly IRefreshCallbackProvider _refreshCallbackProvider;

        public OidcInputConfiguration(
            EndPoint endpoint,
            string principalName = null,
            string providerName = null,
            IRequestCallbackProvider requestCallbackProvider = null,
            IRefreshCallbackProvider refreshCallbackProvider = null)
        {
            _endpoint = Ensure.IsNotNull(endpoint, nameof(endpoint));
            _providerName = providerName; // can be null
            _principalName = principalName; // can be null
            _requestCallbackProvider = requestCallbackProvider; // can be null
            _refreshCallbackProvider = refreshCallbackProvider; // can be null

            EnsureOptionsValid();
        }

        public EndPoint EndPoint => _endpoint;
        public bool IsCallbackWorkflow => _requestCallbackProvider != null || _refreshCallbackProvider != null;
        public string PrincipalName => _principalName;
        public string ProviderName => _providerName;
        public IRequestCallbackProvider RequestCallbackProvider => _requestCallbackProvider;
        public IRefreshCallbackProvider RefreshCallbackProvider => _refreshCallbackProvider;

        public override int GetHashCode() =>
            new Hasher()
                .Hash(_providerName)
                .Hash(_endpoint)
                .Hash(_principalName)
                .GetHashCode();

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null) || GetType() != obj.GetType()) { return false; }
            var rhs = (OidcInputConfiguration)obj;
            return
                // This class is used as an oidc cache key for a callback workflow.
                _providerName == rhs._providerName&&
                EndPointHelper.Equals(_endpoint, rhs._endpoint) &&
                object.Equals(_requestCallbackProvider, rhs._requestCallbackProvider) &&
                object.Equals(_refreshCallbackProvider, rhs._refreshCallbackProvider) &&
                _principalName == rhs._principalName; 
        }

        private void EnsureOptionsValid()
        {
            if (_providerName != null && (_requestCallbackProvider != null || _refreshCallbackProvider != null))
            {
                throw new InvalidOperationException($"{MongoOidcAuthenticator.ProviderName} and OIDC callbacks cannot both be set.");
            }

            if (_providerName == null && _requestCallbackProvider == null && _refreshCallbackProvider == null)
            {
                throw new InvalidOperationException($"{MongoOidcAuthenticator.ProviderName} or OIDC callbacks must be configured.");
            }

            if (_refreshCallbackProvider != null && _requestCallbackProvider == null)
            {
                throw new InvalidOperationException($"{MongoOidcAuthenticator.RequestCallbackName} must be provided with {MongoOidcAuthenticator.RefreshCallbackName}.");
            }

            if (_principalName != null && _providerName != null)
            {
                throw new InvalidOperationException($"PrincipalName is mutually exclusive with {MongoOidcAuthenticator.ProviderName}.");
            }
        }
    }
}
