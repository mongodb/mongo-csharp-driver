﻿/* Copyright 2010-2013 10gen Inc.
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
        private static readonly List<IAuthenticationProtocol> __clientSupportedProtocols = new List<IAuthenticationProtocol>
        {
            // when we start negotiating, MONGODB-CR should be moved to the bottom of the list...
            new MongoCRAuthenticationProtocol(),
            new SaslAuthenticationProtocol(new GssapiMechanism()),
            new SaslAuthenticationProtocol(new PlainMechanism())
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

            foreach (var credential in _credentials)
            {
                Authenticate(credential);
            }
        }

        // private methods
        private void Authenticate(MongoCredential credential)
        {
            foreach (var clientSupportedProtocol in __clientSupportedProtocols)
            {
                if (clientSupportedProtocol.CanUse(credential))
                {
                    clientSupportedProtocol.Authenticate(_connection, credential);
                    return;
                }
            }

            var message = string.Format("Unable to find a protocol to authenticate. The credential for source {0}, username {1} over mechanism {2} could not be authenticated.", credential.Source, credential.Username, credential.Mechanism);
            throw new MongoSecurityException(message);
        }
    }

}
