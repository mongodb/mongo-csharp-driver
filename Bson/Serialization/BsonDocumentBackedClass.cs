/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// A class backed by a BsonDocument.
    /// </summary>
    public abstract class BsonDocumentBackedClass
    {
        // private fields
        private readonly BsonDocument _backingDocument;
        private readonly IBsonDocumentSerializer _serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentBackedClass"/> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        protected BsonDocumentBackedClass(IBsonDocumentSerializer serializer)
            : this(new BsonDocument(), serializer)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentBackedClass"/> class.
        /// </summary>
        /// <param name="backingDocument">The backing document.</param>
        /// <param name="serializer">The serializer.</param>
        protected BsonDocumentBackedClass(BsonDocument backingDocument, IBsonDocumentSerializer serializer)
        {
            if (backingDocument == null)
            {
                throw new ArgumentNullException("backingDocument");
            }
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            _backingDocument = backingDocument;
            _serializer = serializer;
        }

        // protected properties
        protected BsonDocument BackingDocument
        {
            get { return _backingDocument; }
        }

        // protected methods
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <returns></returns>
        protected BsonValue GetValue(string memberName)
        {
            var info = _serializer.GetMemberSerializationInfo(memberName);
            return _backingDocument.GetValue(info.ElementName);
        }

        /// <summary>
        /// Gets the value from the backing document.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        protected BsonValue GetValue(string memberName, BsonValue defaultValue)
        {
            var info = _serializer.GetMemberSerializationInfo(memberName);
            return _backingDocument.GetValue(info.ElementName, defaultValue);
        }

        /// <summary>
        /// Tries to get a value from the backing document.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        protected bool TryGetValue(string memberName, out BsonValue value)
        {
            var info = _serializer.GetMemberSerializationInfo(memberName);
            return _backingDocument.TryGetValue(info.ElementName, out value);
        }

        /// <summary>
        /// Sets the value in the backing document.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="value">The value.</param>
        protected void SetValue(string memberName, BsonValue value)
        {
            var info = _serializer.GetMemberSerializationInfo(memberName);
            _backingDocument.Set(info.ElementName, value);
        }
    }
}