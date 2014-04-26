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
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a request to insert a document.
    /// </summary>
    public class InsertRequest : WriteRequest // TODO: use InsertRequest<TDocument> and set serialization options once for the whole operation?
    {
        // private fields
        private object _document;
        private Type _nominalType;
        private IBsonSerializer _serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InsertRequest"/> class.
        /// </summary>
        public InsertRequest()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InsertRequest" /> class.
        /// </summary>
        /// <param name="nominalType">Type nominal type.</param>
        /// <param name="document">The document.</param>
        public InsertRequest(Type nominalType, object document)
            : this(nominalType, document, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InsertRequest" /> class.
        /// </summary>
        /// <param name="nominalType">Type nominal type.</param>
        /// <param name="document">The document.</param>
        /// <param name="serializer">The serializer.</param>
        public InsertRequest(Type nominalType, object document, IBsonSerializer serializer)
            : base(WriteRequestType.Insert)
        {
            _nominalType = nominalType;
            _document = document;
            _serializer = serializer;
        }

        // public properties
        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        /// <value>
        /// The document.
        /// </value>
        public object Document
        {
            get { return _document; }
            set { _document = value; }
        }

        /// <summary>
        /// Gets or sets the nominal type.
        /// </summary>
        /// <value>
        /// The nominal type.
        /// </value>
        public Type NominalType
        {
            get { return _nominalType; }
            set { _nominalType = value; }
        }

        /// <summary>
        /// Gets or sets the serializer.
        /// </summary>
        /// <value>
        /// The serializer.
        /// </value>
        public IBsonSerializer Serializer
        {
            get { return _serializer; }
            set { _serializer = value; }
        }
    }
}
