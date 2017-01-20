/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a fluent builder for a bulk operation.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class BulkWriteOperation<TDocument>
    {
        // private fields
        private bool? _bypassDocumentValidation;
        private readonly MongoCollection _collection;
        private readonly bool _isOrdered;
        private readonly List<WriteRequest> _requests = new List<WriteRequest>();
        private bool _hasBeenExecuted;

        // constructors
        internal BulkWriteOperation(MongoCollection collection, bool isOrdered)
        {
            _collection = collection;
            _isOrdered = isOrdered;
        }

        // public properties
        /// <summary>
        /// Gets or sets a value indicating whether to bypass document validation.
        /// </summary>
        /// <value>
        /// A value indicating whether to bypass document validation.
        /// </value>
        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

        // public methods
        /// <summary>
        /// Executes the bulk operation using the default write concern from the collection.
        /// </summary>
        /// <returns>A BulkWriteResult.</returns>
        public BulkWriteResult<TDocument> Execute()
        {
            return ExecuteHelper(_collection.Settings.WriteConcern ?? WriteConcern.Acknowledged);
        }

        /// <summary>
        /// Executes the bulk operation.
        /// </summary>
        /// <param name="writeConcern">The write concern for this bulk operation.</param>
        /// <returns>A BulkWriteResult.</returns>
        public BulkWriteResult<TDocument> Execute(WriteConcern writeConcern)
        {
            if (writeConcern == null)
            {
                throw new ArgumentNullException("writeConcern");
            }
            return ExecuteHelper(writeConcern);
        }

        /// <summary>
        /// Creates a builder for a new write request (either a remove or an update).
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="collation">The collation.</param>
        /// <returns>A FluentWriteRequestBuilder.</returns>
        public BulkWriteRequestBuilder<TDocument> Find(IMongoQuery query, Collation collation = null)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (_hasBeenExecuted)
            {
                throw new InvalidOperationException("The bulk write operation has already been executed.");
            }
            return new BulkWriteRequestBuilder<TDocument>(AddRequest, query, collation);
        }

        /// <summary>
        /// Adds an insert request for the specified document to the bulk operation.
        /// </summary>
        /// <param name="document">The document.</param>
        public void Insert(TDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            var serializer = BsonSerializer.LookupSerializer<TDocument>();
            var request = new InsertRequest(new BsonDocumentWrapper(document, serializer));
            AddRequest(request);
        }

        // private methods
        private void AddRequest(WriteRequest request)
        {
            if (_hasBeenExecuted)
            {
                throw new InvalidOperationException("The bulk write operation has already been executed.");
            }
            request.CorrelationId = _requests.Count;
            _requests.Add(request);
        }

        private BulkWriteResult<TDocument> ExecuteHelper(WriteConcern writeConcern)
        {
            if (_hasBeenExecuted)
            {
                throw new InvalidOperationException("The bulk write operation has already been executed.");
            }
            _hasBeenExecuted = true;

            var collectionSettings = _collection.Settings;
            var messageEncoderSettings = _collection.GetMessageEncoderSettings();

            IEnumerable<WriteRequest> requests = _requests;
            if (_collection.Settings.AssignIdOnInsert)
            {
                requests = _requests.Select(x =>
                {
                    var insertRequest = x as InsertRequest;
                    if (insertRequest != null)
                    {
                        object document = insertRequest.Document;
                        IBsonSerializer serializer = BsonDocumentSerializer.Instance;
                        var wrapped = insertRequest.Document as BsonDocumentWrapper;
                        while (wrapped != null)
                        {
                            document = wrapped.Wrapped;
                            serializer = wrapped.Serializer;
                            wrapped = document as BsonDocumentWrapper;
                        }

                        _collection.AssignId(document, serializer);
                    }

                    return x;
                });
            }

            var operation = new BulkMixedWriteOperation(new CollectionNamespace(_collection.Database.Name, _collection.Name), requests, messageEncoderSettings)
            {
                BypassDocumentValidation = _bypassDocumentValidation,
                IsOrdered = _isOrdered,
                WriteConcern = writeConcern
            };

            using (var binding = _collection.Database.Server.GetWriteBinding())
            {
                try
                {
                    var result = operation.Execute(binding, CancellationToken.None);
                    return BulkWriteResult<TDocument>.FromCore(result);
                }
                catch (MongoBulkWriteOperationException ex)
                {
                    throw MongoBulkWriteException<TDocument>.FromCore(ex);
                }
            }
        }
    }
}
