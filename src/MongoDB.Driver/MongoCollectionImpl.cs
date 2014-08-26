/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    internal sealed class MongoCollectionImpl<T> : IMongoCollection<T>
    {
        // fields
        private readonly ICluster _cluster;
        private readonly string _collectionName;
        private readonly IOperationExecutor _operationExecutor;
        private readonly MongoCollectionSettings _settings;

        // constructors
        public MongoCollectionImpl(string collectionName, MongoCollectionSettings settings, ICluster cluster, IOperationExecutor operationExecutor)
        {
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _settings = Ensure.IsNotNull(settings, "settings");
            _cluster = Ensure.IsNotNull(cluster, "cluster");
            _operationExecutor = Ensure.IsNotNull(operationExecutor, "operationExecutor");
        }

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
        }

        public MongoCollectionSettings Settings
        {
            get { return _settings; }
        }

        // methods
        public Task<long> CountAsync(CountModel model, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}