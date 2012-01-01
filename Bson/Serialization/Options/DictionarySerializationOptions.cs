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
    public class DictionarySerializationOptions : IBsonSerializationOptions
    {
        // private static fields
        private static DictionarySerializationOptions __defaults = new DictionarySerializationOptions();
        private static DictionarySerializationOptions __dynamic = new DictionarySerializationOptions(DictionaryRepresentation.Dynamic);
        private static DictionarySerializationOptions __document = new DictionarySerializationOptions(DictionaryRepresentation.Document);
        private static DictionarySerializationOptions __arrayOfArrays = new DictionarySerializationOptions(DictionaryRepresentation.ArrayOfArrays);
        private static DictionarySerializationOptions __arrayOfDocuments = new DictionarySerializationOptions(DictionaryRepresentation.ArrayOfDocuments);

        // private fields
        private DictionaryRepresentation _representation = DictionaryRepresentation.Dynamic;

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
        }
    }
}
