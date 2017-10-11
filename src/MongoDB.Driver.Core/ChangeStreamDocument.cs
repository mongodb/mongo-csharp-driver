/* Copyright 2017 MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// An output document from a $changeStream pipeline stage.
    /// </summary>
    public sealed class ChangeStreamDocument<TDocument>
    {
        // private fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly BsonDocument _documentKey;
        private readonly TDocument _fullDocument;
        private readonly ChangeStreamOperationType _operationType;
        private readonly BsonDocument _resumeToken;
        private readonly ChangeStreamUpdateDescription _updateDescription;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeStreamDocument{TDocument}" /> class.
        /// </summary>
        /// <param name="resumeToken">The resume token.</param>
        /// <param name="operationType">Type of the operation.</param>
        /// <param name="collectionNamespace">Namespace of the collection.</param>
        /// <param name="documentKey">The document key.</param>
        /// <param name="updateDescription">The update description.</param>
        /// <param name="fullDocument">The full document.</param>
        public ChangeStreamDocument(
            BsonDocument resumeToken,
            ChangeStreamOperationType operationType,
            CollectionNamespace collectionNamespace,
            BsonDocument documentKey,
            ChangeStreamUpdateDescription updateDescription,
            TDocument fullDocument)
        {
            _resumeToken = Ensure.IsNotNull(resumeToken, nameof(resumeToken));
            _operationType = operationType;
            _collectionNamespace = collectionNamespace; // can be null when operationType is Invalidate
            _documentKey = documentKey; // can be null
            _updateDescription = updateDescription; // can be null
            _fullDocument = fullDocument; // can be null
        }

        // public properties
        /// <summary>
        /// Gets the namespace of the collection.
        /// </summary>
        /// <value>
        /// The namespace of the collection.
        /// </value>
        public CollectionNamespace CollectionNamespace => _collectionNamespace;

        /// <summary>
        /// Gets the document key.
        /// </summary>
        /// <value>
        /// The document key.
        /// </value>
        public BsonDocument DocumentKey => _documentKey;

        /// <summary>
        /// Gets the full document.
        /// </summary>
        /// <value>
        /// The full document.
        /// </value>
        public TDocument FullDocument => _fullDocument;

        /// <summary>
        /// Gets the type of the operation.
        /// </summary>
        /// <value>
        /// The type of the operation.
        /// </value>
        public ChangeStreamOperationType OperationType => _operationType;

        /// <summary>
        /// Gets the resume token.
        /// </summary>
        /// <value>
        /// The resume token.
        /// </value>
        public BsonDocument ResumeToken => _resumeToken;

        /// <summary>
        /// Gets the update description.
        /// </summary>
        /// <value>
        /// The update description.
        /// </value>
        public ChangeStreamUpdateDescription UpdateDescription => _updateDescription;
    }
}
