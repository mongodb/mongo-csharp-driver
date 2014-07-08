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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public abstract class AggregateOperationBase
    {
        // fields
        private readonly bool? _allowDiskUsage;
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly IReadOnlyList<BsonDocument> _pipeline;

        // constructors
        protected AggregateOperationBase(
            string databaseName,
            string collectionName,
            IEnumerable<BsonDocument> pipeline)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
        }

        protected AggregateOperationBase(
            bool? allowDiskUsage,
            string collectionName,
            string databaseName,
            IReadOnlyList<BsonDocument> pipeline)
        {
            _allowDiskUsage = allowDiskUsage;
            _collectionName = collectionName;
            _databaseName = databaseName;
            _pipeline = pipeline;
        }

        // properties
        public bool? AllowDiskUsage
        {
            get { return _allowDiskUsage; }
        }

        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public IReadOnlyList<BsonDocument> Pipeline
        {
            get { return _pipeline; }
        }

        // methods
        public virtual BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "aggregate", _collectionName },
                { "pipeline", new BsonArray(_pipeline) },
                { "allowDiskUsage", () => _allowDiskUsage.Value, _allowDiskUsage.HasValue }
            };
        }
    }
}
