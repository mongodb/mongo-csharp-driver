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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    internal class MongoDatabaseImpl : IMongoDatabase
    {
        private readonly ICluster _cluster;
        private readonly string _databaseName;
        private readonly MongoDatabaseSettings _settings;

        public MongoDatabaseImpl(ICluster cluster, string databaseName, MongoDatabaseSettings settings)
        {
            _cluster = cluster;
            _databaseName = databaseName;
            _settings = settings;
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public MongoDatabaseSettings Settings
        {
            get { return _settings; }
        }

        public Task DropAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var operation = new DropDatabaseOperation(_databaseName);
            return ExecuteWriteOperation(operation, timeout, cancellationToken);
        }

        public Task<IReadOnlyList<string>> GetCollectionNamesAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var operation = new ListCollectionNamesOperation(_databaseName);
            return ExecuteReadOperation(operation, timeout, cancellationToken);
        }

        public Task<T> RunCommandAsync<T>(BsonDocument command, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var isReadCommand = CanCommandBeSentToSecondary.Delegate(command);

            var serializer = _settings.SerializerRegistry.GetSerializer<T>();
            if (isReadCommand)
            {
                var operation = new ReadCommandOperation<T>(_databaseName, command, serializer);
                return ExecuteReadOperation<T>(operation, timeout, cancellationToken);
            }
            else
            {
                var operation = new WriteCommandOperation<T>(_databaseName, command, serializer);
                return ExecuteWriteOperation<T>(operation, timeout, cancellationToken);
            }
        }

        private async Task<T> ExecuteReadOperation<T>(IReadOperation<T> operation, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // TODO: use settings ReadPreference
            using (var binding = new ReadPreferenceBinding(_cluster, Core.Clusters.ReadPreference.Primary))
            {
                return await operation.ExecuteAsync(binding, timeout, cancellationToken);
            }
        }

        private async Task<T> ExecuteWriteOperation<T>(IWriteOperation<T> operation, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var binding = new WritableServerBinding(_cluster))
            {
                return await operation.ExecuteAsync(binding, timeout, cancellationToken);
            }
        }
    }
}
