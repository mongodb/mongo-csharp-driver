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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Communication.Security.Mechanisms
{
    /// <summary>
    /// A mechanism implementing the GSS API specification.
    /// </summary>
    internal class GssapiMechanism : ISaslMechanism
    {
        // private static fields
        private static bool __useGsasl = !Environment.OSVersion.Platform.ToString().Contains("Win");

        // public properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        public string Name
        {
            get { return "GSSAPI"; }
        }

        // public methods
        /// <summary>
        /// Determines whether this instance can authenticate with the specified credential.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <returns>
        ///   <c>true</c> if this instance can authenticate with the specified credential; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool CanUse(MongoCredential credential)
        {
            if (!credential.Mechanism.Equals(Name, StringComparison.InvariantCultureIgnoreCase) || !(credential.Identity is MongoExternalIdentity))
            {
                return false;
            }
            if (__useGsasl)
            {
                // GSASL relies on kinit to work properly and hence, the evidence is external.
                return credential.Evidence is ExternalEvidence;
            }
            return true;
        }

        /// <summary>
        /// Initializes the mechanism.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        /// <returns>The initial step.</returns>
        public ISaslStep Initialize(MongoConnection connection, MongoCredential credential)
        {
            var serviceName = credential.GetMechanismProperty<string>("SERVICE_NAME", "mongodb");
            var realm = credential.GetMechanismProperty<string>("REALM", null);
            var canonicalizeHostname = credential.GetMechanismProperty<bool>("CANONICALIZE_HOST_NAME", false);

            var hostname = connection.ServerInstance.Address.Host;
            if (canonicalizeHostname)
            {
                var entry = Dns.GetHostEntry(hostname);
                if (entry != null)
                {
                    hostname = entry.HostName;
                }
            }

            // TODO: provide an override to force the use of gsasl?
            if (__useGsasl)
            {
                return new GsaslGssapiImplementation(
                    serviceName,
                    hostname,
                    realm,
                    credential.Username);
            }

            return new WindowsGssapiImplementation(
                serviceName,
                hostname,
                realm,
                credential.Username,
                credential.Evidence);
        }
    }
}