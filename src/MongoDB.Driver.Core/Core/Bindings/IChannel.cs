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
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Bindings
{
    public interface IChannel : IDisposable
    {
        ConnectionDescription ConnectionDescription { get;  }

        Task<WriteConcernResult> DeleteAsync(DeleteWireProtocolArgs args, CancellationToken cancellationToken);
        Task<CursorBatch<TDocument>> GetMoreAsync<TDocument>(GetMoreWireProtocolArgs<TDocument> args, CancellationToken cancellationToken);
        Task<WriteConcernResult> InsertAsync<TDocument>(InsertWireProtocolArgs<TDocument> args, CancellationToken cancellationToken);
        Task KillCursorAsync(KillCursorsWireProtocolArgs args, CancellationToken cancellationToken);
        Task<CursorBatch<TDocument>> QueryAsync<TDocument>(QueryWireProtocolArgs<TDocument> args, CancellationToken cancellationToken);
        Task<TResult> RunCommandAsync<TResult>(CommandWireProtocolArgs<TResult> args, CancellationToken cancellationToken);
        Task<WriteConcernResult> UpdateAsync(UpdateWireProtocolArgs args, CancellationToken cancellationToken);
    }

    public interface IChannelHandle : IChannel
    {
        IChannelHandle Fork();
    }
}
