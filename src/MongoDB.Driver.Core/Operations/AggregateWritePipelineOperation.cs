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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class AggregateWritePipelineOperation : AggregateCursorOperationBase, IWriteOperation<Cursor<BsonDocument>>
    {
        // constructors
        public AggregateWritePipelineOperation(string databaseName, string collectionName, IEnumerable<BsonDocument> pipeline)
            : base(databaseName, collectionName, pipeline)
        {
        }

        private AggregateWritePipelineOperation(
            bool? allowDiskUsage,
            int? batchSize,
            string collectionName,
            string databaseName,
            AggregateResultMode resultMode,
            IReadOnlyList<BsonDocument> pipeline)
            : base(
                allowDiskUsage,
                batchSize,
                collectionName,
                databaseName,
                resultMode,
                pipeline)
        {
        }

        // methods
        public async Task<Cursor<BsonDocument>> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");

            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connectionSource = await binding.GetWriteConnectionSourceAsync(slidingTimeout, cancellationToken))
            {
                var command = CreateCommand();
                var operation = new WriteCommandOperation(DatabaseName, command);
                var result = await operation.ExecuteAsync(connectionSource, slidingTimeout, cancellationToken);
                return CreateCursor(connectionSource, command, result, timeout, cancellationToken);
            }
        }

        public async Task<BsonDocument> ExplainAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");

            var command = CreateCommand();
            command["explain"] = true;
            var operation = new WriteCommandOperation(DatabaseName, command);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public AggregateWritePipelineOperation WithAllowDiskUsage(bool? value)
        {
            return (AllowDiskUsage == value) ? this : new Builder(this) { _allowDiskUsage = value }.Build();
        }

        public AggregateWritePipelineOperation WithBatchSize(int? value)
        {
            return (BatchSize == value) ? this : new Builder(this) { _batchSize = value }.Build();
        }

        public AggregateWritePipelineOperation WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (CollectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public AggregateWritePipelineOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (DatabaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public AggregateWritePipelineOperation WithPipeline(IEnumerable<BsonDocument> value)
        {
            Ensure.IsNotNull(value, "value");
            return (Pipeline == value) ? this : new Builder(this) { _pipeline = value.ToList() }.Build();
        }

        public AggregateWritePipelineOperation WithResultMode(AggregateResultMode value)
        {
            return (ResultMode == value) ? this : new Builder(this) { _resultMode = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public bool? _allowDiskUsage;
            public int? _batchSize;
            public string _collectionName;
            public string _databaseName;
            public AggregateResultMode _resultMode;
            public IReadOnlyList<BsonDocument> _pipeline;

            // constructors
            public Builder(AggregateWritePipelineOperation other)
            {
                _allowDiskUsage = other.AllowDiskUsage;
                _batchSize = other.BatchSize;
                _collectionName = other.CollectionName;
                _databaseName = other.DatabaseName;
                _resultMode = other.ResultMode;
                _pipeline = other.Pipeline;
            }

            // methods
            public AggregateWritePipelineOperation Build()
            {
                return new AggregateWritePipelineOperation(
                    _allowDiskUsage,
                    _batchSize,
                    _collectionName,
                    _databaseName,
                    _resultMode,
                    _pipeline);
            }
        }
    }
}
