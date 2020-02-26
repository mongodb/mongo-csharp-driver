/* Copyright 2015-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public abstract class OperationTestBase : IDisposable
    {
        protected ICluster _cluster;
        protected Driver.CollectionNamespace _collectionNamespace;
        protected DatabaseNamespace _databaseNamespace;
        private bool _hasOncePerFixtureRun;
        protected MessageEncoderSettings _messageEncoderSettings;
        protected readonly ICoreSessionHandle _session;

        public OperationTestBase()
        {
            _cluster = CoreTestConfiguration.Cluster;
            _databaseNamespace = CoreTestConfiguration.DatabaseNamespace;
            _collectionNamespace = CoreTestConfiguration.GetCollectionNamespaceForTestClass(GetType());
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
            _session = CoreTestConfiguration.StartSession(_cluster);
        }

        public virtual void Dispose()
        {
            _session.ReferenceCount().Should().Be(1);
            _session.Dispose();

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

        protected void CreateCollection(CollectionNamespace collectionNamespace)
        {
            var operation = new CreateCollectionOperation(collectionNamespace, _messageEncoderSettings);
            ExecuteOperation(operation);
        }

        protected void Delete(BsonDocument filter)
        {
            var requests = new[] { new DeleteRequest(filter) };
            var operation = new BulkDeleteOperation(_collectionNamespace, requests, _messageEncoderSettings);
            ExecuteOperation(operation);
        }

        protected void Delete(string filter)
        {
            Delete(BsonDocument.Parse(filter));
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
            DropCollection(_collectionNamespace);
        }

        protected void DropCollection(CollectionNamespace collectionNamespace)
        {
            var dropCollectionOperation = new DropCollectionOperation(collectionNamespace, _messageEncoderSettings);
            ExecuteOperation(dropCollectionOperation);
        }

        protected Task DropCollectionAsync()
        {
            var dropCollectionOperation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            return ExecuteOperationAsync(dropCollectionOperation);
        }

        protected void EnsureDatabaseExists()
        {
            var collectionName = $"EnsureDatabaseExists-{_databaseNamespace.DatabaseName}";
            var collectionNamespace = new CollectionNamespace(_databaseNamespace, collectionName);
            var filter = new BsonDocument("_id", 1);
            var update = new BsonDocument("$set", new BsonDocument("x", 1));
            var operation = new FindOneAndUpdateOperation<BsonDocument>(collectionNamespace, filter, update, BsonDocumentSerializer.Instance, new MessageEncoderSettings())
            {
                IsUpsert = true
            };
            ExecuteOperation(operation);
        }

        protected TResult ExecuteOperation<TResult>(IReadOperation<TResult> operation)
        {
            using (var binding = CreateReadBinding())
            using (var bindingHandle = new ReadBindingHandle(binding))
            {
                return operation.Execute(bindingHandle, CancellationToken.None);
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
            using (var binding = CreateReadBinding(readPreference))
            using (var bindingHandle = new ReadBindingHandle(binding))
            {
                return ExecuteOperation(operation, bindingHandle, async);
            }
        }

        protected TResult ExecuteOperation<TResult>(IWriteOperation<TResult> operation, bool useImplicitSession = false)
        {
            using (var binding = CreateReadWriteBinding(useImplicitSession))
            using (var bindingHandle = new ReadWriteBindingHandle(binding))
            {
                return operation.Execute(bindingHandle, CancellationToken.None);
            }
        }

        protected TResult ExecuteOperation<TResult>(IWriteOperation<TResult> operation, bool async, bool useImplicitSession = false)
        {
            if (async)
            {
                return ExecuteOperationAsync(operation, useImplicitSession).GetAwaiter().GetResult();
            }
            else
            {
                return ExecuteOperation(operation, useImplicitSession);
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
            using (var binding = CreateReadBinding())
            using (var bindingHandle = new ReadBindingHandle(binding))
            {
                return await ExecuteOperationAsync(operation, bindingHandle);
            }
        }

        protected async Task<TResult> ExecuteOperationAsync<TResult>(IReadOperation<TResult> operation, IReadBinding binding)
        {
            return await operation.ExecuteAsync(binding, CancellationToken.None);
        }

        protected async Task<TResult> ExecuteOperationAsync<TResult>(IWriteOperation<TResult> operation, bool useImplicitSession = false)
        {
            using (var binding = CreateReadWriteBinding(useImplicitSession))
            using (var bindingHandle = new ReadWriteBindingHandle(binding))
            {
                return await operation.ExecuteAsync(bindingHandle, CancellationToken.None);
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

        protected IReadBinding CreateReadBinding()
        {
            return CreateReadBinding(ReadPreference.Primary);
        }

        protected IReadBinding CreateReadBinding(ReadPreference readPreference)
        {
            return new ReadPreferenceBinding(_cluster, readPreference, _session.Fork());
        }

        protected IReadWriteBinding CreateReadWriteBinding(bool useImplicitSession = false)
        {
            var options = new CoreSessionOptions(isImplicit: useImplicitSession);
            var session = CoreTestConfiguration.StartSession(_cluster, options);
            return new WritableServerBinding(_cluster, session);
        }

        protected void Insert(params BsonDocument[] documents)
        {
            Insert((IEnumerable<BsonDocument>)documents);
        }

        protected void Insert(params string[] documents)
        {
            Insert(documents.Select(d => BsonDocument.Parse(d)));
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

        protected Profiler Profile(DatabaseNamespace databaseNamespace)
        {
            var op = new WriteCommandOperation<BsonDocument>(
                    _databaseNamespace,
                    new BsonDocument("profile", 2),
                    BsonDocumentSerializer.Instance,
                    new MessageEncoderSettings());

            ExecuteOperation(op);

            return new Profiler(this, databaseNamespace);
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
            using (var cursor = ExecuteOperation(operation))
            {
                return ReadCursorToEnd(cursor);
            }
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
            using (var cursor = await ExecuteOperationAsync(operation))
            {
                return await ReadCursorToEndAsync(cursor);
            }
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

        protected void Update(BsonDocument filter, BsonDocument update)
        {
            var requests = new[] { new UpdateRequest(UpdateType.Update, filter, update) };
            var operation = new BulkUpdateOperation(_collectionNamespace, requests, _messageEncoderSettings);
            ExecuteOperation(operation);
        }

        protected void Update(string filter, string update)
        {
            Update(BsonDocument.Parse(filter), BsonDocument.Parse(update));
        }

        protected void VerifySessionIdWasNotSentIfUnacknowledgedWrite<TResult>(
            IWriteOperation<TResult> operation,
            string commandName,
            bool async,
            bool useImplicitSession)
        {
            VerifySessionIdSending(
                (binding, cancellationToken) => operation.ExecuteAsync(binding, cancellationToken),
                (binding, cancellationToken) => operation.Execute(binding, cancellationToken),
                AssertSessionIdWasNotSentIfUnacknowledgedWrite,
                commandName,
                async,
                useImplicitSession);
        }

        protected void VerifySessionIdWasSentWhenSupported<TResult>(IReadOperation<TResult> operation, string commandName, bool async)
        {
            VerifySessionIdSending(
                (binding, cancellationToken) => operation.ExecuteAsync(binding, cancellationToken),
                (binding, cancellationToken) => operation.Execute(binding, cancellationToken),
                AssertSessionIdWasSentWhenSupported,
                commandName,
                async);
        }

        protected void VerifySessionIdWasSentWhenSupported<TResult>(IWriteOperation<TResult> operation, string commandName, bool async)
        {
            VerifySessionIdSending(
                (binding, cancellationToken) => operation.ExecuteAsync(binding, cancellationToken),
                (binding, cancellationToken) => operation.Execute(binding, cancellationToken),
                AssertSessionIdWasSentWhenSupported,
                commandName,
                async);
        }

        protected void VerifySessionIdSending<TResult>(
            Func<WritableServerBinding, CancellationToken, Task<TResult>> executeAsync,
            Func<WritableServerBinding, CancellationToken, TResult> execute,
            Action<EventCapturer, ICoreSessionHandle, Exception> assertResults,
            string commandName,
            bool async,
            bool useImplicitSession = false)
        {
            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == commandName);
            using (var cluster = CoreTestConfiguration.CreateCluster(b => b.Subscribe(eventCapturer)))
            {
                using (var session = CreateSession(cluster, useImplicitSession))
                using (var binding = new WritableServerBinding(cluster, session.Fork()))
                {
                    var cancellationToken = new CancellationTokenSource().Token;
                    Exception exception;
                    if (async)
                    {
                        exception = Record.Exception(() => executeAsync(binding, cancellationToken).GetAwaiter().GetResult());
                    }
                    else
                    {
                        exception = Record.Exception(() => execute(binding, cancellationToken));
                    }

                    assertResults(eventCapturer, session, exception);
                }
            }
        }

        // private methods
        private void AssertSessionIdWasNotSentIfUnacknowledgedWrite(EventCapturer eventCapturer, ICoreSessionHandle session, Exception ex)
        {
            if (session.IsImplicit)
            {
                var commandStartedEvent = (CommandStartedEvent)eventCapturer.Next();
                var command = commandStartedEvent.Command;
                command.Contains("lsid").Should().BeFalse();
                session.ReferenceCount().Should().Be(2);
            }
            else
            {
                var e = ex.Should().BeOfType<InvalidOperationException>().Subject;
                e.Message.Should().Be("Explicit session must not be used with unacknowledged writes.");
            }
        }

        private void AssertSessionIdWasSentWhenSupported(EventCapturer eventCapturer, ICoreSessionHandle session, Exception exception)
        {
            exception.Should().BeNull();
            var commandStartedEvent = (CommandStartedEvent)eventCapturer.Next();
            var command = commandStartedEvent.Command;
            if (session.Id == null)
            {
                command.Contains("lsid").Should().BeFalse();
            }
            else
            {
                command["lsid"].Should().Be(session.Id);
            }

            session.ReferenceCount().Should().Be(2);
        }

        private ICoreSessionHandle CreateSession(ICluster cluster, bool useImplicitSession)
        {
            var options = new CoreSessionOptions(isImplicit: useImplicitSession);
            return CoreTestConfiguration.StartSession(cluster, options);
        }

        protected class Profiler : IDisposable
        {
            private readonly OperationTestBase _testBase;
            private readonly DatabaseNamespace _databaseNamespace;

            public Profiler(OperationTestBase testBase, DatabaseNamespace databaseNamespace)
            {
                _testBase = testBase;
                _databaseNamespace = databaseNamespace;
            }

            public CollectionNamespace CollectionNamespace => new CollectionNamespace(_databaseNamespace, "system.profile");

            public List<BsonDocument> Find(BsonDocument filter)
            {
                var find = new FindOperation<BsonDocument>(CollectionNamespace, BsonDocumentSerializer.Instance, new MessageEncoderSettings())
                {
                    Filter = filter
                };

                var cursor = _testBase.ExecuteOperation(find, false);
                return _testBase.ReadCursorToEnd(cursor, false);
            }

            public void Dispose()
            {
                var turnOff = new WriteCommandOperation<BsonDocument>(
                    _databaseNamespace,
                    new BsonDocument("profile", 0),
                    BsonDocumentSerializer.Instance,
                    new MessageEncoderSettings());

                _testBase.ExecuteOperation(turnOff);

                var drop = new DropCollectionOperation(CollectionNamespace, new MessageEncoderSettings());
                _testBase.ExecuteOperation(drop);
            }
        }
    }
}
