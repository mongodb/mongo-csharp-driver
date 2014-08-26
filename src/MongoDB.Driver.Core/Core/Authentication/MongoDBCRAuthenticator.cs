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
        private readonly UsernamePasswordCredential _credential;

        public MongoDBCRAuthenticator(UsernamePasswordCredential credential)
        {
            _credential = Ensure.IsNotNull(credential, "credential");
        }

        public string Name
        {
            get { return "MONGODB-CR"; }
        }

        public async Task AuthenticateAsync(IConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, "connection");

            try
            {
                var slidingTimeout = new SlidingTimeout(timeout);
                var nonce = await GetNonceAsync(connection, slidingTimeout, cancellationToken);
                await AuthenticateAsync(connection, nonce, slidingTimeout, cancellationToken);
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
            var protocol = new CommandWireProtocol(_credential.Source, command, true, null);
            var document = await protocol.ExecuteAsync(connection, timeout, cancellationToken);
            return (string)document["nonce"];
        }

        private async Task AuthenticateAsync(IConnection connection, string nonce, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var command = new BsonDocument
            {
                { "authenticate", 1 },
                { "user", _credential.Username },
                { "nonce", nonce },
                { "key", AuthenticationHelper.HexMD5(_credential.Username, _credential.Password, nonce) }
            };
            var protocol = new CommandWireProtocol(_credential.Source, command, true, null);
            await protocol.ExecuteAsync(connection, timeout, cancellationToken);
        }
    }
}