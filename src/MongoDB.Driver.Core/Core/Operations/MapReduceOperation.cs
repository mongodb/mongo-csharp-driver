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
    public class MapReduceOperation<TResult> : MapReduceOperationBase, IReadOperation<TResult>
    {
        // fields
        private readonly IBsonSerializer<TResult> _resultSerializer;

        // constructors
        public MapReduceOperation(CollectionNamespace collectionNamespace, BsonJavaScript mapFunction, BsonJavaScript reduceFunction, BsonDocument query, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : base(
                collectionNamespace,
                mapFunction,
                reduceFunction,
                query,
                messageEncoderSettings)
        {
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
        }

        // properties
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer;}
        }

        // methods
        protected override BsonDocument CreateOutputOptions()
        {
            return new BsonDocument("inline", 1);
        }

        public Task<TResult> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new ReadCommandOperation<TResult>(CollectionNamespace.DatabaseNamespace, command, _resultSerializer, MessageEncoderSettings);
            return operation.ExecuteAsync(binding, cancellationToken);
        }
    }
}
