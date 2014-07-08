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
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication.Credentials;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Authentication.Protocols
{
    public class OriginalAuthenticationProtocol : IAuthenticationProtocol
    {
        // properties
        public string Name
        {
            get { return "MONGODB-CR"; }
        }

        // methods
        public async Task AuthenticateAsync(IRootConnection connection, ICredential credential, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            var usernamePasswordCredential = (UsernamePasswordCredential)credential;
            var nonce = await GetNonceAsync(connection, usernamePasswordCredential, slidingTimeout, cancellationToken);
            await AuthenticateAsync(connection, usernamePasswordCredential, nonce, slidingTimeout, cancellationToken);
        }

        private async Task AuthenticateAsync(IRootConnection connection, UsernamePasswordCredential credential, string nonce, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var command = new BsonDocument
            {
                { "authenticate", 1 },
                { "user", credential.Username },
                { "nonce", nonce },
                { "key", HexMD5(credential, nonce) }
            };
            var protocol = new CommandWireProtocol(credential.Source, command, true);
            var result = await protocol.ExecuteAsync(connection, timeout, cancellationToken);

            if (!result.GetValue("ok", false).ToBoolean())
            {
                var message = string.Format("Invalid credential for username '{0}' on database '{1}'.", credential.Username, credential.Source);
                throw new AuthenticationException(message);
            }
        }

        public bool CanUse(ICredential credential)
        {
            return credential.GetType() == typeof(UsernamePasswordCredential);
        }

        private async Task<string> GetNonceAsync(IRootConnection connection, UsernamePasswordCredential credential, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var command = new BsonDocument("getnonce", 1);
            var protocol = new CommandWireProtocol(credential.Source, command, true);
            var document = await protocol.ExecuteAsync(connection, timeout, cancellationToken);
            if (!document.GetValue("ok", false).ToBoolean())
            {
                throw new AuthenticationException("getnonce failed.");
            }
            return (string)document["nonce"];
        }

        private string HexMD5(MD5 md5, string value, UTF8Encoding encoding)
        {
            var bytes = encoding.GetBytes(value);
            var hash = md5.ComputeHash(bytes);
            return BsonUtils.ToHexString(hash);
        }

        private string HexMD5(UsernamePasswordCredential credential, string nonce)
        {
            using (var md5 = MD5.Create())
            {
                var encoding = new UTF8Encoding(false, true);
                var passwordDigest = HexMD5(md5, credential.Username + ":mongo:" + credential.Password, encoding);
                return HexMD5(md5, nonce + credential.Username + passwordDigest, encoding);
            }
        }
    }
}
