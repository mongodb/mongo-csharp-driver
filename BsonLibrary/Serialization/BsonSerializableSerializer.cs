/* Copyright 2010 10gen Inc.
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

using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.Serialization {
    public class BsonSerializableSerializer : IBsonSerializer {
        #region private static fields
        private static BsonSerializableSerializer singleton = new BsonSerializableSerializer();
        #endregion

        #region constructors
        private BsonSerializableSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonSerializableSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public methods
        public object Deserialize(
            BsonReader bsonReader,
            Type type
        ) {
            var obj = Activator.CreateInstance(type);
            ((IBsonSerializable) obj).Deserialize(bsonReader);
            return obj;
        }

        public void Serialize(
            BsonWriter bsonWriter,
            object obj,
            bool serializeIdFirst,
            bool serializeDiscriminator
        ) {
            ((IBsonSerializable) obj).Serialize(bsonWriter, serializeIdFirst, serializeDiscriminator);
        }
        #endregion
    }
}
