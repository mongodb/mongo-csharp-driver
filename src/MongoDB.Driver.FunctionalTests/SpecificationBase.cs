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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using NUnit.Framework;

namespace MongoDB.Driver
{
    public abstract class SpecificationBase
    {
        private ICluster _cluster;
        protected MongoClient _client;
        protected IMongoDatabase _database;
        protected string _collectionName;

        [TestFixtureSetUp]
        public void SetUpFixtureAsync()
        {
            _client = SuiteConfiguration.Client;
            _cluster = _client.Cluster;
            _database = _client.GetDatabase(SuiteConfiguration.DatabaseName);
            _collectionName = GetType().Name;

            Given();
            When();
        }

        protected virtual void Given()
        { }

        protected abstract void When();

        protected Exception Catch(Func<Task> action)
        {
            Exception result = null;
            try
            {
                action().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                result = ex;
            }
            return result;
        }

        protected TResult ExecuteOperation<TResult>(IReadOperation<TResult> operation, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return ExecuteOperationAsync(operation, timeout, cancellationToken).GetAwaiter().GetResult();
        }

        protected Task<TResult> ExecuteOperationAsync<TResult>(IReadOperation<TResult> operation, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = new WritableServerBinding(_cluster))
            {
                return operation.ExecuteAsync(binding, timeout, cancellationToken);
            }
        }

        protected TResult ExecuteOperation<TResult>(IWriteOperation<TResult> operation, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            return ExecuteOperationAsync(operation, timeout, cancellationToken).GetAwaiter().GetResult();
        }

        protected Task<TResult> ExecuteOperationAsync<TResult>(IWriteOperation<TResult> operation, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = new WritableServerBinding(_cluster))
            {
                return operation.ExecuteAsync(binding, timeout, cancellationToken);
            }
        }

        protected void Insert<T>(IEnumerable<T> documents, IBsonSerializer<T> serializer)
        {
            var requests = documents.Select(d => new MongoDB.Driver.Core.Operations.InsertRequest(d, serializer));
            var operation = new BulkMixedWriteOperation(_database.DatabaseName, _collectionName, requests);
            ExecuteOperation(operation);
        }

        protected void Insert(IEnumerable<BsonDocument> documents)
        {
            Insert(documents, BsonDocumentSerializer.Instance);
        }
    }
}