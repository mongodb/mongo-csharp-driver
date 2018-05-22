/* Copyright 2018-present MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core.Bindings
{
    /// <summary>
    /// Represents a session.
    /// </summary>
    /// <seealso cref="MongoDB.Driver.Core.Bindings.ICoreSession" />
    public sealed class CoreSession : ICoreSession
    {
        // private fields
        private readonly ICluster _cluster;
        private readonly IClusterClock _clusterClock = new ClusterClock();
        private CoreTransaction _currentTransaction;
        private bool _disposed;
        private readonly IOperationClock _operationClock = new OperationClock();
        private readonly CoreSessionOptions _options;
        private readonly ICoreServerSession _serverSession;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CoreSession" /> class.
        /// </summary>
        /// <param name="cluster">The cluster.</param>
        /// <param name="serverSession">The server session.</param>
        /// <param name="options">The options.</param>
        public CoreSession(
            ICluster cluster,
            ICoreServerSession serverSession,
            CoreSessionOptions options)
        {
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
            _serverSession = Ensure.IsNotNull(serverSession, nameof(serverSession));
            _options = Ensure.IsNotNull(options, nameof(options));
        }

        // public properties
        /// <inheritdoc />
        public ICluster Cluster => _cluster;

        /// <inheritdoc />
        public BsonDocument ClusterTime => _clusterClock.ClusterTime;

        /// <inheritdoc />
        public CoreTransaction CurrentTransaction => _currentTransaction;

        /// <inheritdoc />
        public BsonDocument Id => _serverSession.Id;

        /// <inheritdoc />
        public bool IsCausallyConsistent => _options.IsCausallyConsistent;

        /// <inheritdoc />
        public bool IsImplicit => _options.IsImplicit;

        /// <inheritdoc />
        public bool IsInTransaction => _currentTransaction != null;

        /// <inheritdoc />
        public BsonTimestamp OperationTime => _operationClock.OperationTime;

        /// <inheritdoc />
        public CoreSessionOptions Options => _options;

        /// <inheritdoc />
        public ICoreServerSession ServerSession => _serverSession;

        // public methods
        /// <inheritdoc />
        public void AbortTransaction(CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureIsInTransaction(nameof(AbortTransaction));

            try
            {
                if (_currentTransaction.StatementId == 0)
                {
                    return;
                }

                try
                {
                    var firstAttempt = CreateAbortTransactionOperation();
                    ExecuteEndTransactionOnPrimary(firstAttempt, cancellationToken);
                    return;
                }
                catch (Exception exception) when (ShouldIgnoreAbortTransactionException(exception))
                {
                    return; // ignore exception and return
                }
                catch (Exception exception) when (ShouldRetryEndTransactionException(exception))
                {
                    // ignore exception and retry
                }
                catch
                {
                    return; // ignore exception and return
                }

                try
                {
                    var secondAttempt = CreateAbortTransactionOperation();
                    ExecuteEndTransactionOnPrimary(secondAttempt, cancellationToken);
                }
                catch
                {
                    return; // ignore exception and return
                }
            }
            finally
            {
                _currentTransaction = null;
            }
        }

        /// <inheritdoc />
        public async Task AbortTransactionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureIsInTransaction(nameof(AbortTransaction));

            try
            {
                if (_currentTransaction.StatementId == 0)
                {
                    return;
                }

                try
                {
                    var firstAttempt = CreateAbortTransactionOperation();
                    await ExecuteEndTransactionOnPrimaryAsync(firstAttempt, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception exception) when (ShouldIgnoreAbortTransactionException(exception))
                {
                    return; // ignore exception and return
                }
                catch (Exception exception) when (ShouldRetryEndTransactionException(exception))
                {
                    // ignore exception and retry
                }
                catch
                {
                    return; // ignore exception and return
                }

                try
                {
                    var secondAttempt = CreateAbortTransactionOperation();
                    await ExecuteEndTransactionOnPrimaryAsync(secondAttempt, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    return; // ignore exception and return
                }
            }
            finally
            {
                _currentTransaction = null;
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
            return _serverSession.AdvanceTransactionNumber();
        }

        /// <inheritdoc />
        public void CommitTransaction(CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureIsInTransaction(nameof(CommitTransaction));

            try
            {
                if (_currentTransaction.StatementId == 0)
                {
                    return;
                }

                try
                {
                    var firstAttempt = CreateCommitTransactionOperation();
                    ExecuteEndTransactionOnPrimary(firstAttempt, cancellationToken);
                    return;
                }
                catch (Exception exception) when (ShouldRetryEndTransactionException(exception))
                {
                    // ignore exception and retry
                }

                var secondAttempt = CreateCommitTransactionOperation();
                ExecuteEndTransactionOnPrimary(secondAttempt, cancellationToken);
            }
            finally
            {
                _currentTransaction = null;
            }
        }

        /// <inheritdoc />
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureIsInTransaction(nameof(CommitTransaction));

            try
            {
                if (_currentTransaction.StatementId == 0)
                {
                    return;
                }

                try
                {
                    var firstAttempt = CreateCommitTransactionOperation();
                    await ExecuteEndTransactionOnPrimaryAsync(firstAttempt, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception exception) when (ShouldRetryEndTransactionException(exception))
                {
                    // ignore exception and retry
                }

                var secondAttempt = CreateCommitTransactionOperation();
                await ExecuteEndTransactionOnPrimaryAsync(secondAttempt, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _currentTransaction = null;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_currentTransaction != null)
                {
                    try
                    {
                        AbortTransaction(CancellationToken.None);
                    }
                    catch
                    {
                        // ignore exceptions
                    }
                }

                _serverSession.Dispose();
                _disposed = true;
            }
        }

        /// <inheritdoc />
        public void StartTransaction(TransactionOptions transactionOptions = null)
        {
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("Transaction already in progress.");
            }

            var transactionNumber = AdvanceTransactionNumber();
            var effectiveTransactionOptions = GetEffectiveTransactionOptions(transactionOptions);
            var transaction = new CoreTransaction(transactionNumber, effectiveTransactionOptions);

            _currentTransaction = transaction;
        }

        /// <inheritdoc />
        public void WasUsed()
        {
            _serverSession.WasUsed();
        }

        // private methods
        private IReadOperation<BsonDocument> CreateAbortTransactionOperation()
        {
            return new AbortTransactionOperation(GetTransactionWriteConcern());
        }

        private IReadOperation<BsonDocument> CreateCommitTransactionOperation()
        {
            return new CommitTransactionOperation(GetTransactionWriteConcern());
        }

        private void EnsureIsInTransaction(string methodName)
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No transaction started.");
            }
        }

        private TResult ExecuteEndTransactionOnPrimary<TResult>(IReadOperation<TResult> operation, CancellationToken cancellationToken)
        {
            using (var sessionHandle = new NonDisposingCoreSessionHandle(this))
            using (var binding = new WritableServerBinding(_cluster, sessionHandle))
            {
                return operation.Execute(binding, cancellationToken);
            }
        }

        private async Task<TResult> ExecuteEndTransactionOnPrimaryAsync<TResult>(IReadOperation<TResult> operation, CancellationToken cancellationToken)
        {
            using (var sessionHandle = new NonDisposingCoreSessionHandle(this))
            using (var binding = new WritableServerBinding(_cluster, sessionHandle))
            {
                return await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
            }
        }

        private TransactionOptions GetEffectiveTransactionOptions(TransactionOptions transactionOptions)
        {
            var readConcern = transactionOptions?.ReadConcern ?? _options.DefaultTransactionOptions?.ReadConcern ?? ReadConcern.Default;
            var readPreference = transactionOptions?.ReadPreference ?? _options.DefaultTransactionOptions?.ReadPreference ?? ReadPreference.Primary;
            var writeConcern = transactionOptions?.WriteConcern ?? _options.DefaultTransactionOptions?.WriteConcern ?? new WriteConcern();
            return new TransactionOptions(readConcern, readPreference, writeConcern);
        }

        private WriteConcern GetTransactionWriteConcern()
        {
            return
                _currentTransaction.TransactionOptions?.WriteConcern ??
                _options.DefaultTransactionOptions?.WriteConcern ??
                WriteConcern.WMajority;
        }

        private bool ShouldIgnoreAbortTransactionException(Exception exception)
        {
            var commandException = exception as MongoCommandException;
            if (commandException != null)
            {
                return true;
            }

            return false;
        }

        private bool ShouldRetryEndTransactionException(Exception exception)
        {
            return RetryabilityHelper.IsRetryableWriteException(exception);
        }
    }
}
