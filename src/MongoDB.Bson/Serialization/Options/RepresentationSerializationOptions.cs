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
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Bson.Serialization.Options
{
    /// <summary>
    /// Represents the external representation of a field or property.
    /// </summary>
    public class RepresentationSerializationOptions : BsonBaseSerializationOptions
    {
        // private fields
        private BsonType _representation;
        private bool _allowOverflow;
        private bool _allowTruncation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the RepresentationSerializationOptions class.
        /// </summary>
        /// <param name="representation">The external representation.</param>
        public RepresentationSerializationOptions(BsonType representation)
        {
            _representation = representation;
        }

        /// <summary>
        /// Initializes a new instance of the RepresentationSerializationOptions class.
        /// </summary>
        /// <param name="representation">The external representation.</param>
        /// <param name="allowOverflow">Whether to allow overflow.</param>
        /// <param name="allowTruncation">Whether to allow truncation.</param>
        public RepresentationSerializationOptions(BsonType representation, bool allowOverflow, bool allowTruncation)
        {
            _representation = representation;
            _allowOverflow = allowOverflow;
            _allowTruncation = allowTruncation;
        }

        // public properties
        /// <summary>
        /// Gets the external representation.
        /// </summary>
        public BsonType Representation
        {
            get { return _representation; }
            set
            {
                EnsureNotFrozen();
                _representation = value;
            }
        }

        /// <summary>
        /// Gets whether to allow overflow.
        /// </summary>
        public bool AllowOverflow
        {
            get { return _allowOverflow; }
            set
            {
                EnsureNotFrozen();
                _allowOverflow = value;
            }
        }

        /// <summary>
        /// Gets whether to allow truncation.
        /// </summary>
        public bool AllowTruncation
        {
            get { return _allowTruncation; }
            set
            {
                EnsureNotFrozen();
                _allowTruncation = value;
            }
        }

        // public methods
        /// <summary>
        /// Clones the serialization options.
        /// </summary>
        /// <returns>A cloned copy of the serialization options.</returns>
        public override IBsonSerializationOptions Clone()
        {
            return new RepresentationSerializationOptions(_representation);
        }
    }
}
