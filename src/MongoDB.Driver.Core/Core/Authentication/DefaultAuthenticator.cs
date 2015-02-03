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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// The default authenticator (uses SCRAM-SHA1 if possible, falls back to MONGODB-CR otherwise).
    /// </summary>
    public class DefaultAuthenticator : IAuthenticator
    {
        // static
        private static readonly SemanticVersion __scramVersionRequirement = new SemanticVersion(2, 7, 5);

        // fields
        private readonly UsernamePasswordCredential _credential;
        private readonly IRandomStringGenerator _randomStringGenerator;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAuthenticator"/> class.
        /// </summary>
        /// <param name="credential">The credential.</param>
        public DefaultAuthenticator(UsernamePasswordCredential credential)
            : this(credential, new RNGCryptoServiceProviderRandomStringGenerator())
        {
        }

        internal DefaultAuthenticator(UsernamePasswordCredential credential, IRandomStringGenerator randomStringGenerator)
        {
            _credential = Ensure.IsNotNull(credential, "credential");
            _randomStringGenerator = Ensure.IsNotNull(randomStringGenerator, "randomStringGenerator");
        }

        // properties
        /// <inheritdoc/>
        public string Name
        {
            get { return "DEFAULT"; }
        }

        // methods
        /// <inheritdoc/>
        public Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, "connection");
            Ensure.IsNotNull(description, "description");

            IAuthenticator authenticator;
            if (description.BuildInfoResult.ServerVersion >= __scramVersionRequirement)
            {
                authenticator = new ScramSha1Authenticator(_credential, _randomStringGenerator);
            }
            else
            {
                authenticator = new MongoDBCRAuthenticator(_credential);
            }

            return authenticator.AuthenticateAsync(connection, description, cancellationToken);
        }
    }
}