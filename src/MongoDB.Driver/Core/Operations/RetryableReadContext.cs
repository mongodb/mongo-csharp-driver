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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class RetryableReadContext : IDisposable
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IReadBinding _binding;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private IChannelHandle _channel;
        private IChannelSourceHandle _channelSource;
        private bool _disposed;
        private bool _retryRequested;

        public RetryableReadContext(IReadBinding binding, bool retryRequested)
        {
            _binding = Ensure.IsNotNull(binding, nameof(binding));
            _retryRequested = retryRequested;
        }

        public IReadBinding Binding => _binding;
        public IChannelHandle Channel => _channel;
        public IChannelSourceHandle ChannelSource => _channelSource;
        public bool RetryRequested => _retryRequested;

        public void Dispose()
        {
            if (!_disposed)
            {
                DisposeChannelAndSource();
                _disposed = true;
            }
        }

        public ServerDescription DoServerSelection(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            try
            {
                operationContext.ThrowIfTimedOutOrCanceled();
                var readChannelSource = Binding.GetReadChannelSource(operationContext, deprioritizedServers);
                ReplaceChannelSource(readChannelSource);
                return ChannelSource.ServerDescription;
            }
            catch
            {
                DisposeChannelAndSource();
                throw;
            }
        }

        public async Task<ServerDescription> DoServerSelectionAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            try
            {
                operationContext.ThrowIfTimedOutOrCanceled();
                var readChannelSource = await Binding
                    .GetReadChannelSourceAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                ReplaceChannelSource(readChannelSource);
                return ChannelSource.ServerDescription;
            }
            catch
            {
                DisposeChannelAndSource();
                throw;
            }
        }

        public void DoChannelAcquisition(OperationContext operationContext)
        {
            try
            {
                if (_channelSource is null)
                {
                    throw new InvalidOperationException("Channel source is not initialized. Server selection must be performed before channel acquisition.");
                }
                operationContext.ThrowIfTimedOutOrCanceled();
                ReplaceChannel(ChannelSource.GetChannel(operationContext));
                ChannelPinningHelper.PinChannellIfRequired(ChannelSource, Channel, Binding.Session);
            }
            catch
            {
                DisposeChannelAndSource();
                throw;
            }
        }

        public async Task DoChannelAcquisitionAsync(OperationContext operationContext)
        {
            try
            {
                if (_channelSource is null)
                {
                    throw new InvalidOperationException("Channel source is not initialized. Server selection must be performed before channel acquisition.");
                }
                operationContext.ThrowIfTimedOutOrCanceled();
                ReplaceChannel(await ChannelSource.GetChannelAsync(operationContext).ConfigureAwait(false));
                ChannelPinningHelper.PinChannellIfRequired(ChannelSource, Channel, Binding.Session);
            }
            catch
            {
                DisposeChannelAndSource();
                throw;
            }
        }

        //TODO We can proably remove those, are used only by tests now
        public void AcquireOrReplaceChannel(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            DoServerSelection(operationContext, deprioritizedServers);
            DoChannelAcquisition(operationContext);
        }

        public async Task AcquireOrReplaceChannelAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            await DoServerSelectionAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
            await DoChannelAcquisitionAsync(operationContext).ConfigureAwait(false);
        }

        private void ReplaceChannel(IChannelHandle channel)
        {
            Ensure.IsNotNull(channel, nameof(channel));
            _channel?.Dispose();
            _channel = channel;
        }

        private void ReplaceChannelSource(IChannelSourceHandle channelSource)
        {
            Ensure.IsNotNull(channelSource, nameof(channelSource));
            _channelSource?.Dispose();
            _channel?.Dispose();
            _channelSource = channelSource;
            _channel = null;
        }

        private void DisposeChannelAndSource()
        {
            _channelSource?.Dispose();
            _channel?.Dispose();
        }
    }
}
