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
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// A client session handle.
    /// </summary>
    /// <seealso cref="MongoDB.Driver.IClientSessionHandle" />
    internal sealed class ClientSessionHandle : IClientSessionHandle, IClientSessionInternal
    {
        // private fields
        private readonly IMongoClient _client;
        private readonly IClock _clock;
        private readonly ICoreSessionHandle _coreSession;
        private bool _disposed;
        private readonly ClientSessionOptions _options;
        private readonly IRandom _random;
        private IServerSession _serverSession;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSessionHandle" /> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="options">The options.</param>
        /// <param name="coreSession">The wrapped session.</param>
        public ClientSessionHandle(IMongoClient client, ClientSessionOptions options, ICoreSessionHandle coreSession)
            : this(client, options, coreSession, SystemClock.Instance, DefaultRandom.Instance)
        {
        }

        internal ClientSessionHandle(IMongoClient client, ClientSessionOptions options, ICoreSessionHandle coreSession, IClock clock, IRandom random)
        {
            _client = client;
            _options = options;
            _coreSession = coreSession;
            _clock = clock;
            _random = random;
        }

        // public properties
        /// <inheritdoc />
        public IMongoClient Client => _client;

        /// <inheritdoc />
        public BsonDocument ClusterTime => _coreSession.ClusterTime;

        /// <inheritdoc />
        public bool IsImplicit => _coreSession.IsImplicit;

        /// <inheritdoc />
        public bool IsInTransaction => _coreSession.IsInTransaction;

        /// <inheritdoc />
        public BsonTimestamp OperationTime => _coreSession.OperationTime;

        /// <inheritdoc />
        public ClientSessionOptions Options => _options;

        /// <inheritdoc />
        public IServerSession ServerSession
        {
            get
            {
                if (_serverSession == null)
                {
                    _serverSession = new ServerSession(_coreSession.ServerSession);
                }

                return _serverSession;
            }
        }

        /// <inheritdoc />
        public ICoreSessionHandle WrappedCoreSession => _coreSession;

        // public methods
        /// <inheritdoc />
        public void AbortTransaction(CancellationToken cancellationToken = default)
            => _coreSession.AbortTransaction(cancellationToken);

        // TODO: CSOT: Make it public when CSOT will be ready for GA and add default value to cancellationToken parameter.
        void IClientSessionInternal.AbortTransaction(AbortTransactionOptions options, CancellationToken cancellationToken)
            => _coreSession.AbortTransaction(options, cancellationToken);

        /// <inheritdoc />
        public Task AbortTransactionAsync(CancellationToken cancellationToken = default)
            => _coreSession.AbortTransactionAsync(cancellationToken);

        // TODO: CSOT: Make it public when CSOT will be ready for GA and add default value to cancellationToken parameter.
        Task IClientSessionInternal.AbortTransactionAsync(AbortTransactionOptions options, CancellationToken cancellationToken)
            => _coreSession.AbortTransactionAsync(options, cancellationToken);

        /// <inheritdoc />
        public void AdvanceClusterTime(BsonDocument newClusterTime)
        {
            _coreSession.AdvanceClusterTime(newClusterTime);
        }

        /// <inheritdoc />
        public void AdvanceOperationTime(BsonTimestamp newOperationTime)
        {
            _coreSession.AdvanceOperationTime(newOperationTime);
        }

        /// <inheritdoc />
        public void CommitTransaction(CancellationToken cancellationToken = default)
            => _coreSession.CommitTransaction(cancellationToken);

        // TODO: CSOT: Make it public when CSOT will be ready for GA and add default value to cancellationToken parameter.
        void IClientSessionInternal.CommitTransaction(CommitTransactionOptions options, CancellationToken cancellationToken)
            => _coreSession.CommitTransaction(options, cancellationToken);

        /// <inheritdoc />
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
            => _coreSession.CommitTransactionAsync(cancellationToken);

        // TODO: CSOT: Make it public when CSOT will be ready for GA and add default value to cancellationToken parameter.
        Task IClientSessionInternal.CommitTransactionAsync(CommitTransactionOptions options, CancellationToken cancellationToken)
            => _coreSession.CommitTransactionAsync(options, cancellationToken);

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _coreSession.Dispose();
                _serverSession?.Dispose();
                _disposed = true;
            }
        }

        /// <inheritdoc />
        public IClientSessionHandle Fork()
        {
            return new ClientSessionHandle(_client, _options, _coreSession.Fork());
        }

        /// <inheritdoc />
        public void StartTransaction(TransactionOptions transactionOptions = null)
        {
            var effectiveTransactionOptions = GetEffectiveTransactionOptions(transactionOptions);

            var tracingOptions = _client?.Settings?.TracingOptions;
            var isTracingEnabled = tracingOptions == null || !tracingOptions.Disabled;

            ((ICoreSessionInternal)_coreSession).StartTransaction(effectiveTransactionOptions, isTracingEnabled);
        }

        /// <inheritdoc />
        public TResult WithTransaction<TResult>(Func<IClientSessionHandle, CancellationToken, TResult> callback, TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(callback, nameof(callback));

            return TransactionExecutor.ExecuteWithRetries(this, callback, transactionOptions, _clock, _random, cancellationToken);
        }

        /// <inheritdoc />
        public Task<TResult> WithTransactionAsync<TResult>(Func<IClientSessionHandle, CancellationToken, Task<TResult>> callbackAsync, TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(callbackAsync, nameof(callbackAsync));

            return TransactionExecutor.ExecuteWithRetriesAsync(this, callbackAsync, transactionOptions, _clock, _random, cancellationToken);
        }

        private TransactionOptions GetEffectiveTransactionOptions(TransactionOptions transactionOptions)
        {
            var defaultTransactionOptions = _options?.DefaultTransactionOptions;
            var readConcern = transactionOptions?.ReadConcern ?? defaultTransactionOptions?.ReadConcern ?? _client.Settings?.ReadConcern ?? ReadConcern.Default;
            var readPreference = transactionOptions?.ReadPreference ?? defaultTransactionOptions?.ReadPreference ?? _client.Settings?.ReadPreference ?? ReadPreference.Primary;
            var writeConcern = transactionOptions?.WriteConcern ?? defaultTransactionOptions?.WriteConcern ?? _client.Settings?.WriteConcern ?? new WriteConcern();
            var maxCommitTime = transactionOptions?.MaxCommitTime ?? defaultTransactionOptions?.MaxCommitTime;

            return new TransactionOptions(readConcern, readPreference, writeConcern, maxCommitTime);
        }
    }
}
