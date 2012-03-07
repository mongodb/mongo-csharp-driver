﻿/* Copyright 2010-2012 10gen Inc.
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
    /// Represents serialization options for a document.
    /// </summary>
    public class DocumentSerializationOptions : IBsonSerializationOptions
    {
        // private static fields
        private static DocumentSerializationOptions __allowDuplicateNamesInstance = new DocumentSerializationOptions { AllowDuplicateNames = true };
        private static DocumentSerializationOptions __defaults = new DocumentSerializationOptions();
        private static DocumentSerializationOptions __serializeIdFirstInstance = new DocumentSerializationOptions { SerializeIdFirst = true };

        // private fields
        private bool _allowDuplicateNames;
        private bool _serializeIdFirst;

        // constructors
        /// <summary>
        /// Initializes a new instance of the DocumentSerializationOptions class.
        /// </summary>
        public DocumentSerializationOptions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the DocumentSerializationOptions class.
        /// </summary>
        /// <param name="serializeIdFirst">Whether to serialize the Id as the first element.</param>
        public DocumentSerializationOptions(bool serializeIdFirst)
        {
            _serializeIdFirst = serializeIdFirst;
        }

        // public static properties
        /// <summary>
        /// Gets an instance of DocumentSerializationOptions that specifies that duplicate names are allowed.
        /// </summary>
        public static DocumentSerializationOptions AllowDuplicateNamesInstance
        {
            get { return __allowDuplicateNamesInstance; }
        }

        /// <summary>
        /// Gets or sets the default document serialization options.
        /// </summary>
        public static DocumentSerializationOptions Defaults
        {
            get { return __defaults; }
            set { __defaults = value; }
        }

        /// <summary>
        /// Gets an instance of DocumentSerializationOptions that specifies to serialize the Id as the first element.
        /// </summary>
        public static DocumentSerializationOptions SerializeIdFirstInstance
        {
            get { return __serializeIdFirstInstance; }
        }

        // public properties
        /// <summary>
        /// Gets whether to allow duplicate names.
        /// </summary>
        public bool AllowDuplicateNames
        {
            get { return _allowDuplicateNames; }
            set { _allowDuplicateNames = value; }
        }

        /// <summary>
        /// Gets whether to serialize the Id as the first element.
        /// </summary>
        public bool SerializeIdFirst
        {
            get { return _serializeIdFirst; }
            set { _serializeIdFirst = value; }
        }
    }
}
