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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a fluent builder for a write request (either a remove or an update).
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class BulkWriteRequestBuilder<TDocument>
    {
        // private fields
        private readonly Action<WriteRequest> _addRequest;
        private readonly Collation _collation;
        private readonly IMongoQuery _query;

        // constructors
        internal BulkWriteRequestBuilder(Action<WriteRequest> addRequest, IMongoQuery query, Collation collation)
        {
            _addRequest = addRequest;
            _query = query;
            _collation = collation;
        }

        // public methods
        /// <summary>
        /// Adds a request to remove all matching documents to the bulk operation.
        /// </summary>
        public void Remove()
        {
            var request = new DeleteRequest(new BsonDocumentWrapper(_query))
            {
                Collation = _collation,
                Limit = 0
            };
            _addRequest(request);
        }

        /// <summary>
        /// Adds a request to remove one matching documents to the bulk operation.
        /// </summary>
        public void RemoveOne()
        {
            var request = new DeleteRequest(new BsonDocumentWrapper(_query))
            {
                Collation = _collation,
                Limit = 1
            };
            _addRequest(request);
        }

        /// <summary>
        /// Adds a request to replace one matching documents to the bulk operation.
        /// </summary>
        /// <param name="document">The document.</param>
        public void ReplaceOne(TDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            new BulkUpdateRequestBuilder<TDocument>(_addRequest, _query, _collation, false).ReplaceOne(document);
        }

        /// <summary>
        /// Adds a request to update all matching documents to the bulk operation.
        /// </summary>
        /// <param name="update">The update.</param>
        public void Update(IMongoUpdate update)
        {
            if (update == null)
            {
                throw new ArgumentNullException("update");
            }
            new BulkUpdateRequestBuilder<TDocument>(_addRequest, _query, _collation, false).Update(update);
        }

        /// <summary>
        /// Adds a request to update one matching documents to the bulk operation.
        /// </summary>
        /// <param name="update">The update.</param>
        public void UpdateOne(IMongoUpdate update)
        {
            if (update == null)
            {
                throw new ArgumentNullException("update");
            }
            new BulkUpdateRequestBuilder<TDocument>(_addRequest, _query, _collation, false).UpdateOne(update);
        }

        /// <summary>
        /// Specifies that the request being built should be an upsert.
        /// </summary>
        /// <returns>A bulk update request builder.</returns>
        public BulkUpdateRequestBuilder<TDocument> Upsert()
        {
            return new BulkUpdateRequestBuilder<TDocument>(_addRequest, _query, _collation, true);
        }
    }
}
