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

using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Connections
{
    internal interface IConnectionInitializer
    {
        ConnectionDescription Handshake(IConnection connection, CancellationToken cancellationToken);
        ConnectionDescription ConnectionAuthentication(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken);

        Task<ConnectionDescription> HandshakeAsync(IConnection connection, CancellationToken cancellationToken);
        Task<ConnectionDescription> ConnectionAuthenticationAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken);
    }
}
