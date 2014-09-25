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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Authentication
{
    public sealed class MongoDBCRAuthenticator : IAuthenticator
    {
        // static properties
        public static string MechanismName
        {
            get { return "MONGODB-CR"; }
        }

        // fields
        private readonly UsernamePasswordCredential _credential;

        // constructors
        public MongoDBCRAuthenticator(UsernamePasswordCredential credential)
        {
            _credential = Ensure.IsNotNull(credential, "credential");
        }

        // properties
        public string Name
        {
            get { return MechanismName; }
        }

        // methods
        public async Task AuthenticateAsync(IConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, "connection");

            try
            {
                var slidingTimeout = new SlidingTimeout(timeout);
                var nonce = await GetNonceAsync(connection, slidingTimeout, cancellationToken).ConfigureAwait(false);
                await AuthenticateAsync(connection, nonce, slidingTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch(MongoCommandException ex)
            {
                var message = string.Format("Unable to authenticate username '{0}' on database '{1}'.", _credential.Username, _credential.Source);
                throw new MongoAuthenticationException(message, ex);
            }
        }

        private async Task<string> GetNonceAsync(IConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var command = new BsonDocument("getnonce", 1);
            var protocol = new CommandWireProtocol(new DatabaseNamespace(_credential.Source), command, true, null);
            var document = await protocol.ExecuteAsync(connection, timeout, cancellationToken).ConfigureAwait(false);
            return (string)document["nonce"];
        }

        private async Task AuthenticateAsync(IConnection connection, string nonce, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var command = new BsonDocument
            {
                { "authenticate", 1 },
                { "user", _credential.Username },
                { "nonce", nonce },
                { "key", CreateKey(_credential.Username, _credential.Password, nonce) }
            };
            var protocol = new CommandWireProtocol(new DatabaseNamespace(_credential.Source), command, true, null);
            await protocol.ExecuteAsync(connection, timeout, cancellationToken).ConfigureAwait(false);
        }

        private string CreateKey(string username, SecureString password, string nonce)
        {
            var passwordDigest = AuthenticationHelper.MongoPasswordDigest(username, password);
            using (var md5 = MD5.Create())
            {
                var bytes = new UTF8Encoding(false, true).GetBytes(nonce + username + passwordDigest);
                bytes = md5.ComputeHash(bytes);
                return BsonUtils.ToHexString(bytes);
            }
        }
    }
}