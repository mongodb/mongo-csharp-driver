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
namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a fluent builder for one update request.
    /// </summary>
    public sealed class BulkUpdateRequestBuilder
    {
        // private fields
        private readonly Action<WriteRequest> _addRequest;
        private readonly IMongoQuery _query;
        private readonly bool _upsert;

        // constructors
        internal BulkUpdateRequestBuilder(Action<WriteRequest> addRequest, IMongoQuery query, bool upsert)
        {
            _addRequest = addRequest;
            _query = query;
            _upsert = upsert;
        }

        // public methods
        /// <summary>
        /// Adds an update request to replace one matching document to the bulk operation.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="document">The document.</param>
        public void ReplaceOne<TDocument>(TDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            var update = Builders.Update.Replace(document);
            Update(update, false);
        }

        /// <summary>
        /// Adds an update request to update all matching documents to the bulk operation.
        /// </summary>
        /// <param name="update">The update.</param>
        public void Update(IMongoUpdate update)
        {
            if (update == null)
            {
                throw new ArgumentNullException("update");
            }
            Update(update, true);
        }

        /// <summary>
        /// Adds an update request to update one matching document to the bulk operation.
        /// </summary>
        /// <param name="update">The update.</param>
        public void UpdateOne(IMongoUpdate update)
        {
            if (update == null)
            {
                throw new ArgumentNullException("update");
            }
            Update(update, false);
        }

        // private methods
        private void Update(IMongoUpdate update, bool multi)
        {
            var request = new UpdateRequest(_query, update)
            {
                IsMultiUpdate = multi,
                IsUpsert = _upsert
            };
            _addRequest(request);
        }
    }
}
