/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.Bson.Serialization.Options {
    /// <summary>
    /// Represents serialization options for a document.
    /// </summary>
    public class DocumentSerializationOptions : IBsonSerializationOptions {
        #region private static fields
        private static DocumentSerializationOptions defaults = new DocumentSerializationOptions(false);
        private static DocumentSerializationOptions serializeIdFirstInstance = new DocumentSerializationOptions(true);
        #endregion

        #region private fields
        private bool serializeIdFirst;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the DocumentSerializationOptions class.
        /// </summary>
        /// <param name="serializeIdFirst">Whether to serialize the Id as the first element.</param>
        public DocumentSerializationOptions(
            bool serializeIdFirst
        ) {
            this.serializeIdFirst = serializeIdFirst;
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets or sets the default document serialization options.
        /// </summary>
        public static DocumentSerializationOptions Defaults {
            get { return defaults; }
            set { defaults = value; }
        }

        /// <summary>
        /// Gets an instance of DocumentSerializationOptions that specifies to serialize the Id as the first element.
        /// </summary>
        public static DocumentSerializationOptions SerializeIdFirstInstance {
            get { return serializeIdFirstInstance; }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets whether to serialize the Id as the first element.
        /// </summary>
        public bool SerializeIdFirst {
            get { return serializeIdFirst; }
        }
        #endregion
    }
}
