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

        IChannelSourceHandle GetReadChannelSource(OperationContext operationContext);
        Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext);

        IChannelSourceHandle GetReadChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers);
        Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers);
    }

    internal interface IWriteBinding : IBinding
    {
        IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext);
        IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers);
        IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IMayUseSecondaryCriteria mayUseSecondary);
        IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary);

        Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext);
        Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers);
        Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IMayUseSecondaryCriteria mayUseSecondary);
        Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary);
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
