/* Copyright 2015-2016 MongoDB Inc.
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
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public abstract class OperationTestBase : IDisposable
    {
        protected DatabaseNamespace _databaseNamespace;
        protected CollectionNamespace _collectionNamespace;
        protected MessageEncoderSettings _messageEncoderSettings;
        private bool _hasOncePerFixtureRun;

        public OperationTestBase()
        {
            _databaseNamespace = CoreTestConfiguration.DatabaseNamespace;
            _collectionNamespace = new CollectionNamespace(_databaseNamespace, GetType().Name);
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
        }

        public virtual void Dispose()
        {
            try
            {
                // TODO: DropDatabase
                //var dropDatabaseOperation = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);
                //ExecuteOperation(dropDatabaseOperation);
            }
            catch
            {
                // ignore exceptions
            }
        }

        protected void DropDatabase()
        {
            var dropDatabaseOperation = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);
            ExecuteOperation(dropDatabaseOperation);
        }

        protected Task DropDatabaseAsync()
        {
            var dropDatabaseOperation = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);
            return ExecuteOperationAsync(dropDatabaseOperation);
        }

        protected void DropCollection()
        {
            var dropCollectionOperation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            ExecuteOperation(dropCollectionOperation);
        }

        protected Task DropCollectionAsync()
        {
            var dropCollectionOperation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            return ExecuteOperationAsync(dropCollectionOperation);
        }

        protected TResult ExecuteOperation<TResult>(IReadOperation<TResult> operation)
        {
            using (var binding = CoreTestConfiguration.GetReadBinding())
            {
                return operation.Execute(binding, CancellationToken.None);
            }
        }

        protected TResult ExecuteOperation<TResult>(IReadOperation<TResult> operation, bool async)
        {
            if (async)
            {
                return ExecuteOperationAsync(operation).GetAwaiter().GetResult();
            }
            else
            {
                return ExecuteOperation(operation);
            }
        }

        protected TResult ExecuteOperation<TResult>(IReadOperation<TResult> operation, IReadBinding binding, bool async)
        {
            if (async)
            {
                return operation.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                return operation.Execute(binding, CancellationToken.None);
            }
        }

        protected TResult ExecuteOperation<TResult>(IReadOperation<TResult> operation, ReadPreference readPreference, bool async)
        {
            var cluster = CoreTestConfiguration.Cluster;
            using (var binding = new ReadPreferenceBinding(cluster, readPreference))
            {
                return ExecuteOperation(operation, binding, async);
            }
        }

        protected TResult ExecuteOperation<TResult>(IWriteOperation<TResult> operation)
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                return operation.Execute(binding, CancellationToken.None);
            }
        }

        protected TResult ExecuteOperation<TResult>(IWriteOperation<TResult> operation, bool async)
        {
            if (async)
            {
                return ExecuteOperationAsync(operation).GetAwaiter().GetResult();
            }
            else
            {
                return ExecuteOperation(operation);
            }
        }

        protected TResult ExecuteOperation<TResult>(IWriteOperation<TResult> operation, IReadWriteBinding binding, bool async)
        {
            if (async)
            {
                return operation.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                return operation.Execute(binding, CancellationToken.None);
            }
        }

        protected async Task<TResult> ExecuteOperationAsync<TResult>(IReadOperation<TResult> operation)
        {
            using (var binding = CoreTestConfiguration.GetReadBinding())
            {
                return await ExecuteOperationAsync(operation, binding);
            }
        }

        protected async Task<TResult> ExecuteOperationAsync<TResult>(IReadOperation<TResult> operation, IReadBinding binding)
        {
            return await operation.ExecuteAsync(binding, CancellationToken.None);
        }

        protected async Task<TResult> ExecuteOperationAsync<TResult>(IWriteOperation<TResult> operation)
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                return await operation.ExecuteAsync(binding, CancellationToken.None);
            }
        }

        protected async Task<TResult> ExecuteOperationAsync<TResult>(IWriteOperation<TResult> operation, IWriteBinding binding)
        {
            return await operation.ExecuteAsync(binding, CancellationToken.None);
        }

        protected void CreateIndexes(params CreateIndexRequest[] requests)
        {
            var operation = new CreateIndexesOperation(
                _collectionNamespace,
                requests,
                _messageEncoderSettings);

            ExecuteOperation(operation);
        }

        protected void Insert(params BsonDocument[] documents)
        {
            Insert((IEnumerable<BsonDocument>)documents);
        }

        protected void Insert(IEnumerable<BsonDocument> documents)
        {
            var requests = documents.Select(d => new InsertRequest(d));
            var insertOperation = new BulkInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);
            ExecuteOperation(insertOperation);
        }

        protected Task InsertAsync(params BsonDocument[] documents)
        {
            return InsertAsync((IEnumerable<BsonDocument>)documents);
        }

        protected async Task InsertAsync(IEnumerable<BsonDocument> documents)
        {
            var requests = documents.Select(d => new InsertRequest(d));
            var insertOperation = new BulkInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);
            await ExecuteOperationAsync(insertOperation);
        }

        protected List<BsonDocument> ReadAllFromCollection()
        {
            return ReadAllFromCollection(_collectionNamespace);
        }

        protected List<BsonDocument> ReadAllFromCollection(bool async)
        {
            return ReadAllFromCollection(_collectionNamespace, async);
        }

        protected List<BsonDocument> ReadAllFromCollection(CollectionNamespace collectionNamespace)
        {
            var operation = new FindOperation<BsonDocument>(collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var cursor = ExecuteOperation(operation);
            return ReadCursorToEnd(cursor);
        }

        protected List<BsonDocument> ReadAllFromCollection(CollectionNamespace collectionNamespace, bool async)
        {
            if (async)
            {
                return ReadAllFromCollectionAsync(collectionNamespace).GetAwaiter().GetResult();
            }
            else
            {
                return ReadAllFromCollection(collectionNamespace);
            }
        }

        protected Task<List<BsonDocument>> ReadAllFromCollectionAsync()
        {
            return ReadAllFromCollectionAsync(_collectionNamespace);
        }

        protected async Task<List<BsonDocument>> ReadAllFromCollectionAsync(CollectionNamespace collectionNamespace)
        {
            var operation = new FindOperation<BsonDocument>(collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var cursor = await ExecuteOperationAsync(operation);
            return await ReadCursorToEndAsync(cursor);
        }

        protected List<T> ReadCursorToEnd<T>(IAsyncCursor<T> cursor)
        {
            var documents = new List<T>();
            while (cursor.MoveNext(CancellationToken.None))
            {
                foreach (var document in cursor.Current)
                {
                    documents.Add(document);
                }
            }
            return documents;
        }

        protected List<T> ReadCursorToEnd<T>(IAsyncCursor<T> cursor, bool async)
        {
            if (async)
            {
                return ReadCursorToEndAsync(cursor).GetAwaiter().GetResult();
            }
            else
            {
                return ReadCursorToEnd(cursor);
            }
        }

        protected async Task<List<T>> ReadCursorToEndAsync<T>(IAsyncCursor<T> cursor)
        {
            var documents = new List<T>();
            while (await cursor.MoveNextAsync(CancellationToken.None))
            {
                foreach (var document in cursor.Current)
                {
                    documents.Add(document);
                }
            }
            return documents;
        }

        protected void RunOncePerFixture(Action act)
        {
            if (!_hasOncePerFixtureRun)
            {
                act();
                _hasOncePerFixtureRun = true;
            }
        }
    }
}
