using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.SyncExtensionMethods;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    public abstract class OperationTestBase
    {
        protected CollectionNamespace _collectionNamespace;
        protected MessageEncoderSettings _messageEncoderSettings;
        private bool _hasOncePerFixtureRun;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _collectionNamespace = new CollectionNamespace(SuiteConfiguration.DatabaseNamespace, GetType().Name);
            _messageEncoderSettings = SuiteConfiguration.MessageEncoderSettings;
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

        protected List<BsonDocument> ReadCursorToEnd(IAsyncCursor<BsonDocument> cursor)
        {
            var documents = new List<BsonDocument>();
            while (cursor.MoveNextAsync().GetAwaiter().GetResult())
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
