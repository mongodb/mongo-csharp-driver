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
using System.Threading;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using MongoDB.Driver.Core.SyncExtensionMethods;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a fluent builder for a bulk operation.
    /// </summary>
    public sealed class BulkWriteOperation
    {
        // private fields
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

        // public methods
        /// <summary>
        /// Executes the bulk operation using the default write concern from the collection.
        /// </summary>
        /// <returns>A BulkWriteResult.</returns>
        public BulkWriteResult Execute()
        {
            return ExecuteHelper(_collection.Settings.WriteConcern ?? WriteConcern.Acknowledged);
        }

        /// <summary>
        /// Executes the bulk operation.
        /// </summary>
        /// <param name="writeConcern">The write concern for this bulk operation.</param>
        /// <returns>A BulkWriteResult.</returns>
        public BulkWriteResult Execute(WriteConcern writeConcern)
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
        /// <returns>A FluentWriteRequestBuilder.</returns>
        public BulkWriteRequestBuilder Find(IMongoQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (_hasBeenExecuted)
            {
                throw new InvalidOperationException("The bulk write operation has already been executed.");
            }
            return new BulkWriteRequestBuilder(AddRequest, query);
        }

        /// <summary>
        /// Adds an insert request for the specified document to the bulk operation.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="document">The document.</param>
        public void Insert<TDocument>(TDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            var serializer = BsonSerializer.LookupSerializer<TDocument>();
            var request = new InsertRequest(document, serializer);
            AddRequest(request);
        }

        // private methods
        private void AddRequest(WriteRequest request)
        {
            if (_hasBeenExecuted)
            {
                throw new InvalidOperationException("The bulk write operation has already been executed.");
            }
            _requests.Add(request);
        }

        private BulkWriteResult ExecuteHelper(WriteConcern writeConcern)
        {
            if (_hasBeenExecuted)
            {
                throw new InvalidOperationException("The bulk write operation has already been executed.");
            }
            _hasBeenExecuted = true;

            var assignId = _collection.Settings.AssignIdOnInsert ? (Action<object, IBsonSerializer>)_collection.AssignId : null;
            var collectionSettings = _collection.Settings;
            var messageEncoderSettings = _collection.GetMessageEncoderSettings();

            var requests = _requests.Select(r => r.ToCore());

            var operation = new BulkMixedWriteOperation(new CollectionNamespace(_collection.Database.Name, _collection.Name), requests, messageEncoderSettings)
            {
                AssignId = assignId,
                ElementNameValidator = CollectionElementNameValidator.Instance, // note: not configurable when using the fluent Bulk API
                IsOrdered = _isOrdered,
                WriteConcern = writeConcern
            };

            using (var binding = _collection.Database.Server.GetWriteBinding())
            {
                try
                {
                    var result = operation.Execute(binding, Timeout.InfiniteTimeSpan, CancellationToken.None);
                    return BulkWriteResult.FromCore(result);
                }
                catch (Core.BulkWriteException ex)
                {
                    throw BulkWriteException.FromCore(ex);
                }
            }
        }
    }
}
