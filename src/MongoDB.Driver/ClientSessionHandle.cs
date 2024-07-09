﻿/* Copyright 2017-present MongoDB Inc.
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
    internal sealed class ClientSessionHandle : IClientSessionHandle
    {
        // private fields
        private readonly IMongoClient _client;
        private readonly IClock _clock;
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly ICoreSessionHandle _coreSession;
#pragma warning restore CS0618 // Type or member is obsolete
        private bool _disposed;
        private readonly ClientSessionOptions _options;
        private IServerSession _serverSession;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSessionHandle" /> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="options">The options.</param>
        /// <param name="coreSession">The wrapped session.</param>
        [Obsolete]
        public ClientSessionHandle(IMongoClient client, ClientSessionOptions options, ICoreSessionHandle coreSession)
            : this(client, options, coreSession, SystemClock.Instance)
        {
        }

        [Obsolete]
        internal ClientSessionHandle(IMongoClient client, ClientSessionOptions options, ICoreSessionHandle coreSession, IClock clock)
        {
            _client = client;
            _options = options;
            _coreSession = coreSession;
            _clock = clock;
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
        [Obsolete("This property will be removed in later release.")]
        public ICoreSessionHandle WrappedCoreSession => _coreSession;

        // public methods
        /// <inheritdoc />
        public void AbortTransaction(CancellationToken cancellationToken = default(CancellationToken))
        {
            _coreSession.AbortTransaction(cancellationToken);
        }

        /// <inheritdoc />
        public Task AbortTransactionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _coreSession.AbortTransactionAsync(cancellationToken);
        }

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
        public void CommitTransaction(CancellationToken cancellationToken = default(CancellationToken))
        {
            _coreSession.CommitTransaction(cancellationToken);
        }

        /// <inheritdoc />
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _coreSession.CommitTransactionAsync(cancellationToken);
        }

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
#pragma warning disable CS0612 // Type or member is obsolete
            return new ClientSessionHandle(_client, _options, _coreSession.Fork());
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <inheritdoc />
        public void StartTransaction(TransactionOptions transactionOptions = null)
        {
            var effectiveTransactionOptions = GetEffectiveTransactionOptions(transactionOptions);
            _coreSession.StartTransaction(effectiveTransactionOptions);
        }

        /// <inheritdoc />
        public TResult WithTransaction<TResult>(Func<IClientSessionHandle, CancellationToken, TResult> callback, TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(callback, nameof(callback));

            return TransactionExecutor.ExecuteWithRetries(this, callback, transactionOptions, _clock, cancellationToken);
        }

        /// <inheritdoc />
        public Task<TResult> WithTransactionAsync<TResult>(Func<IClientSessionHandle, CancellationToken, Task<TResult>> callbackAsync, TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(callbackAsync, nameof(callbackAsync));

            return TransactionExecutor.ExecuteWithRetriesAsync(this, callbackAsync, transactionOptions, _clock, cancellationToken);
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
