using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    public abstract class OperationTestBase
    {
        protected DatabaseNamespace _databaseNamespace;
        protected CollectionNamespace _collectionNamespace;
        protected MessageEncoderSettings _messageEncoderSettings;
        private bool _hasOncePerFixtureRun;

        [TestFixtureSetUp]
        public virtual void TestFixtureSetUp()
        {
            _databaseNamespace = CoreTestConfiguration.DatabaseNamespace;
            _collectionNamespace = new CollectionNamespace(_databaseNamespace, GetType().Name);
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown()
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

        protected async Task<TException> CatchAsync<TException>(Func<Task> action)
            where TException : Exception
        {
            try
            {
                await action();
                Assert.Fail("Expected an exception of type {0} but got none.", typeof(TException));
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected an exception of type {0} but got {1}.", typeof(TException), ex.GetType());
            }

            throw new InvalidOperationException("We should never get here!");
        }

        protected void DropDatabase()
        {
            DropDatabaseAsync().GetAwaiter().GetResult();
        }

        protected Task DropDatabaseAsync()
        {
            var dropDatabaseOperation = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);
            return ExecuteOperationAsync(dropDatabaseOperation);
        }

        protected void DropCollection()
        {
            DropCollectionAsync().GetAwaiter().GetResult();
        }

        protected Task DropCollectionAsync()
        {
            var dropCollectionOperation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            return ExecuteOperationAsync(dropCollectionOperation);
        }

        protected TResult ExecuteOperation<TResult>(IReadOperation<TResult> operation)
        {
            return ExecuteOperationAsync(operation).GetAwaiter().GetResult();
        }

        protected TResult ExecuteOperation<TResult>(IWriteOperation<TResult> operation)
        {
            return ExecuteOperationAsync(operation).GetAwaiter().GetResult();
        }

        protected async Task<TResult> ExecuteOperationAsync<TResult>(IReadOperation<TResult> operation)
        {
            using (var binding = CoreTestConfiguration.GetReadBinding())
            {
                return await operation.ExecuteAsync(binding, CancellationToken.None);
            }
        }

        protected async Task<TResult> ExecuteOperationAsync<TResult>(IWriteOperation<TResult> operation)
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                return await operation.ExecuteAsync(binding, CancellationToken.None);
            }
        }

        protected void Insert(params BsonDocument[] documents)
        {
            Insert((IEnumerable<BsonDocument>)documents);
        }

        protected void Insert(IEnumerable<BsonDocument> documents)
        {
            InsertAsync(documents).GetAwaiter().GetResult();
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

        protected async Task<List<BsonDocument>> ReadCursorToEndAsync(IAsyncCursor<BsonDocument> cursor)
        {
            var documents = new List<BsonDocument>();
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
