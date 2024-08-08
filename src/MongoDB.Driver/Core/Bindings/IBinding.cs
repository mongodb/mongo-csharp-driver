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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    internal interface IBinding : IDisposable
    {
        ICoreSessionHandle Session { get; }
    }

    internal interface IReadBinding : IBinding
    {
        ReadPreference ReadPreference { get; }

        IChannelSourceHandle GetReadChannelSource(CancellationToken cancellationToken);
        Task<IChannelSourceHandle> GetReadChannelSourceAsync(CancellationToken cancellationToken);

        IChannelSourceHandle GetReadChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, CancellationToken cancellationToken);
        Task<IChannelSourceHandle> GetReadChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, CancellationToken cancellationToken);
    }

    internal interface IWriteBinding : IBinding
    {
        IChannelSourceHandle GetWriteChannelSource(CancellationToken cancellationToken);
        IChannelSourceHandle GetWriteChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, CancellationToken cancellationToken);
        IChannelSourceHandle GetWriteChannelSource(IMayUseSecondaryCriteria mayUseSecondary, CancellationToken cancellationToken);
        IChannelSourceHandle GetWriteChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary, CancellationToken cancellationToken);

        Task<IChannelSourceHandle> GetWriteChannelSourceAsync(CancellationToken cancellationToken);
        Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, CancellationToken cancellationToken);
        Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IMayUseSecondaryCriteria mayUseSecondary, CancellationToken cancellationToken);
        Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary, CancellationToken cancellationToken);
    }

    internal interface IReadWriteBinding : IReadBinding, IWriteBinding
    {
    }

    internal interface IReadBindingHandle : IReadBinding
    {
        IReadBindingHandle Fork();
    }

    internal interface IWriteBindingHandle : IWriteBinding
    {
        IWriteBindingHandle Fork();
    }

    internal interface IReadWriteBindingHandle : IReadWriteBinding, IReadBindingHandle, IWriteBindingHandle
    {
        new IReadWriteBindingHandle Fork();
    }

    internal interface IMayUseSecondaryCriteria
    {
        ReadPreference EffectiveReadPreference { get; set; }
        ReadPreference ReadPreference { get; }

        bool CanUseSecondary(ServerDescription server);
    }
}
