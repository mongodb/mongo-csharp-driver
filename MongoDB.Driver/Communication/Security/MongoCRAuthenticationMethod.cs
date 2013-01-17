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
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Communication.Security
{
    /// <summary>
    /// Authenticates credentials using the MONGO-CR protocol.
    /// </summary>
    internal class MongoCRAuthenticationMethod : IAuthenticationMethod
    {
        // public properties
        public string Name
        {
            get { return "MONGO-CR"; }
        }

        // public methods
        /// <summary>
        /// Authenticates the connection against the given database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credentials">The credentials.</param>
        public void Authenticate(MongoConnection connection, MongoCredentials credentials)
        {
            var nonceCommand = new CommandDocument("getnonce", 1);
            var commandResult = connection.RunCommand(credentials.Source, QueryFlags.None, nonceCommand, false);
            if (!commandResult.Ok)
            {
                throw new MongoAuthenticationException(
                    "Error getting nonce for authentication.",
                    new MongoCommandException(commandResult));
            }

            var nonce = commandResult.Response["nonce"].AsString;
            var passwordDigest = MongoUtils.Hash(credentials.Username + ":mongo:" + ((PasswordEvidence)credentials.Evidence).Password);
            var digest = MongoUtils.Hash(nonce + credentials.Username + passwordDigest);
            var authenticateCommand = new CommandDocument
                {
                    { "authenticate", 1 },
                    { "user", credentials.Username },
                    { "nonce", nonce },
                    { "key", digest }
                };

            commandResult = connection.RunCommand(credentials.Source, QueryFlags.None, authenticateCommand, false);
            if (!commandResult.Ok)
            {
                var message = string.Format("Invalid credentials for database '{0}'.", credentials.Source);
                throw new MongoAuthenticationException(
                    message,
                    new MongoCommandException(commandResult));
            }
        }

        /// <summary>
        /// Determines whether this instance can use the specified credentials.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <returns>
        ///   <c>true</c> if this instance can use the specified credentials; otherwise, <c>false</c>.
        /// </returns>
        public bool CanUse(MongoCredentials credentials)
        {
            return credentials.AuthenticationProtocol == MongoAuthenticationProtocol.Strongest &&
                credentials.Evidence is PasswordEvidence;
        }
    }
}
