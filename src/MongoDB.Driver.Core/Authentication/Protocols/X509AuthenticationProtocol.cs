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
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Authentication.Protocols
{
    public class X509AuthenticationProtocol : IAuthenticationProtocol
    {
        // properties
        public string Name
        {
            get { return "MONGODB-X509"; }
        }

        // methods
        public async Task AuthenticateAsync(IRootConnection connection, ICredential credential, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var x509Credential = (X509Credential)credential;
            await AuthenticateAsync(connection, x509Credential, timeout, cancellationToken);
        }

        private async Task AuthenticateAsync(IRootConnection connection, X509Credential credential, TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                var command = new BsonDocument
                {
                    { "authenticate", 1 },
                    { "mechanism", Name },
                    { "user", credential.Username }
                };
                var protocol = new CommandWireProtocol("$external", command, true);
                var result = await protocol.ExecuteAsync(connection, timeout, cancellationToken);
            }
            catch (CommandException ex)
            {
                var message = string.Format("Invalid credential for username '{0}' using protocol '{1}'.", credential.Username, Name);
                throw new AuthenticationException(message, ex);
            }
        }

        public bool CanUse(ICredential credential)
        {
            return credential.GetType() == typeof(X509Credential);
        }
    }
}
