﻿/* Copyright 2018-present MongoDB Inc.
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    /// <summary>
    /// Represents a session.
    /// </summary>
    /// <seealso cref="MongoDB.Driver.Core.Bindings.ICoreSession" />
    public sealed class CoreSession : ICoreSession
    {
        // private fields
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IClusterInternal _cluster;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly IClusterClock _clusterClock = new ClusterClock();
        private CoreTransaction _currentTransaction;
        private bool _disposed;
        private bool _isCommitTransactionInProgress;
        private readonly IOperationClock _operationClock = new OperationClock();
        private readonly CoreSessionOptions _options;
        private readonly Lazy<ICoreServerSession> _serverSession;
        private BsonTimestamp _snapshotTime;

        // constructors
        internal CoreSession(
            IClusterInternal cluster,
            ICoreServerSession serverSession,
            CoreSessionOptions options)
        : this(cluster, options: options)
        {
            Ensure.IsNotNull(serverSession, nameof(serverSession));
            _serverSession = new Lazy<ICoreServerSession>(() => serverSession);
        }

        internal CoreSession(
            IClusterInternal cluster,
            ICoreServerSessionPool serverSessionPool,
            CoreSessionOptions options)
             : this(cluster, options)
        {
            Ensure.IsNotNull(serverSessionPool, nameof(serverSessionPool));
            _serverSession = new Lazy<ICoreServerSession>(() => serverSessionPool.AcquireSession());
        }

        private CoreSession(
           IClusterInternal cluster,
           CoreSessionOptions options)
        {
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
            _options = Ensure.IsNotNull(options, nameof(options));
        }

        // public properties
        /// <summary>
        /// Gets the cluster.
        /// </summary>
        /// <value>
        /// The cluster.
        /// </value>
        public ICluster Cluster => _cluster;

        /// <inheritdoc />
        public BsonDocument ClusterTime => _clusterClock.ClusterTime;

        /// <inheritdoc />
        public CoreTransaction CurrentTransaction => _currentTransaction;

        /// <inheritdoc />
        public BsonDocument Id => _serverSession.Value.Id;

        /// <inheritdoc />
        public bool IsCausallyConsistent => _options.IsCausallyConsistent;

        /// <inheritdoc />
        public bool IsDirty => _serverSession.Value.IsDirty;

        /// <inheritdoc />
        public bool IsImplicit => _options.IsImplicit;

        /// <inheritdoc />
        public bool IsInTransaction
        {
            get
            {
                if (_currentTransaction != null)
                {
                    switch (_currentTransaction.State)
                    {
                        case CoreTransactionState.Aborted:
                            return false;

                        case CoreTransactionState.Committed:
                            return _isCommitTransactionInProgress; // when retrying a commit we are temporarily "back in" the already committed transaction

                        default:
                            return true;
                    }
                }

                return false;
            }
        }

        /// <inheritdoc />
        public bool IsSnapshot => _options.IsSnapshot;

        /// <inheritdoc />
        public BsonTimestamp OperationTime => _operationClock.OperationTime;

        /// <inheritdoc />
        public CoreSessionOptions Options => _options;

        /// <inheritdoc />
        public ICoreServerSession ServerSession => _serverSession.Value;

        /// <inheritdoc />
        public BsonTimestamp SnapshotTime => _snapshotTime;

        // public methods
        /// <inheritdoc />
        public void AbortTransaction(CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureAbortTransactionCanBeCalled(nameof(AbortTransaction));

            // TODO: CSOT implement proper way to obtain the operationContext
            var operationContext = new OperationContext(null, cancellationToken);
            try
            {
                if (_currentTransaction.IsEmpty)
                {
                    return;
                }

                try
                {
                    var firstAttempt = CreateAbortTransactionOperation();
                    ExecuteEndTransactionOnPrimary(operationContext, firstAttempt);
                    return;
                }
                catch (Exception exception) when (ShouldRetryEndTransactionException(exception))
                {
                    // unpin if retryable error
                    _currentTransaction.UnpinAll();

                    // ignore exception and retry
                }
                catch
                {
                    return; // ignore exception and return
                }

                try
                {
                    var secondAttempt = CreateAbortTransactionOperation();
                    ExecuteEndTransactionOnPrimary(operationContext, secondAttempt);
                }
                catch
                {
                    return; // ignore exception and return
                }
            }
            finally
            {
                _currentTransaction.SetState(CoreTransactionState.Aborted);
                // The transaction is aborted.The session MUST be unpinned regardless
                // of whether the abortTransaction command succeeds or fails
                _currentTransaction.UnpinAll();
            }
        }

        /// <inheritdoc />
        public async Task AbortTransactionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureAbortTransactionCanBeCalled(nameof(AbortTransaction));

            // TODO: CSOT implement proper way to obtain the operationContext
            var operationContext = new OperationContext(null, cancellationToken);
            try
            {
                if (_currentTransaction.IsEmpty)
                {
                    return;
                }

                try
                {
                    var firstAttempt = CreateAbortTransactionOperation();
                    await ExecuteEndTransactionOnPrimaryAsync(operationContext, firstAttempt).ConfigureAwait(false);
                    return;
                }
                catch (Exception exception) when (ShouldRetryEndTransactionException(exception))
                {
                    // unpin if retryable error
                    _currentTransaction.UnpinAll();

                    // ignore exception and retry
                }
                catch
                {
                    return; // ignore exception and return
                }

                try
                {
                    var secondAttempt = CreateAbortTransactionOperation();
                    await ExecuteEndTransactionOnPrimaryAsync(operationContext, secondAttempt).ConfigureAwait(false);
                }
                catch
                {
                    return; // ignore exception and return
                }
            }
            finally
            {
                _currentTransaction.SetState(CoreTransactionState.Aborted);
                // The transaction is aborted.The session MUST be unpinned regardless
                // of whether the abortTransaction command succeeds or fails
                _currentTransaction.UnpinAll();
            }
        }

        /// <inheritdoc />
        public void AboutToSendCommand()
        {
            if (_currentTransaction != null)
            {
                switch (_currentTransaction.State)
                {
                    case CoreTransactionState.Starting: // Starting changes to InProgress after the message is sent to the server
                    case CoreTransactionState.InProgress:
                        return;

                    case CoreTransactionState.Aborted:
                        _currentTransaction = null;
                        break;

                    case CoreTransactionState.Committed:
                        // don't set to null when retrying a commit
                        if (!_isCommitTransactionInProgress)
                        {
                            // Unpin data non-transaction operation uses the commited session
                            _currentTransaction.UnpinAll();
                            _currentTransaction = null;
                        }
                        return;

                    default:
                        throw new Exception($"Unexpected transaction state: {_currentTransaction.State}.");
                }
            }
        }

        /// <inheritdoc />
        public void AdvanceClusterTime(BsonDocument newClusterTime)
        {
            _clusterClock.AdvanceClusterTime(newClusterTime);
        }

        /// <inheritdoc />
        public void AdvanceOperationTime(BsonTimestamp newOperationTime)
        {
            _operationClock.AdvanceOperationTime(newOperationTime);
        }

        /// <inheritdoc />
        public long AdvanceTransactionNumber()
        {
            return _serverSession.Value.AdvanceTransactionNumber();
        }

        /// <inheritdoc />
        public void CommitTransaction(CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureCommitTransactionCanBeCalled(nameof(CommitTransaction));

            // TODO: CSOT implement proper way to obtain the operationContext
            var operationContext = new OperationContext(null, cancellationToken);
            try
            {
                _isCommitTransactionInProgress = true;
                if (_currentTransaction.IsEmpty)
                {
                    return;
                }

                try
                {
                    var firstAttempt = CreateCommitTransactionOperation(IsFirstCommitAttemptRetry());
                    ExecuteEndTransactionOnPrimary(operationContext, firstAttempt);
                    return;
                }
                catch (Exception exception) when (ShouldRetryEndTransactionException(exception))
                {
                    // unpin server if needed, then ignore exception and retry
                    TransactionHelper.UnpinServerIfNeededOnRetryableCommitException(_currentTransaction, exception);
                }

                var secondAttempt = CreateCommitTransactionOperation(isCommitRetry: true);
                ExecuteEndTransactionOnPrimary(operationContext, secondAttempt);
            }
            finally
            {
                _isCommitTransactionInProgress = false;
                _currentTransaction.SetState(CoreTransactionState.Committed);
            }
        }

        /// <inheritdoc />
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureCommitTransactionCanBeCalled(nameof(CommitTransaction));

            // TODO: CSOT implement proper way to obtain the operationContext
            var operationContext = new OperationContext(null, cancellationToken);
            try
            {
                _isCommitTransactionInProgress = true;
                if (_currentTransaction.IsEmpty)
                {
                    return;
                }

                try
                {
                    var firstAttempt = CreateCommitTransactionOperation(IsFirstCommitAttemptRetry());
                    await ExecuteEndTransactionOnPrimaryAsync(operationContext, firstAttempt).ConfigureAwait(false);
                    return;
                }
                catch (Exception exception) when (ShouldRetryEndTransactionException(exception))
                {
                    // unpin server if needed, then ignore exception and retry
                    TransactionHelper.UnpinServerIfNeededOnRetryableCommitException(_currentTransaction, exception);
                }

                var secondAttempt = CreateCommitTransactionOperation(isCommitRetry: true);
                await ExecuteEndTransactionOnPrimaryAsync(operationContext, secondAttempt).ConfigureAwait(false);
            }
            finally
            {
                _isCommitTransactionInProgress = false;
                _currentTransaction.SetState(CoreTransactionState.Committed);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_currentTransaction != null)
                {
                    switch (_currentTransaction.State)
                    {
                        case CoreTransactionState.Starting:
                        case CoreTransactionState.InProgress:
                            try
                            {
                                AbortTransaction(CancellationToken.None);
                            }
                            catch
                            {
                                // ignore exceptions
                            }
                            break;
                    }
                }

                _currentTransaction?.UnpinAll();
                _serverSession.Value.Dispose();
                _disposed = true;
            }
        }

        /// <inheritdoc />
        public void MarkDirty()
        {
            _serverSession.Value.MarkDirty();
        }

        /// <inheritdoc />
        public void StartTransaction(TransactionOptions transactionOptions = null)
        {
            EnsureStartTransactionCanBeCalled();

            var transactionNumber = AdvanceTransactionNumber();
            var effectiveTransactionOptions = GetEffectiveTransactionOptions(transactionOptions);
            if (!effectiveTransactionOptions.WriteConcern.IsAcknowledged)
            {
                throw new InvalidOperationException("Transactions do not support unacknowledged write concerns.");
            }

            _currentTransaction?.UnpinAll(); // unpin data if any when a new transaction is started
            _currentTransaction = new CoreTransaction(transactionNumber, effectiveTransactionOptions);
        }

        /// <inheritdoc />
        public void SetSnapshotTimeIfNeeded(BsonTimestamp snapshotTime)
        {
            if (IsSnapshot && _snapshotTime == null)
            {
                _snapshotTime = snapshotTime;
            }
        }

        /// <inheritdoc />
        public void WasUsed()
        {
            _serverSession.Value.WasUsed();
        }

        // private methods
        private IReadOperation<BsonDocument> CreateAbortTransactionOperation()
        {
            return new AbortTransactionOperation(_currentTransaction.RecoveryToken, GetTransactionWriteConcern());
        }

        private IReadOperation<BsonDocument> CreateCommitTransactionOperation(bool isCommitRetry)
        {
            var writeConcern = GetCommitTransactionWriteConcern(isCommitRetry);
            var maxCommitTime = _currentTransaction.TransactionOptions.MaxCommitTime;
            return new CommitTransactionOperation(_currentTransaction.RecoveryToken, writeConcern) { MaxCommitTime = maxCommitTime };
        }

        private void EnsureAbortTransactionCanBeCalled(string methodName)
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException($"{methodName} cannot be called when no transaction started.");
            }

            switch (_currentTransaction.State)
            {
                case CoreTransactionState.Starting:
                case CoreTransactionState.InProgress:
                    return;

                case CoreTransactionState.Aborted:
                    throw new InvalidOperationException($"Cannot call {methodName} twice.");

                case CoreTransactionState.Committed:
                    throw new InvalidOperationException($"Cannot call {methodName} after calling CommitTransaction.");

                default:
                    throw new Exception($"{methodName} called in unexpected transaction state: {_currentTransaction.State}.");
            }
        }

        private void EnsureCommitTransactionCanBeCalled(string methodName)
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException($"{methodName} cannot be called when no transaction started.");
            }

            switch (_currentTransaction.State)
            {
                case CoreTransactionState.Starting:
                case CoreTransactionState.InProgress:
                case CoreTransactionState.Committed:
                    return;

                case CoreTransactionState.Aborted:
                    throw new InvalidOperationException($"Cannot call {methodName} after calling AbortTransaction.");

                default:
                    throw new Exception($"{methodName} called in unexpected transaction state: {_currentTransaction.State}.");
            }
        }

        private void EnsureStartTransactionCanBeCalled()
        {
            if (IsSnapshot)
            {
                throw new MongoClientException("Transactions are not supported in snapshot sessions.");
            }
            if (_currentTransaction == null)
            {
                EnsureTransactionsAreSupported();
            }
            else
            {
                switch (_currentTransaction.State)
                {
                    case CoreTransactionState.Aborted:
                    case CoreTransactionState.Committed:
                        break;

                    default:
                        throw new InvalidOperationException("Transaction already in progress.");
                }
            }
        }

        private void EnsureTransactionsAreSupported()
        {
            if (_cluster.Description.Type == ClusterType.LoadBalanced)
            {
                // LB always supports transactions
                return;
            }

            var connectedDataBearingServers = _cluster.Description.Servers.Where(s => s.State == ServerState.Connected && s.IsDataBearing).ToList();

            foreach (var connectedDataBearingServer in connectedDataBearingServers)
            {
                var serverType = connectedDataBearingServer.Type;

                switch (serverType)
                {
                    case ServerType.Standalone:
                        throw new NotSupportedException("Standalone servers do not support transactions.");
                    case ServerType.ShardRouter:
                        Feature.ShardedTransactions.ThrowIfNotSupported(connectedDataBearingServer.MaxWireVersion);
                        break;
                    case ServerType.LoadBalanced:
                        // do nothing, load balancing always supports transactions
                        break;
                    default:
                        Feature.Transactions.ThrowIfNotSupported(connectedDataBearingServer.MaxWireVersion);
                        break;
                }
            }
        }

        private TResult ExecuteEndTransactionOnPrimary<TResult>(OperationContext operationContext, IReadOperation<TResult> operation)
        {
            using (var sessionHandle = new NonDisposingCoreSessionHandle(this))
            using (var binding = ChannelPinningHelper.CreateReadWriteBinding(_cluster, sessionHandle))
            {
                return operation.Execute(operationContext, binding);
            }
        }

        private async Task<TResult> ExecuteEndTransactionOnPrimaryAsync<TResult>(OperationContext operationContext, IReadOperation<TResult> operation)
        {
            using (var sessionHandle = new NonDisposingCoreSessionHandle(this))
            using (var binding = ChannelPinningHelper.CreateReadWriteBinding(_cluster, sessionHandle))
            {
                return await operation.ExecuteAsync(operationContext, binding).ConfigureAwait(false);
            }
        }

        private TransactionOptions GetEffectiveTransactionOptions(TransactionOptions transactionOptions)
        {
            var readConcern = transactionOptions?.ReadConcern ?? _options.DefaultTransactionOptions?.ReadConcern ?? ReadConcern.Default;
            var readPreference = transactionOptions?.ReadPreference ?? _options.DefaultTransactionOptions?.ReadPreference ?? ReadPreference.Primary;
            var writeConcern = transactionOptions?.WriteConcern ?? _options.DefaultTransactionOptions?.WriteConcern ?? new WriteConcern();
            var maxCommitTime = transactionOptions?.MaxCommitTime ?? _options.DefaultTransactionOptions?.MaxCommitTime;
            return new TransactionOptions(readConcern, readPreference, writeConcern, maxCommitTime);
        }

        private WriteConcern GetTransactionWriteConcern()
        {
            return
                _currentTransaction.TransactionOptions?.WriteConcern ??
                _options.DefaultTransactionOptions?.WriteConcern ??
                WriteConcern.WMajority;
        }

        private WriteConcern GetCommitTransactionWriteConcern(bool isCommitRetry)
        {
            var writeConcern = GetTransactionWriteConcern();
            if (isCommitRetry)
            {
                writeConcern = writeConcern.With(mode: "majority");
                if (writeConcern.WTimeout == null)
                {
                    writeConcern = writeConcern.With(wTimeout: TimeSpan.FromMilliseconds(10000));
                }
            }

            return writeConcern;
        }

        private bool IsFirstCommitAttemptRetry()
        {
            // According to the spec, trying to commit again while the state is "committed" is considered a retry.
            return _currentTransaction.State == CoreTransactionState.Committed;
        }

        private bool ShouldRetryEndTransactionException(Exception exception)
        {
            return RetryabilityHelper.IsRetryableWriteException(exception);
        }
    }
}
