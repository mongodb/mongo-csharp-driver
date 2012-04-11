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

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization.Options
{
    /// <summary>
    /// Represents the representation to use for dictionaries.
    /// </summary>
    public enum DictionaryRepresentation
    {
        /// <summary>
        /// Represent the dictionary as a document if the keys are strings and valid element names, otherwise as an array of arrays.
        /// </summary>
        Dynamic,
        /// <summary>
        /// Represent the dictionary as a Document.
        /// </summary>
        Document,
        /// <summary>
        /// Represent the dictionary as an array of arrays.
        /// </summary>
        ArrayOfArrays,
        /// <summary>
        /// Represent the dictionary as an array of documents.
        /// </summary>
        ArrayOfDocuments
    }

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
        private IBsonSerializationOptions _itemSerializationOptions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the DictionarySerializationOptions class.
        /// </summary>
        public DictionarySerializationOptions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the DictionarySerializationOptions class.
        /// </summary>
        /// <param name="representation">The representation to use for a Dictionary.</param>
        public DictionarySerializationOptions(DictionaryRepresentation representation)
        {
            _representation = representation;
        }

        /// <summary>
        /// Initializes a new instance of the DictionarySerializationOptions class.
        /// </summary>
        /// <param name="representation">The representation to use for a Dictionary.</param>
        /// <param name="itemSerializationOptions">The serialization options for the items in the dictionary.</param>
        public DictionarySerializationOptions(DictionaryRepresentation representation, IBsonSerializationOptions itemSerializationOptions)
        {
            _representation = representation;
            _itemSerializationOptions = itemSerializationOptions;
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
        public static DictionarySerializationOptions Defaults
        {
            get { return __defaults; }
            set { __defaults = value; }
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
        /// Gets or sets the serialization options for the items in the dictionary.
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
        /// Apply an attribute to these serialization options and modify the options accordingly.
        /// </summary>
        /// <param name="serializer">The serializer that these serialization options are for.</param>
        /// <param name="attribute">The serialization options attribute.</param>
        public override void ApplyAttribute(IBsonSerializer serializer, Attribute attribute)
        {
            EnsureNotFrozen();
            var dictionaryOptionsAttribute = attribute as BsonDictionaryOptionsAttribute;
            if (dictionaryOptionsAttribute != null)
            {
                _representation = dictionaryOptionsAttribute.Representation;
                return;
            }

            // for backward compatibility reasons representations Array and Document apply to the Dictionary and not the items
            var representationAttribute = attribute as BsonRepresentationAttribute;
            if (representationAttribute != null)
            {
                switch (representationAttribute.Representation)
                {
                    case BsonType.Array:
                        _representation = DictionaryRepresentation.ArrayOfArrays;
                        return;
                    case BsonType.Document:
                        _representation = DictionaryRepresentation.Document;
                        return;
                }
            }

            var itemSerializer = serializer.GetItemSerializationInfo().Serializer;
            if (_itemSerializationOptions == null)
            {
                var itemDefaultSerializationOptions = itemSerializer.GetDefaultSerializationOptions();

                // special case for legacy dictionaries: allow BsonRepresentation on object
                if (itemDefaultSerializationOptions == null && 
                    serializer.GetType() == typeof(DictionarySerializer) && 
                    attribute.GetType() == typeof(BsonRepresentationAttribute))
                {
                    itemDefaultSerializationOptions = new RepresentationSerializationOptions(BsonType.Null); // will be modified later by ApplyAttribute
                }

                if (itemDefaultSerializationOptions == null)
                {
                    var message = string.Format(
                        "A serialization options attribute of type {0} cannot be used when the serializer is of type {1} and the item serializer is of type {2}.",
                        BsonUtils.GetFriendlyTypeName(attribute.GetType()),
                        BsonUtils.GetFriendlyTypeName(serializer.GetType()),
                        BsonUtils.GetFriendlyTypeName(itemSerializer.GetType()));
                    throw new NotSupportedException(message);
                }

                _itemSerializationOptions = itemDefaultSerializationOptions.Clone();
            }

            _itemSerializationOptions.ApplyAttribute(itemSerializer, attribute);
        }

        /// <summary>
        /// Clones the serialization options.
        /// </summary>
        /// <returns>A cloned copy of the serialization options.</returns>
        public override IBsonSerializationOptions Clone()
        {
            return new DictionarySerializationOptions(_representation, _itemSerializationOptions);
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
