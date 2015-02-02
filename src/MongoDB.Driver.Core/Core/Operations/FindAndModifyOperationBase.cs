/* Copyright 2013-2014 MongoDB Inc.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a base class for find and modify operations.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public abstract class FindAndModifyOperationBase<TResult> : IWriteOperation<TResult>
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FindAndModifyOperationBase{TResult}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public FindAndModifyOperationBase(CollectionNamespace collectionNamespace, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
        }

        // properties
        /// <summary>
        /// Gets the collection namespace.
        /// </summary>
        /// <value>
        /// The collection namespace.
        /// </value>
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        /// <summary>
        /// Gets the message encoder settings.
        /// </summary>
        /// <value>
        /// The message encoder settings.
        /// </value>
        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        /// <summary>
        /// Gets the result serializer.
        /// </summary>
        /// <value>
        /// The result serializer.
        /// </value>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        // methods
        internal abstract BsonDocument CreateCommand();

        /// <inheritdoc/>
        public Task<TResult> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation<TResult>(_collectionNamespace.DatabaseNamespace, command, _resultSerializer, _messageEncoderSettings)
            {
                CommandValidator = GetCommandValidator()
            };
            return operation.ExecuteAsync(binding, cancellationToken);
        }

        /// <summary>
        /// Gets the command validator.
        /// </summary>
        /// <returns>An element name validator for the command.</returns>
        protected abstract IElementNameValidator GetCommandValidator();
    }
}
