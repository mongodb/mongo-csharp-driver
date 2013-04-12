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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Operations;

namespace MongoDB.Driver.Communication.Security
{
    /// <summary>
    /// Authenticates a credential using the MONGODB-CR protocol.
    /// </summary>
    internal class MongoCRAuthenticationProtocol : IAuthenticationProtocol
    {
        // public properties
        public string Name
        {
            get { return "MONGODB-CR"; }
        }

        // public methods
        /// <summary>
        /// Authenticates the connection against the given database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        public void Authenticate(MongoConnection connection, MongoCredential credential)
        {
            string nonce;
            try
            {
                var nonceCommand = new CommandDocument("getnonce", 1);
                var nonceResult = RunCommand(connection, credential.Source, nonceCommand);
                nonce = nonceResult.Response["nonce"].AsString;
            }
            catch (MongoCommandException ex)
            {
                throw new MongoAuthenticationException("Error getting nonce for authentication.", ex);
            }

            try
            {
                var passwordDigest = ((PasswordEvidence)credential.Evidence).ComputeMongoCRPasswordDigest(credential.Username);
                var digest = MongoUtils.Hash(nonce + credential.Username + passwordDigest);
                var authenticateCommand = new CommandDocument
                {
                    { "authenticate", 1 },
                    { "user", credential.Username },
                    { "nonce", nonce },
                    { "key", digest }
                };

                RunCommand(connection, credential.Source, authenticateCommand);
            }
            catch (MongoCommandException ex)
            {
                var message = string.Format("Invalid credential for database '{0}'.", credential.Source);
                throw new MongoAuthenticationException(message, ex);
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
            return credential.Mechanism.Equals("MONGODB-CR", StringComparison.InvariantCultureIgnoreCase) &&
                credential.Evidence is PasswordEvidence;
        }

        // private methods
        private CommandResult RunCommand(MongoConnection connection, string databaseName, IMongoCommand command)
        {
            var readerSettings = new BsonBinaryReaderSettings();
            var writerSettings = new BsonBinaryWriterSettings();
            var resultSerializer = BsonSerializer.LookupSerializer(typeof(CommandResult));

            var commandOperation = new CommandOperation<CommandResult>(
                databaseName,
                readerSettings,
                writerSettings,
                command,
                QueryFlags.None,
                null, // options
                null, // readPreference
                null, // serializationOptions
                resultSerializer);

            return commandOperation.Execute(connection);
        }
    }
}