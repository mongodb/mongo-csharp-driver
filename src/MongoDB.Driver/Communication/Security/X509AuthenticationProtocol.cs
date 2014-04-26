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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Operations;

namespace MongoDB.Driver.Communication.Security
{
    /// <summary>
    /// Authentication protocol using the SSL X509 certificates as the client identity.
    /// </summary>
    internal class X509AuthenticationProtocol : IAuthenticationProtocol
    {
        // public properties
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return "MONGODB-X509"; }
        }

        // public methods
        /// <summary>
        /// Authenticates the specified connection with the given credential.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        public void Authenticate(MongoConnection connection, MongoCredential credential)
        {
            try
            {
                var command = new CommandDocument
                {
                    { "authenticate", 1 },
                    { "mechanism", Name },
                    { "user", credential.Username }
                };
                RunCommand(connection, credential.Source, command);
            }
            catch (MongoCommandException ex)
            {
                throw new MongoAuthenticationException(string.Format("Unable to authenticate '{0}' using '{1}'.", credential.Username, Name), ex);
            }
        }

        /// <summary>
        /// Determines whether this instance can use the specified credential.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <returns>
        ///   <c>true</c> if this instance can use the specified credential; otherwise, <c>false</c>.
        /// </returns>
        public bool CanUse(MongoCredential credential)
        {
            return credential.Mechanism.Equals(Name, StringComparison.InvariantCultureIgnoreCase) &&
                credential.Identity is MongoExternalIdentity &&
                credential.Evidence is ExternalEvidence;
        }

        // private methods
        private CommandResult RunCommand(MongoConnection connection, string databaseName, IMongoCommand command)
        {
            var readerSettings = new BsonBinaryReaderSettings();
            var writerSettings = new BsonBinaryWriterSettings();
            var resultSerializer = BsonSerializer.LookupSerializer<CommandResult>();

            var commandOperation = new CommandOperation<CommandResult>(
                databaseName,
                readerSettings,
                writerSettings,
                command,
                QueryFlags.None,
                null, // options
                null, // readPreference
                resultSerializer);

            return commandOperation.Execute(connection);
        }
    }
}