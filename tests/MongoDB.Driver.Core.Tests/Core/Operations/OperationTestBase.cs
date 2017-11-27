/* Copyright 2015-2017 MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.TestHelpers;
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
        protected readonly ICoreSessionHandle _session;

        public OperationTestBase()
        {
            _databaseNamespace = CoreTestConfiguration.DatabaseNamespace;
            _collectionNamespace = CoreTestConfiguration.GetCollectionNamespaceForTestClass(GetType());
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
            _session = CoreTestConfiguration.StartSession();
        }

        public virtual void Dispose()
        {
            _session.ReferenceCount().Should().Be(1);

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
            using (var binding = CoreTestConfiguration.GetReadBinding(_session.Fork()))
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
            var cluster = CoreTestConfiguration.Cluster;
            using (var binding = new ReadPreferenceBinding(cluster, readPreference, NoCoreSession.NewHandle()))
            using (var bindingHandle = new ReadBindingHandle(binding))
            {
                return ExecuteOperation(operation, bindingHandle, async);
            }
        }

        protected TResult ExecuteOperation<TResult>(IWriteOperation<TResult> operation)
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding(_session.Fork()))
            using (var bindingHandle = new ReadWriteBindingHandle(binding))
            {
                return operation.Execute(bindingHandle, CancellationToken.None);
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
            using (var binding = CoreTestConfiguration.GetReadBinding(_session.Fork()))
            using (var bindingHandle = new ReadBindingHandle(binding))
            {
                return await ExecuteOperationAsync(operation, bindingHandle);
            }
        }

        protected async Task<TResult> ExecuteOperationAsync<TResult>(IReadOperation<TResult> operation, IReadBinding binding)
        {
            return await operation.ExecuteAsync(binding, CancellationToken.None);
        }

        protected async Task<TResult> ExecuteOperationAsync<TResult>(IWriteOperation<TResult> operation)
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding(_session.Fork()))
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

        protected void VerifySessionIdWasSentWhenSupported<TResult>(IReadOperation<TResult> operation, string commandName, bool async)
        {
            VerifySessionIdWasSentWhenSupported(
                (binding, cancellationToken) => operation.ExecuteAsync(binding, cancellationToken),
                (binding, cancellationToken) => operation.Execute(binding, cancellationToken),
                commandName,
                async);
        }

        protected void VerifySessionIdWasSentWhenSupported<TResult>(IWriteOperation<TResult> operation, string commandName, bool async)
        {
            VerifySessionIdWasSentWhenSupported(
                (binding, cancellationToken) => operation.ExecuteAsync(binding, cancellationToken),
                (binding, cancellationToken) => operation.Execute(binding, cancellationToken),
                commandName,
                async);
        }

        protected void VerifySessionIdWasSentWhenSupported<TResult>(
            Func<WritableServerBinding, CancellationToken, Task<TResult>> executeAsync,
            Func<WritableServerBinding, CancellationToken, TResult> execute, 
            string commandName,
            bool async)
        {
            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == commandName);
            using (var cluster = CoreTestConfiguration.CreateCluster(b => b.Subscribe(eventCapturer)))
            {
                using (var session = CoreTestConfiguration.StartSession(cluster))
                using (var binding = new WritableServerBinding(cluster, session))
                {
                    var cancellationToken = new CancellationTokenSource().Token;
                    if (async)
                    {
                        executeAsync(binding, cancellationToken).GetAwaiter().GetResult();
                    }
                    else
                    {
                        execute(binding, cancellationToken);
                    }

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
                    session.ReferenceCount().Should().Be(1);
                }
            }
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
