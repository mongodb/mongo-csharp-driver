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
    internal sealed class RetryableWriteContext : IDisposable
    {
        #region static

        public static RetryableWriteContext Create(OperationContext operationContext, IWriteBinding binding, bool retryRequested)
        {
            var context = new RetryableWriteContext(binding, retryRequested);
            return context;
        }
        #endregion

#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IWriteBinding _binding;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private IChannelHandle _channel;
        private IChannelSourceHandle _channelSource;
        private bool _disposed;
        private bool _retryRequested;

        public RetryableWriteContext(IWriteBinding binding, bool retryRequested)
        {
            _binding = Ensure.IsNotNull(binding, nameof(binding));
            _retryRequested = retryRequested;
        }

        public IWriteBinding Binding => _binding;
        public IChannelHandle Channel => _channel;
        public IChannelSourceHandle ChannelSource => _channelSource;
        public bool RetryRequested => _retryRequested;

        public void Dispose()
        {
            if (!_disposed)
            {
                _channelSource?.Dispose();
                _channel?.Dispose();
                _disposed = true;
            }
        }

        public void AcquireOrReplaceChannel(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            try
            {
                operationContext.ThrowIfTimedOutOrCanceled();
                var writeChannelSource = Binding.GetWriteChannelSource(operationContext, deprioritizedServers);
                ReplaceChannelSource(writeChannelSource);
                ReplaceChannel(ChannelSource.GetChannel(operationContext));

                ChannelPinningHelper.PinChannellIfRequired(ChannelSource, Channel, Binding.Session);
            }
            catch
            {
                _channelSource?.Dispose();
                _channel?.Dispose();
                throw;
            }
        }

        public async Task AcquireOrReplaceChannelAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            try
            {
                operationContext.ThrowIfTimedOutOrCanceled();
                var writeChannelSource = await Binding
                    .GetWriteChannelSourceAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                ReplaceChannelSource(writeChannelSource);
                ReplaceChannel(await ChannelSource.GetChannelAsync(operationContext).ConfigureAwait(false));

                ChannelPinningHelper.PinChannellIfRequired(ChannelSource, Channel, Binding.Session);
            }
            catch
            {
                _channelSource?.Dispose();
                _channel?.Dispose();
                throw;
            }
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
    }
}
