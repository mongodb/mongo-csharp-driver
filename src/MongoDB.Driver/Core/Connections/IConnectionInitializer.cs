/* Copyright 2010-present MongoDB Inc.
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

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Connections
{
    internal sealed class ConnectionInitializerContext
    {
        public ConnectionInitializerContext(ConnectionDescription description, IAuthenticator authenticator)
        {
            Description = Ensure.IsNotNull(description, nameof(description));
            Authenticator = authenticator;
        }

        public IAuthenticator Authenticator { get; }
        public ConnectionDescription Description { get; }
    }

    internal interface IConnectionInitializer
    {
        ConnectionInitializerContext Authenticate(IConnection connection, ConnectionInitializerContext connectionInitializerContext, CancellationToken cancellationToken);
        Task<ConnectionInitializerContext> AuthenticateAsync(IConnection connection, ConnectionInitializerContext connectionInitializerContext, CancellationToken cancellationToken);
        ConnectionInitializerContext SendHello(IConnection connection, CancellationToken cancellationToken);
        Task<ConnectionInitializerContext> SendHelloAsync(IConnection connection, CancellationToken cancellationToken);
    }
}
