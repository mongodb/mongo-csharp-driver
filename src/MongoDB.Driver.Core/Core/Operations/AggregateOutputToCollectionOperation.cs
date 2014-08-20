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
    public class AggregateOutputToCollectionOperation : AggregateOperationBase, IWriteOperation<BsonDocument>
    {
        // constructors
        public AggregateOutputToCollectionOperation(string databaseName, string collectionName, IEnumerable<BsonDocument> pipeline)
            : base(databaseName, collectionName, pipeline)
        {
        }

        private AggregateOutputToCollectionOperation(
            bool? allowDiskUsage,
            string collectionName,
            string databaseName,
            IReadOnlyList<BsonDocument> pipeline)
            : base(
                allowDiskUsage,
                collectionName,
                databaseName,
                pipeline)
        {
        }

        // methods
        private void EnsureIsOutputToCollectionPipeline()
        {
            var lastStage = Pipeline.LastOrDefault();
            if (lastStage == null || lastStage.GetElement(0).Name != "$out")
            {
                throw new ArgumentException("The last stage of the pipeline for an AggregateOutputToCollectionOperation must have a $out operator.");
            }
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            EnsureIsOutputToCollectionPipeline();

            var command = CreateCommand();
            var operation = new WriteCommandOperation(DatabaseName, command);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public async Task<BsonDocument> ExplainAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            EnsureIsOutputToCollectionPipeline();

            var command = CreateCommand();
            command["explain"] = true;
            var operation = new WriteCommandOperation(DatabaseName, command);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public AggregateOutputToCollectionOperation WithAllowDiskUsage(bool? value)
        {
            return (AllowDiskUsage == value) ? this : new Builder(this) { _allowDiskUsage = value }.Build();
        }

        public AggregateOutputToCollectionOperation WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (CollectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public AggregateOutputToCollectionOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (DatabaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public AggregateOutputToCollectionOperation WithPipeline(IEnumerable<BsonDocument> value)
        {
            Ensure.IsNotNull(value, "value");
            return (Pipeline == value) ? this : new Builder(this) { _pipeline = value.ToList() }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public bool? _allowDiskUsage;
            public string _collectionName;
            public string _databaseName;
            public IReadOnlyList<BsonDocument> _pipeline;

            // constructors
            public Builder(AggregateOutputToCollectionOperation other)
            {
                _allowDiskUsage = other.AllowDiskUsage;
                _collectionName = other.CollectionName;
                _databaseName = other.DatabaseName;
                _pipeline = other.Pipeline;
            }

            // methods
            public AggregateOutputToCollectionOperation Build()
            {
                return new AggregateOutputToCollectionOperation(
                    _allowDiskUsage,
                    _collectionName,
                    _databaseName,
                    _pipeline);
            }
        }
    }
}
