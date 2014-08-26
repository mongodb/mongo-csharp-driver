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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class MapReduceOutputToCollectionOperation : MapReduceOperationBase, IWriteOperation<BsonDocument>
    {
        // fields
        private bool? _nonAtomicOutput;
        private string _outputCollectionName;
        private string _outputDatabaseName;
        private MapReduceOutputMode _outputMode;
        private bool? _shardedOutput;

        // constructors
        public MapReduceOutputToCollectionOperation(
            string databaseName,
            string collectionName,
            string outputCollectionName,
            BsonJavaScript mapFunction,
            BsonJavaScript reduceFunction,
            BsonDocument query,
            MessageEncoderSettings messageEncoderSettings)
            : base(
                databaseName,
                collectionName,
                mapFunction,
                reduceFunction,
                query,
                messageEncoderSettings)
        {
            _outputCollectionName = Ensure.IsNotNullOrEmpty(outputCollectionName, "outputCollectionName");
            _outputMode = MapReduceOutputMode.Replace;
        }

        // properties
        public bool? NonAtomicOutput
        {
            get { return _nonAtomicOutput; }
            set { _nonAtomicOutput = value; }
        }

        public string OutputCollectionName
        {
            get { return _outputCollectionName; }
            set { _outputCollectionName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public string OutputDatabaseName
        {
            get { return _outputDatabaseName; }
            set { _outputDatabaseName = value; }
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
            Ensure.IsNullOrNotEmpty(_outputDatabaseName, "DatabaseName");
            Ensure.IsNotNullOrEmpty(_outputCollectionName, "OutputCollectionName");

            var action = _outputMode.ToString().ToLowerInvariant();
            return new BsonDocument
            {
                { action, _outputCollectionName },
                { "db", _outputDatabaseName, _outputDatabaseName != null },
                { "sharded", () => _shardedOutput.Value, _shardedOutput.HasValue },
                { "nonAtomic", () => _nonAtomicOutput.Value, _nonAtomicOutput.HasValue }
            };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation(DatabaseName, command, MessageEncoderSettings);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
    }
}
