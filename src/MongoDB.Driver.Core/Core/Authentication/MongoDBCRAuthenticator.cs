/* Copyright 2013-2014 MongoDB Inc.
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
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// A MONGODB-CR authenticator.
    /// </summary>
    public sealed class MongoDBCRAuthenticator : IAuthenticator
    {
        // static properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        /// <value>
        /// The name of the mechanism.
        /// </value>
        public static string MechanismName
        {
            get { return "MONGODB-CR"; }
        }

        // fields
        private readonly UsernamePasswordCredential _credential;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBCRAuthenticator"/> class.
        /// </summary>
        /// <param name="credential">The credential.</param>
        public MongoDBCRAuthenticator(UsernamePasswordCredential credential)
        {
            _credential = Ensure.IsNotNull(credential, "credential");
        }

        // properties
        /// <inheritdoc/>
        public string Name
        {
            get { return MechanismName; }
        }

        // methods
        /// <inheritdoc/>
        public async Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, "connection");
            Ensure.IsNotNull(description, "description");

            try
            {
                var nonce = await GetNonceAsync(connection, cancellationToken).ConfigureAwait(false);
                await AuthenticateAsync(connection, nonce, cancellationToken).ConfigureAwait(false);
            }
            catch(MongoCommandException ex)
            {
                var message = string.Format("Unable to authenticate username '{0}' on database '{1}'.", _credential.Username, _credential.Source);
                throw new MongoAuthenticationException(connection.ConnectionId, message, ex);
            }
        }

        private async Task<string> GetNonceAsync(IConnection connection, CancellationToken cancellationToken)
        {
            var command = new BsonDocument("getnonce", 1);
            var protocol = new CommandWireProtocol<BsonDocument>(
                new DatabaseNamespace(_credential.Source),
                command,
                true,
                BsonDocumentSerializer.Instance,
                null);
            var document = await protocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
            return (string)document["nonce"];
        }

        private async Task AuthenticateAsync(IConnection connection, string nonce, CancellationToken cancellationToken)
        {
            var command = new BsonDocument
            {
                { "authenticate", 1 },
                { "user", _credential.Username },
                { "nonce", nonce },
                { "key", CreateKey(_credential.Username, _credential.Password, nonce) }
            };
            var protocol = new CommandWireProtocol<BsonDocument>(
                new DatabaseNamespace(_credential.Source),
                command,
                true,
                BsonDocumentSerializer.Instance,
                null);
            await protocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
        }

        private string CreateKey(string username, SecureString password, string nonce)
        {
            var passwordDigest = AuthenticationHelper.MongoPasswordDigest(username, password);
            using (var md5 = MD5.Create())
            {
                var bytes = Utf8Encodings.Strict.GetBytes(nonce + username + passwordDigest);
                bytes = md5.ComputeHash(bytes);
                return BsonUtils.ToHexString(bytes);
            }
        }
    }
}