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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver.Communication.Security.Mechanisms;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Communication.Security
{
    /// <summary>
    /// Authenticates credentials against MongoDB.
    /// </summary>
    internal class Authenticator
    {
        // private static fields
        private static readonly List<IAuthenticationMethod> __clientSupportedMethods = new List<IAuthenticationMethod>
        {
            new SaslAuthenticationMethod(new GssapiMechanism()),
            new SaslAuthenticationMethod(new CramMD5Mechanism()),
            new SaslAuthenticationMethod(new DigestMD5Mechanism()),
            new MongoCRAuthenticationMethod() 
        };

        // private fields
        private readonly MongoConnection _connection;
        private readonly IEnumerable<MongoCredential> _credentials;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Authenticator" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credentials">The credentials.</param>
        public Authenticator(MongoConnection connection, IEnumerable<MongoCredential> credentials)
        {
            _connection = connection;
            _credentials = credentials;
        }

        // public methods
        /// <summary>
        /// Authenticates the specified connection.
        /// </summary>
        public void Authenticate()
        {
            if (!_credentials.Any())
            {
                return;
            }

            var serverSupportedMethods = GetServerSupportedMethods();
            foreach (var credential in _credentials)
            {
                Authenticate(credential, serverSupportedMethods);
            }
        }

        // private methods
        private void Authenticate(MongoCredential credential, List<string> serverSupportedMethods)
        {
            foreach (var clientSupportedMethod in __clientSupportedMethods)
            {
                if (serverSupportedMethods.Contains(clientSupportedMethod.Name) && clientSupportedMethod.CanUse(credential))
                {
                    clientSupportedMethod.Authenticate(_connection, credential);
                    return;
                }
            }

            var message = string.Format("Unable to negotiate a protocol to authenticate. Credential for source {0}, username {1} over protocol {2} could not be authenticated", credential.Source, credential.Username, credential.AuthenticationProtocol);
            throw new MongoSecurityException(message);
        }

        private List<string> GetServerSupportedMethods()
        {
            var command = new CommandDocument
            {
                { "saslStart", 1 },
                { "mechanism", ""}, // forces a response that contains a list of supported mechanisms...
                { "payload", new byte[0] }
            };

            var list = new List<string>();
            var result = _connection.RunCommand("admin", QueryFlags.SlaveOk, command, false);
            if (result.Response.Contains("supportedMechanisms"))
            {
                list.AddRange(result.Response["supportedMechanisms"].AsBsonArray.Select(x => x.AsString));
            }

            // because MONGO-CR is last in the list, we don't need to check if the server supports it...
            // in the future, we may need to add a check.
            list.Add("MONGO-CR");
            return list;
        }
    }

}