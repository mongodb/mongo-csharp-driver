/* Copyright 2010-2013 10gen Inc.
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

using System.Collections.Generic;

namespace MongoDB.Driver.Communication.Security.Mechanisms
{
    /// <summary>
    /// Implements the GssApi specification using the Gsasl library.
    /// </summary>
    internal class GsaslGssapiImplementation : GsaslImplementationBase
    {
        // private fields
        private readonly string _authorizationId;
        private readonly MongoIdentityEvidence _evidence;
        private readonly string _servicePrincipalName;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GsaslGssapiImplementation" /> class.
        /// </summary>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="username">The username.</param>
        /// <param name="evidence">The evidence.</param>
        public GsaslGssapiImplementation(string serverName, string username, MongoIdentityEvidence evidence)
            : base("GSSAPI", new byte[0])
        {
            _authorizationId = username;
            _evidence = evidence;
            _servicePrincipalName = "mongodb/" + serverName;
        }

        // protected methods
        /// <summary>
        /// Gets the properties that should be used in the specified mechanism.
        /// </summary>
        /// <returns>The properties.</returns>
        protected override IEnumerable<KeyValuePair<string, string>> GetProperties()
        {
            yield return new KeyValuePair<string, string>("AUTHZID", _authorizationId);
            yield return new KeyValuePair<string, string>("AUTHID", _authorizationId);
            if (_evidence is PasswordEvidence)
            {
                yield return new KeyValuePair<string, string>("PASSWORD", ((PasswordEvidence)_evidence).Password); // TODO: fix this to be secure
            }
            var atIndex = _authorizationId.LastIndexOf("@");
            if (atIndex != -1 && atIndex != _authorizationId.Length - 1)
            {
                var realm = _authorizationId.Substring(atIndex + 1);
                yield return new KeyValuePair<string, string>("REALM", realm);
            }
            yield return new KeyValuePair<string, string>("SERVICE", _servicePrincipalName);
        }
    }
}