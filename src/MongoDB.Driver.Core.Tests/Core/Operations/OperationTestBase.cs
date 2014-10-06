using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.SyncExtensionMethods;
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
        public void TestFixtureSetUp()
        {
            _databaseNamespace = SuiteConfiguration.DatabaseNamespace;
            _collectionNamespace = new CollectionNamespace(_databaseNamespace, GetType().Name);
            _messageEncoderSettings = SuiteConfiguration.MessageEncoderSettings;
        }

        protected async Task<TException> Catch<TException>(Func<Task> action)
            where TException : Exception
        {
            try
            {
                await action();
                Assert.Fail("Expected an exception of type {0} but got none.", typeof(TException));
            }
            catch(TException ex)
            {
                return ex;
            }
            catch(Exception ex)
            {
                Assert.Fail("Expected an exception of type {0} but got {1}.", typeof(TException), ex.GetType());
            }

            throw new InvalidOperationException();
        }

        protected void DropDatabase()
        {
            using(var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                var dropDatabaseOperation = new DropDatabaseOperation(_databaseNamespace, _messageEncoderSettings);
                dropDatabaseOperation.Execute(binding);
            }
        }

        protected void DropCollection()
        {
            using (var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                var dropCollectionOperation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
                dropCollectionOperation.Execute(binding);
            }
        }

        protected async Task<TResult> ExecuteOperation<TResult>(IReadOperation<TResult> operation)
        {
            using(var binding = SuiteConfiguration.GetReadBinding())
            {
                return await operation.ExecuteAsync(binding, Timeout.InfiniteTimeSpan, CancellationToken.None);
            }
        }

        protected async Task<TResult> ExecuteOperation<TResult>(IWriteOperation<TResult> operation)
        {
            using (var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                return await operation.ExecuteAsync(binding, Timeout.InfiniteTimeSpan, CancellationToken.None);
            }
        }

        protected void Insert(params BsonDocument[] data)
        {
            Insert((IEnumerable<BsonDocument>)data);
        }

        protected void Insert(IEnumerable<BsonDocument> data)
        {
            using (var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                var dropCollectionOperation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
                dropCollectionOperation.Execute(binding);

                var requests = data
                    .Select(document => new InsertRequest(document));
                var insertOperation = new BulkInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);
                insertOperation.Execute(binding);
            }
        }

        protected Task<List<BsonDocument>> ReadAllFromCollection()
        {
            return ReadAllFromCollection(_collectionNamespace); 
        }

        protected async Task<List<BsonDocument>> ReadAllFromCollection(CollectionNamespace collectionNamespace)
        {
            using(var binding = SuiteConfiguration.GetReadBinding())
            {
                var op = new FindOperation<BsonDocument>(collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings);
                var cursor = await op.ExecuteAsync(binding, Timeout.InfiniteTimeSpan, CancellationToken.None);
                return await ReadCursorToEnd(cursor);
            }
        }

        protected async Task<List<BsonDocument>> ReadCursorToEnd(IAsyncCursor<BsonDocument> cursor)
        {
            var documents = new List<BsonDocument>();
            while (await cursor.MoveNextAsync())
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
            if(!_hasOncePerFixtureRun)
            {
                act();
                _hasOncePerFixtureRun = true;
            }
        }
    }
}
