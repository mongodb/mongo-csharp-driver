/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a map-reduce operation.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class MapReduceOperation<TResult> : MapReduceOperationBase, IReadOperation<IAsyncCursor<TResult>>
    {
        // fields
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
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
        }

        // properties
        /// <summary>
        /// Gets the result serializer.
        /// </summary>
        /// <value>
        /// The result serializer.
        /// </value>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer;}
        }

        // methods
        /// <inheritdoc/>
        protected override BsonDocument CreateOutputOptions()
        {
            return new BsonDocument("inline", 1);
        }

        /// <inheritdoc/>
        public async Task<IAsyncCursor<TResult>> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var resultArraySerializer = new ArraySerializer<TResult>(_resultSerializer);
            var resultSerializer = new ElementDeserializer<TResult[]>("results", resultArraySerializer);
            var operation = new ReadCommandOperation<TResult[]>(CollectionNamespace.DatabaseNamespace, command, resultSerializer, MessageEncoderSettings);
            var result = await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
            return new SingleBatchAsyncCursor<TResult>(result);
        }
    }
}
