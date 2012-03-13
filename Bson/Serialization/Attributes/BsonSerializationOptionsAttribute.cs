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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Attributes
{
    /// <summary>
    /// Specifies the serialization options for this class (see derived attributes).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public abstract class BsonSerializationOptionsAttribute : Attribute
    {
        // private fields
        private bool _isItemOptions;
        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonSerializationOptionsAttribute class.
        /// </summary>
        protected BsonSerializationOptionsAttribute()
        {
        }

        // public properties
        /// <summary>
        /// Gets or sets whether this attribute should be applied to the items of the collection rather than the collection itself.
        /// </summary>
        public bool IsItemOptions
        {
            get { return _isItemOptions; }
            set { _isItemOptions = value; }
        }

        // public methods
        /// <summary>
        /// Gets the serialization options specified by this attribute.
        /// </summary>
        /// <returns>The serialization options.</returns>
        public abstract IBsonSerializationOptions GetOptions();

        // protected methods
        /// <summary>
        /// Wraps the serialization options in an ItemSerializationOptionsWrapper if IsItemOptions is true.
        /// </summary>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns>The serialization options wrapped in an ItemSerializationOptionsWrapper if IsItemOptions is true; otherwise, the original serialization options.</returns>
        protected IBsonSerializationOptions CheckIfIsItemsOptions(IBsonSerializationOptions serializationOptions)
        {
            if (_isItemOptions)
            {
                return new ItemSerializationOptionsWrapper(serializationOptions);
            }
            else
            {
                return serializationOptions;
            }
        }
    }
}
