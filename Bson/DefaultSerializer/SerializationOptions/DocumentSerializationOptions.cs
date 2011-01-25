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

namespace MongoDB.Bson.Serialization {
    public class DocumentSerializationOptions : IBsonSerializationOptions {
        #region private static fields
        private static DocumentSerializationOptions defaults = new DocumentSerializationOptions(false);
        private static DocumentSerializationOptions serializeIdFirstInstance = new DocumentSerializationOptions(true);
        #endregion

        #region private fields
        private bool serializeIdFirst;
        #endregion

        #region constructors
        public DocumentSerializationOptions(
            bool serializeIdFirst
        ) {
            this.serializeIdFirst = serializeIdFirst;
        }
        #endregion

        #region public static properties
        public static DocumentSerializationOptions Defaults {
            get { return defaults; }
        }

        public static DocumentSerializationOptions SerializeIdFirstInstance {
            get { return serializeIdFirstInstance; }
        }
        #endregion

        #region public properties
        public bool SerializeIdFirst {
            get { return serializeIdFirst; }
        }
        #endregion
    }
}
