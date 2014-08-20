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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.WireProtocol
{
    public class KillCursorsWireProtocol : IWireProtocol
    {
        // fields
        private readonly IReadOnlyList<long> _cursorIds;

        // constructors
        public KillCursorsWireProtocol(IEnumerable<long> cursorIds)
        {
            Ensure.IsNotNull(cursorIds, "cursorIds");
            _cursorIds = (cursorIds as IReadOnlyList<long>) ?? cursorIds.ToList();
        }

        // methods
        private KillCursorsMessage CreateMessage()
        {
            return new KillCursorsMessage(RequestMessage.GetNextRequestId(), _cursorIds);
        }

        public async Task ExecuteAsync(IConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var message = CreateMessage();
            await connection.SendMessageAsync(message, timeout, cancellationToken);
        }
    }
}
