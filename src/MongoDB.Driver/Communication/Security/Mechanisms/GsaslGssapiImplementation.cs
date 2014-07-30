/* Copyright 2010-2014 MongoDB Inc.
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
        private readonly string _hostname;
        private readonly string _hostRealm;
        private readonly string _service;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GsaslGssapiImplementation" /> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="username">The username.</param>
        /// <param name="hostRealm">The realm.</param>
        public GsaslGssapiImplementation(string serviceName, string hostName, string hostRealm, string username)
            : base("GSSAPI", new byte[0])
        {
            _authorizationId = username;
            _hostRealm = hostRealm;
            _hostname = hostName;
            _service = serviceName;
        }

        // protected methods
        /// <summary>
        /// Gets the properties that should be used in the specified mechanism.
        /// </summary>
        /// <returns>The properties.</returns>
        protected override IEnumerable<KeyValuePair<string, string>> GetProperties()
        {
            yield return new KeyValuePair<string, string>("AUTHID", _authorizationId);
            yield return new KeyValuePair<string, string>("HOSTNAME", _hostname);
            yield return new KeyValuePair<string, string>("SERVICE", _service);
            if (!string.IsNullOrEmpty(_hostRealm))
            {
                yield return new KeyValuePair<string, string>("REALM", _hostRealm);
            }
        }
    }
}