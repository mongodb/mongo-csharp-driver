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
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization.Options
{
    /// <summary>
    /// Represents serialization options for an Array value.
    /// </summary>
    public class ArraySerializationOptions : BsonBaseSerializationOptions
    {
        // private fields
        private IBsonSerializationOptions _itemSerializationOptions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ArraySerializationOptions class.
        /// </summary>
        public ArraySerializationOptions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ArraySerializationOptions class.
        /// </summary>
        /// <param name="itemSerializationOptions">The serialization options to use for items in the array.</param>
        public ArraySerializationOptions(IBsonSerializationOptions itemSerializationOptions)
        {
            _itemSerializationOptions = itemSerializationOptions;
        }

        // public properties
        /// <summary>
        /// Gets or sets the serialization options for the items in the array.
        /// </summary>
        public IBsonSerializationOptions ItemSerializationOptions
        {
            get { return _itemSerializationOptions; }
            set
            {
                EnsureNotFrozen();
                _itemSerializationOptions = value;
            }
        }

        // public methods
        /// <summary>
        /// Clones the serialization options.
        /// </summary>
        /// <returns>A cloned copy of the serialization options.</returns>
        public override IBsonSerializationOptions Clone()
        {
            return new ArraySerializationOptions(_itemSerializationOptions);
        }

        /// <summary>
        /// Freezes the serialization options.
        /// </summary>
        /// <returns>The frozen serialization options.</returns>
        public override IBsonSerializationOptions Freeze()
        {
            if (!IsFrozen)
            {
                if (_itemSerializationOptions != null)
                {
                    _itemSerializationOptions.Freeze();
                }
            }
            return base.Freeze();
        }
    }
}
