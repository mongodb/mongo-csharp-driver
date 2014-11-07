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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class MapReduceOutputToCollectionOperation : MapReduceOperationBase, IWriteOperation<BsonDocument>
    {
        // fields
        private bool? _nonAtomicOutput;
        private readonly CollectionNamespace _outputCollectionNamespace;
        private MapReduceOutputMode _outputMode;
        private bool? _shardedOutput;

        // constructors
        public MapReduceOutputToCollectionOperation(
            CollectionNamespace collectionNamespace,
            CollectionNamespace outputCollectionNamespace,
            BsonJavaScript mapFunction,
            BsonJavaScript reduceFunction,
            BsonDocument query,
            MessageEncoderSettings messageEncoderSettings)
            : base(
                collectionNamespace,
                mapFunction,
                reduceFunction,
                query,
                messageEncoderSettings)
        {
            _outputCollectionNamespace = Ensure.IsNotNull(outputCollectionNamespace, "outputCollectionNamespace");
            _outputMode = MapReduceOutputMode.Replace;
        }

        // properties
        public bool? NonAtomicOutput
        {
            get { return _nonAtomicOutput; }
            set { _nonAtomicOutput = value; }
        }

        public CollectionNamespace OutputCollectionNamespace
        {
            get { return _outputCollectionNamespace; }
        }

        public MapReduceOutputMode OutputMode
        {
            get { return _outputMode; }
            set { _outputMode = value; }
        }

        public bool? ShardedOutput
        {
            get { return _shardedOutput; }
            set { _shardedOutput = value; }
        }

        // methods
        protected override BsonDocument CreateOutputOptions()
        {
            var action = _outputMode.ToString().ToLowerInvariant();
            return new BsonDocument
            {
                { action, _outputCollectionNamespace.CollectionName },
                { "db", _outputCollectionNamespace.DatabaseNamespace.DatabaseName },
                { "sharded", () => _shardedOutput.Value, _shardedOutput.HasValue },
                { "nonAtomic", () => _nonAtomicOutput.Value, _nonAtomicOutput.HasValue }
            };
        }

        public Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation(CollectionNamespace.DatabaseNamespace, command, MessageEncoderSettings);
            return operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
    }
}
