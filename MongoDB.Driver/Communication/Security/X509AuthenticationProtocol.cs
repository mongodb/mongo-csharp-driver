using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// <exception cref="MongoAuthenticationException">Error getting nonce for authentication.</exception>
        public void Authenticate(MongoConnection connection, MongoCredential credential)
        {
            try
            {
                var command = new CommandDocument
                {
                    { "authenticate", 1},
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