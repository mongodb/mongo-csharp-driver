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
    /// Represents serialization options for a Dictionary value.
    /// </summary>
    public class DictionarySerializationOptions : BsonBaseSerializationOptions
    {
        // private static fields
        private static DictionarySerializationOptions __defaults = new DictionarySerializationOptions();
        private static DictionarySerializationOptions __dynamic = new DictionarySerializationOptions(DictionaryRepresentation.Dynamic);
        private static DictionarySerializationOptions __document = new DictionarySerializationOptions(DictionaryRepresentation.Document);
        private static DictionarySerializationOptions __arrayOfArrays = new DictionarySerializationOptions(DictionaryRepresentation.ArrayOfArrays);
        private static DictionarySerializationOptions __arrayOfDocuments = new DictionarySerializationOptions(DictionaryRepresentation.ArrayOfDocuments);

        // private fields
        private DictionaryRepresentation _representation = DictionaryRepresentation.Dynamic;
        private KeyValuePairSerializationOptions _keyValuePairSerializationOptions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the DictionarySerializationOptions class.
        /// </summary>
        public DictionarySerializationOptions()
        {
            _keyValuePairSerializationOptions = (KeyValuePairSerializationOptions)KeyValuePairSerializationOptions.Defaults.Clone();
        }

        /// <summary>
        /// Initializes a new instance of the DictionarySerializationOptions class.
        /// </summary>
        /// <param name="representation">The representation to use for a Dictionary.</param>
        public DictionarySerializationOptions(DictionaryRepresentation representation)
        {
            _representation = representation;
            _keyValuePairSerializationOptions = (KeyValuePairSerializationOptions)KeyValuePairSerializationOptions.Defaults.Clone();
        }

        /// <summary>
        /// Initializes a new instance of the DictionarySerializationOptions class.
        /// </summary>
        /// <param name="representation">The representation to use for a Dictionary.</param>
        /// <param name="keyValuePairSerializationOptions">The serialization options for the key/value pairs in the dictionary.</param>
        public DictionarySerializationOptions(DictionaryRepresentation representation, KeyValuePairSerializationOptions keyValuePairSerializationOptions)
        {
            if (keyValuePairSerializationOptions == null)
            {
                throw new ArgumentNullException("keyValuePairSerializationOptions");
            }
            _representation = representation;
            _keyValuePairSerializationOptions = keyValuePairSerializationOptions;
        }

        // public static properties
        /// <summary>
        /// Gets an instance of DictionarySerializationOptions with Representation=ArrayOfArrays.
        /// </summary>
        public static DictionarySerializationOptions ArrayOfArrays
        {
            get { return __arrayOfArrays; }
        }

        /// <summary>
        /// Gets an instance of DictionarySerializationOptions with Representation=ArrayOfDocuments.
        /// </summary>
        public static DictionarySerializationOptions ArrayOfDocuments
        {
            get { return __arrayOfDocuments; }
        }

        /// <summary>
        /// Gets or sets the default Dictionary serialization options.
        /// </summary>
        [Obsolete("Create and register a DictionarySerializer with the desired options instead.")]
        public static DictionarySerializationOptions Defaults
        {
            get { return __defaults; }
            set {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                __defaults = value;
            }
        }

        /// <summary>
        /// Gets an instance of DictionarySerializationOptions with Representation=Document.
        /// </summary>
        public static DictionarySerializationOptions Document
        {
            get { return __document; }
        }

        /// <summary>
        /// Gets an instance of DictionarySerializationOptions with Representation=Dynamic.
        /// </summary>
        public static DictionarySerializationOptions Dynamic
        {
            get { return __dynamic; }
        }

        // public properties
        /// <summary>
        /// Gets or sets the serialization options for the values in the dictionary.
        /// </summary>
        [Obsolete("Use KeyValuePairSerializationOptions instead.")]
        public IBsonSerializationOptions ItemSerializationOptions
        {
            get { return _keyValuePairSerializationOptions.ValueSerializationOptions; }
            set {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _keyValuePairSerializationOptions = new KeyValuePairSerializationOptions(
                    _keyValuePairSerializationOptions.Representation,
                    _keyValuePairSerializationOptions.KeySerializationOptions,
                    value);
            }
        }

        /// <summary>
        /// Gets the representation to use for a Dictionary.
        /// </summary>
        public DictionaryRepresentation Representation
        {
            get { return _representation; }
            set
            {
                EnsureNotFrozen();
                _representation = value;
            }
        }

        /// <summary>
        /// Gets or sets the serialization options for the values in the dictionary.
        /// </summary>
        public KeyValuePairSerializationOptions KeyValuePairSerializationOptions
        {
            get { return _keyValuePairSerializationOptions; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                EnsureNotFrozen();
                _keyValuePairSerializationOptions = value;
            }
        }

        // public methods
        /// <summary>
        /// Clones the serialization options.
        /// </summary>
        /// <returns>A cloned copy of the serialization options.</returns>
        public override IBsonSerializationOptions Clone()
        {
            return new DictionarySerializationOptions(_representation, _keyValuePairSerializationOptions);
        }

        /// <summary>
        /// Freezes the serialization options.
        /// </summary>
        /// <returns>The frozen serialization options.</returns>
        public override IBsonSerializationOptions Freeze()
        {
            if (!IsFrozen)
            {
                if (_keyValuePairSerializationOptions != null)
                {
                    _keyValuePairSerializationOptions.Freeze();
                }
            }
            return base.Freeze();
        }
    }
}
