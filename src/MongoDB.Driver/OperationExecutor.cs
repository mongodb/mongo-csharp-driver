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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    internal sealed class OperationExecutor : IOperationExecutor
    {
        private readonly IMongoClient _client;

        public OperationExecutor(IMongoClient client)
        {
            _client = client;
        }

        public TResult ExecuteReadOperation<TResult>(
            IReadOperation<TResult> operation,
            ReadOperationOptions options,
            IClientSessionHandle session,
            CancellationToken cancellationToken)
        {
            bool isOwnSession = session == null;
            session ??= StartImplicitSession(cancellationToken);

            try
            {
                var readPreference = options.GetEffectiveReadPreference(session);
                using var binding = CreateReadBinding(session, readPreference);
                return operation.Execute(binding, cancellationToken);
            }
            finally
            {
                if (isOwnSession)
                {
                    session.Dispose();
                }
            }
        }

        public async Task<TResult> ExecuteReadOperationAsync<TResult>(
            IReadOperation<TResult> operation,
            ReadOperationOptions options,
            IClientSessionHandle session,
            CancellationToken cancellationToken)
        {
            bool isOwnSession = session == null;
            session ??= await StartImplicitSessionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var readPreference = options.GetEffectiveReadPreference(session);
                using var binding = CreateReadBinding(session, readPreference);
                return await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (isOwnSession)
                {
                    session.Dispose();
                }
            }
        }

        public TResult ExecuteWriteOperation<TResult>(
            IWriteOperation<TResult> operation,
            WriteOperationOptions options,
            IClientSessionHandle session,
            CancellationToken cancellationToken)
        {
            bool isOwnSession = session == null;
            session ??= StartImplicitSession(cancellationToken);

            try
            {
                using var binding = CreateReadWriteBinding(session);
                return operation.Execute(binding, cancellationToken);
            }
            finally
            {
                if (isOwnSession)
                {
                    session.Dispose();
                }
            }
        }

        public async Task<TResult> ExecuteWriteOperationAsync<TResult>(
            IWriteOperation<TResult> operation,
            WriteOperationOptions options,
            IClientSessionHandle session,
            CancellationToken cancellationToken)
        {
            bool isOwnSession = session == null;
            session ??= await StartImplicitSessionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                using var binding = CreateReadWriteBinding(session);
                return await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (isOwnSession)
                {
                    session.Dispose();
                }
            }
        }

        public IClientSessionHandle StartImplicitSession(CancellationToken cancellationToken)
            => StartImplicitSession();

        public Task<IClientSessionHandle> StartImplicitSessionAsync(CancellationToken cancellationToken)
            => Task.FromResult(StartImplicitSession());

        private IReadBindingHandle CreateReadBinding(IClientSessionHandle session, ReadPreference readPreference)
        {
            if (session.IsInTransaction && readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary)
            {
                throw new InvalidOperationException("Read preference in a transaction must be primary.");
            }

            // TODO: CreateReadBinding from MongoClient did not used ChannelPinningHelper, double-check if it's OK to start using it
            return ChannelPinningHelper.CreateReadBinding(_client.GetClusterInternal(), session.WrappedCoreSession.Fork(), readPreference);
        }

        private IReadWriteBindingHandle CreateReadWriteBinding(IClientSessionHandle session)
        {
            // TODO: CreateReadWriteBinding from MongoClient did not used ChannelPinningHelper, double-check if it's OK to start using it
            return ChannelPinningHelper.CreateReadWriteBinding(_client.GetClusterInternal(), session.WrappedCoreSession.Fork());
        }

        private IClientSessionHandle StartImplicitSession()
        {
            var options = new ClientSessionOptions { CausalConsistency = false, Snapshot = false };
            ICoreSessionHandle coreSession = _client.GetClusterInternal().StartSession(options.ToCore(isImplicit: true));
            return new ClientSessionHandle(_client, options, coreSession);
        }
    }
}
