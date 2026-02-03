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

using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    /// <summary>
    /// The state of a transaction.
    /// </summary>
    public class CoreTransaction
    {
        // private fields
        private bool _isEmpty;
        private IChannelHandle _pinnedChannel = null;
        private IServer _pinnedServer;
        private BsonDocument _recoveryToken;
        private CoreTransactionState _state;
        private readonly long _transactionNumber;
        private readonly TransactionOptions _transactionOptions;
        private readonly object _lock = new object();
        private Activity _transactionActivity;
        private readonly bool _isTracingEnabled;

        // public constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CoreTransaction" /> class.
        /// </summary>
        /// <param name="transactionNumber">The transaction number.</param>
        /// <param name="transactionOptions">The transaction options.</param>
        public CoreTransaction(long transactionNumber, TransactionOptions transactionOptions)
            : this(transactionNumber, transactionOptions, isTracingEnabled: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreTransaction" /> class.
        /// </summary>
        /// <param name="transactionNumber">The transaction number.</param>
        /// <param name="transactionOptions">The transaction options.</param>
        /// <param name="isTracingEnabled">Whether OpenTelemetry tracing is enabled for this transaction.</param>
        internal CoreTransaction(long transactionNumber, TransactionOptions transactionOptions, bool isTracingEnabled)
        {
            _transactionNumber = transactionNumber;
            _transactionOptions = transactionOptions;
            _isTracingEnabled = isTracingEnabled;
            _state = CoreTransactionState.Starting;
            _isEmpty = true;
        }

        // internal properties
        /// <summary>
        /// Gets whether OpenTelemetry tracing is enabled for this transaction.
        /// </summary>
        internal bool IsTracingEnabled => _isTracingEnabled;

        internal OperationContext OperationContext { get; set; }

        internal IChannelHandle PinnedChannel
        {
            get => _pinnedChannel;
        }

        /// <summary>
        /// Gets or sets pinned server for the current transaction.
        /// Value has meaning if and only if a transaction is in progress.
        /// </summary>
        /// <value>
        /// The pinned server for the current transaction.
        /// </value>
        internal IServer PinnedServer
        {
            get => _pinnedServer;
            set => _pinnedServer = value;
        }

        /// <summary>
        /// Gets or sets the transaction activity (for OpenTelemetry tracing).
        /// </summary>
        internal Activity TransactionActivity
        {
            get => _transactionActivity;
            set => _transactionActivity = value;
        }

        // public properties
        /// <summary>
        /// Gets a value indicating whether the transaction is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the transaction is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty => _isEmpty;

        /// <summary>
        /// Gets the transaction state.
        /// </summary>
        /// <value>
        /// The transaction state.
        /// </value>
        public CoreTransactionState State => _state;

        /// <summary>
        /// Gets the transaction number.
        /// </summary>
        /// <value>
        /// The transaction number.
        /// </value>
        public long TransactionNumber => _transactionNumber;

        /// <summary>
        /// Gets the transaction options.
        /// </summary>
        /// <value>
        /// The transaction options.
        /// </value>
        public TransactionOptions TransactionOptions => _transactionOptions;

        /// <summary>
        /// Gets the recovery token used in sharded transactions.
        /// </summary>
        /// <value>
        /// The recovery token.
        /// </value>
        public BsonDocument RecoveryToken
        {
            get => _recoveryToken;
            internal set => _recoveryToken = value;
        }

        // internal methods
        internal void EndTransactionActivity()
        {
            _transactionActivity?.SetStatus(ActivityStatusCode.Ok);
            _transactionActivity?.Dispose();
            _transactionActivity = null;
        }

        internal void PinChannel(IChannelHandle channel)
        {
            lock (_lock)
            {
                _pinnedChannel?.Dispose();
                _pinnedChannel = channel;
            }
        }

        internal void SetState(CoreTransactionState state)
        {
            _state = state;
            if (state == CoreTransactionState.InProgress)
            {
                _isEmpty = false;
            }
        }

        internal void UnpinAll()
        {
            lock (_lock)
            {
                _pinnedChannel?.Dispose();
                _pinnedChannel = null;
                _pinnedServer = null;
            }
        }
    }
}
