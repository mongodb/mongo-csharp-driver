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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a map-reduce operation.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    [Obsolete("Use Aggregation pipeline instead.")]
    internal sealed class MapReduceOperation<TResult> : MapReduceOperationBase, IReadOperation<IAsyncCursor<TResult>>
    {
        // fields
        private ReadConcern _readConcern = ReadConcern.Default;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MapReduceOperation{TResult}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="mapFunction">The map function.</param>
        /// <param name="reduceFunction">The reduce function.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public MapReduceOperation(CollectionNamespace collectionNamespace, BsonJavaScript mapFunction, BsonJavaScript reduceFunction, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : base(
                collectionNamespace,
                mapFunction,
                reduceFunction,
                messageEncoderSettings)
        {
            _resultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
        }

        // properties
        /// <summary>
        /// Gets or sets the read concern.
        /// </summary>
        /// <value>
        /// The read concern.
        /// </value>
        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set { _readConcern = Ensure.IsNotNull(value, nameof(value)); }
        }

        /// <summary>
        /// Gets the result serializer.
        /// </summary>
        /// <value>
        /// The result serializer.
        /// </value>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        /// <summary>
        /// Gets the name of the operation.
        /// </summary>
        public string OperationName => "mapReduce";

        // methods
        /// <inheritdoc/>
        protected override BsonDocument CreateOutputOptions()
        {
            return new BsonDocument("inline", 1);
        }

        /// <inheritdoc/>
        public IAsyncCursor<TResult> Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var channelSource = binding.GetReadChannelSource(operationContext))
            using (var channel = channelSource.GetChannel(operationContext))
            using (var channelBinding = new ChannelReadBinding(channelSource.Server, channel, binding.ReadPreference, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session, channel.ConnectionDescription);
                var result = operation.Execute(operationContext, channelBinding);
                return new SingleBatchAsyncCursor<TResult>(result);
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncCursor<TResult>> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var channelSource = await binding.GetReadChannelSourceAsync(operationContext).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadBinding(channelSource.Server, channel, binding.ReadPreference, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session, channel.ConnectionDescription);
                var result = await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
                return new SingleBatchAsyncCursor<TResult>(result);
            }
        }

        /// <inheritdoc/>
        protected internal override BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription)
        {
            var command = base.CreateCommand(operationContext, session, connectionDescription);

            var readConcern = ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, _readConcern);
            if (readConcern != null)
            {
                command.Add("readConcern", readConcern);
            }

            return command;
        }

        private ReadCommandOperation<TResult[]> CreateOperation(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription)
        {
            var command = CreateCommand(operationContext, session, connectionDescription);
            var resultArraySerializer = new ArraySerializer<TResult>(_resultSerializer);
            var resultSerializer = new ElementDeserializer<TResult[]>("results", resultArraySerializer);
            return new ReadCommandOperation<TResult[]>(CollectionNamespace.DatabaseNamespace, command, resultSerializer, MessageEncoderSettings)
            {
                RetryRequested = false
            };
        }
    }
}
