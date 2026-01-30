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

        public static RetryableWriteContext Create(OperationContext operationContext, IWriteBinding binding, bool retryRequested, IMayUseSecondaryCriteria mayUseSecondaryCriteria = null)
        {
            var context = new RetryableWriteContext(binding, retryRequested, mayUseSecondaryCriteria: mayUseSecondaryCriteria);
            try
            {
                context.AcquireOrReplaceChannel(operationContext, null, mayUseSecondaryCriteria);
            }
            catch
            {
                context.Dispose();
                throw;
            }

            ChannelPinningHelper.PinChannellIfRequired(context.ChannelSource, context.Channel, context.Binding.Session);
            return context;
        }

        public static async Task<RetryableWriteContext> CreateAsync(OperationContext operationContext, IWriteBinding binding, bool retryRequested, IMayUseSecondaryCriteria mayUseSecondaryCriteria = null)
        {
            var context = new RetryableWriteContext(binding, retryRequested, mayUseSecondaryCriteria: mayUseSecondaryCriteria);
            try
            {
                await context.AcquireOrReplaceChannelAsync(operationContext, null, mayUseSecondaryCriteria).ConfigureAwait(false);
            }
            catch
            {
                context.Dispose();
                throw;
            }

            ChannelPinningHelper.PinChannellIfRequired(context.ChannelSource, context.Channel, context.Binding.Session);
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
        private IRandom _random;
        private IMayUseSecondaryCriteria _mayUseSecondaryCriteria;

        public RetryableWriteContext(IWriteBinding binding, bool retryRequested, IRandom random = null, IMayUseSecondaryCriteria mayUseSecondaryCriteria = null)
        {
            _binding = Ensure.IsNotNull(binding, nameof(binding));
            _retryRequested = retryRequested;
            _random = random ?? DefaultRandom.Instance;
            _mayUseSecondaryCriteria = mayUseSecondaryCriteria;
        }

        public IWriteBinding Binding => _binding;
        public IChannelHandle Channel => _channel;
        public IChannelSourceHandle ChannelSource => _channelSource;
        public IMayUseSecondaryCriteria MayUseSecondaryCriteria => _mayUseSecondaryCriteria;
        public IRandom Random => _random;
        /// <summary>
        /// This property only influences the retryability for retryable reads/writes and has no effect
        /// on client backpressure errors.
        /// </summary>
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

        //TODO Do this inside the main loop, but remember that this follows reads retryability logic, even with writes
        public void AcquireOrReplaceChannel(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondaryCriteria = null)
        {
            var attempt = 1;
            while (true)
            {
                operationContext.ThrowIfTimedOutOrCanceled();
                var writeChannelSource = mayUseSecondaryCriteria == null  //TODO The implementation of those two overloads is different, I'm worried there some important difference I can't appreciate
                    ? Binding.GetWriteChannelSource(operationContext, deprioritizedServers)
                    : Binding.GetWriteChannelSource(operationContext, deprioritizedServers, mayUseSecondaryCriteria);
                ReplaceChannelSource(writeChannelSource);
                var server = ChannelSource.ServerDescription;
                try
                {
                    ReplaceChannel(ChannelSource.GetChannel(operationContext));
                    return;
                }
                catch (Exception ex) when (RetryableWriteOperationExecutor.ShouldConnectionAcquireBeRetried(operationContext, this, server, ex, attempt))
                {
                    attempt++;
                }
            }
        }

        public async Task AcquireOrReplaceChannelAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondaryCriteria = null)
        {
            var attempt = 1;
            while (true)
            {
                operationContext.ThrowIfTimedOutOrCanceled();
                var writeChannelSource = mayUseSecondaryCriteria == null  //TODO The implementation of those two overloads is different, I'm worried there some important difference I can't appreciate
                    ? await Binding.GetWriteChannelSourceAsync(operationContext, deprioritizedServers).ConfigureAwait(false)
                    : await Binding.GetWriteChannelSourceAsync(operationContext, deprioritizedServers, mayUseSecondaryCriteria).ConfigureAwait(false);
                ReplaceChannelSource(writeChannelSource);
                var server = ChannelSource.ServerDescription;
                try
                {
                    ReplaceChannel(await ChannelSource.GetChannelAsync(operationContext).ConfigureAwait(false));
                    return;
                }
                catch (Exception ex) when (RetryableWriteOperationExecutor.ShouldConnectionAcquireBeRetried(operationContext, this, server, ex, attempt))
                {
                    attempt++;
                }
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
