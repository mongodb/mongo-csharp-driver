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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Abstract base class for AbortTransactionOperation and CommitTransactionOperation.
    /// </summary>
    public abstract class EndTransactionOperation : IReadOperation<BsonDocument>
    {
        // private fields
        private MessageEncoderSettings _messageEncoderSettings;
        private readonly WriteConcern _writeConcern;

        // protected constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EndTransactionOperation"/> class.
        /// </summary>
        /// <param name="writeConcern">The write concern.</param>
        protected EndTransactionOperation(WriteConcern writeConcern)
        {
            _writeConcern = Ensure.IsNotNull(writeConcern, nameof(writeConcern));
        }

        // public properties
        /// <summary>
        /// Gets or sets the message encoder settings.
        /// </summary>
        /// <value>
        /// The message encoder settings.
        /// </value>
        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        /// <summary>
        /// Gets the write concern.
        /// </summary>
        /// <value>
        /// The write concern.
        /// </value>
        public WriteConcern WriteConcern => _writeConcern;

        // protected properties
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        /// <value>
        /// The name of the command.
        /// </value>
        protected abstract string CommandName { get; }

        // public methods
        /// <inheritdoc />
        public BsonDocument Execute(IReadBinding binding, CancellationToken cancellationToken)
        {
            var operation = CreateOperation();
            return operation.Execute(binding, cancellationToken);
        }

        /// <inheritdoc />
        public Task<BsonDocument> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            var operation = CreateOperation();
            return operation.ExecuteAsync(binding, cancellationToken);
        }

        // private methods
        private BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { CommandName, 1 },
                { "writeConcern", () => _writeConcern.ToBsonDocument(), !_writeConcern.IsServerDefault }
            };
        }

        private IReadOperation<BsonDocument> CreateOperation()
        {
            var command = CreateCommand();
            return new ReadCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
        }
    }

    /// <summary>
    /// The abort transaction operation.
    /// </summary>
    public sealed class AbortTransactionOperation : EndTransactionOperation
    {
        // public constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AbortTransactionOperation"/> class.
        /// </summary>
        /// <param name="writeConcern">The write concern.</param>
        public AbortTransactionOperation(WriteConcern writeConcern)
            : base(writeConcern)
        {
        }

        // protected properties
        /// <inheritdoc />
        protected override string CommandName => "abortTransaction";
    }

    /// <summary>
    /// The commit transaction operation.
    /// </summary>
    public sealed class CommitTransactionOperation : EndTransactionOperation
    {
        // public constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AbortTransactionOperation"/> class.
        /// </summary>
        /// <param name="writeConcern">The write concern.</param>
        public CommitTransactionOperation(WriteConcern writeConcern)
            : base(writeConcern)
        {
        }

        // protected properties
        /// <inheritdoc />
        protected override string CommandName => "commitTransaction";
    }
}
