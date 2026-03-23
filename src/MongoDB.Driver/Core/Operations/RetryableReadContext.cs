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
        private bool _canBeRetried;
        private IRandom _random;

        public RetryableReadContext(IReadBinding binding, bool retryRequested, bool canBeRetried, IRandom random = null)
        {
            _binding = Ensure.IsNotNull(binding, nameof(binding));
            _retryRequested = retryRequested;
            _canBeRetried = canBeRetried;
            _random = random ?? DefaultRandom.Instance;
        }

        public IReadBinding Binding => _binding;
        public IChannelHandle Channel => _channel;
        public IChannelSourceHandle ChannelSource => _channelSource;
        /// <summary>
        /// This property only influences the retryability for retryable reads/writes and has no effect
        /// on client backpressure errors.
        /// </summary>
        public bool RetryRequested => _retryRequested;
        /// <summary>
        /// Indicates whether the operation can be retried. If false, retries are disabled entirely.
        /// </summary>
        public bool CanBeRetried => _canBeRetried;
        public IRandom Random => _random;

        public void Dispose()
        {
            if (!_disposed)
            {
                DisposeChannelAndSource();
                _disposed = true;
            }
        }

        public ServerDescription SelectServer(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
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

        public async Task<ServerDescription> SelectServerAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
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

        public void AcquireChannel(OperationContext operationContext)
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

        public async Task AcquireChannelAsync(OperationContext operationContext)
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
            _channelSource = null;
            _channel = null;
        }
    }
}
