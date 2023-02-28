/* Copyright 2013-present MongoDB Inc.
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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Authentication;

namespace MongoDB.Driver.Core.Connections
{
    internal interface IConnectionInitializer
    {
        ConnectionDescription Authenticate(IConnection connection, (ConnectionDescription Description, IReadOnlyList<IAuthenticator> Authenticators) helloResult, CancellationToken cancellationToken);
        Task<ConnectionDescription> AuthenticateAsync(IConnection connection, (ConnectionDescription Description, IReadOnlyList<IAuthenticator> Authenticators) helloResult, CancellationToken cancellationToken);
        (ConnectionDescription Description, IReadOnlyList<IAuthenticator> Authenticators) SendHello(IConnection connection, CancellationToken cancellationToken);
        Task<(ConnectionDescription Description, IReadOnlyList<IAuthenticator> Authenticators)> SendHelloAsync(IConnection connection, CancellationToken cancellationToken);
    }
}
