/* Copyright 2017-present MongoDB Inc.
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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    /// <summary>
    /// An UpdateDescription in a ChangeStreamDocument instance.
    /// </summary>
    public sealed class ChangeStreamUpdateDescription
    {
        // private fields
        private readonly BsonDocument _disambiguatedPaths;
        private readonly string[] _removedFields;
        private readonly BsonArray _truncatedArrays;
        private readonly BsonDocument _updatedFields;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeStreamUpdateDescription" /> class.
        /// </summary>
        /// <param name="updatedFields">The updated fields.</param>
        /// <param name="removedFields">The removed fields.</param>
        public ChangeStreamUpdateDescription(
            BsonDocument updatedFields,
            string[] removedFields)
            : this(updatedFields, removedFields, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeStreamUpdateDescription" /> class.
        /// </summary>
        /// <param name="updatedFields">The updated fields.</param>
        /// <param name="removedFields">The removed fields.</param>
        /// <param name="truncatedArrays">The truncated arrays.</param>
        public ChangeStreamUpdateDescription(
            BsonDocument updatedFields,
            string[] removedFields,
            BsonArray truncatedArrays)
            : this(updatedFields, removedFields, truncatedArrays, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeStreamUpdateDescription" /> class.
        /// </summary>
        /// <param name="updatedFields">The updated fields.</param>
        /// <param name="removedFields">The removed fields.</param>
        /// <param name="truncatedArrays">The truncated arrays.</param>
        /// <param name="disambiguatedPaths">The DisambiguatedPaths document.</param>
        public ChangeStreamUpdateDescription(
            BsonDocument updatedFields,
            string[] removedFields,
            BsonArray truncatedArrays,
            BsonDocument disambiguatedPaths)
        {
            _updatedFields = Ensure.IsNotNull(updatedFields, nameof(updatedFields));
            _removedFields = Ensure.IsNotNull(removedFields, nameof(removedFields));
            _truncatedArrays = truncatedArrays; // can be null
            _disambiguatedPaths = disambiguatedPaths;
        }

        /// <summary>
        /// Gets the disambiguated paths if present.
        /// </summary>
        /// <value>
        /// The disambiguated paths.
        /// </value>
        /// <remarks>
        /// <para>
        /// A document containing a map that associates an update path to an array containing the path components used in the update document. This data
        /// can be used in combination with the other fields in an <see cref="ChangeStreamDocument{TDocument}.UpdateDescription"/> to determine the
        /// actual path in the document that was updated. This is necessary in cases where a key contains dot-separated strings (i.e. <c>{ "a.b": "c" }</c>) or
        /// a document contains a numeric literal string key (i.e. <c>{ "a": { "0": "a" } }</c>). Note that in this scenario, the numeric key can't be the top
        /// level key because <c>{ "0": "a" }</c> is not ambiguous - update paths would simply be <c>'0'</c> which is unambiguous because BSON documents cannot have
        /// arrays at the top level. Each entry in the document maps an update path to an array which contains the actual path used when the document
        /// was updated. For example, given a document with the following shape <c>{ "a": { "0": 0 } }</c> and an update of <c>{ $inc: { "a.0": 1 } }</c>,
        /// <see cref="ChangeStreamDocument{TDocument}.DisambiguatedPaths"/> would look like the following:
        /// </para>
        /// <code>
        ///   {
        ///      "a.0": ["a", "0"]
        ///   }
        /// </code>
        /// <para>
        /// In each array, all elements will be returned as strings with the exception of array indices, which will be returned as 32-bit integers.
        /// </para>
        /// <para>
        /// Added in MongoDB version 6.1.0.
        /// </para>
        /// </remarks>
        public BsonDocument DisambiguatedPaths => _disambiguatedPaths;

        // public properties
        /// <summary>
        /// Gets the removed fields.
        /// </summary>
        /// <value>
        /// The removed fields.
        /// </value>
        public string[] RemovedFields => _removedFields;

        /// <summary>
        /// Gets the truncated arrays.
        /// </summary>
        /// <value>
        /// The truncated arrays.
        /// </value>
        public BsonArray TruncatedArrays => _truncatedArrays;

        /// <summary>
        /// Gets the updated fields.
        /// </summary>
        /// <value>
        /// The updated fields.
        /// </value>
        public BsonDocument UpdatedFields => _updatedFields;

        // public methods
        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(ChangeStreamUpdateDescription))
            {
                return false;
            }

            var other = (ChangeStreamUpdateDescription)obj;
            return
                _removedFields.SequenceEqual(other._removedFields) &&
                _updatedFields.Equals(other._updatedFields) &&
                object.Equals(_truncatedArrays, other._truncatedArrays) &&
                object.Equals(_disambiguatedPaths, other._disambiguatedPaths);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return new Hasher()
                .HashElements(_removedFields)
                .Hash(_updatedFields)
                .Hash(_truncatedArrays)
                .Hash(_disambiguatedPaths)
                .GetHashCode();
        }
    }
}
