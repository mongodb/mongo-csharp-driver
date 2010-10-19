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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.BsonLibrary.DefaultSerializer {
    public class GenericEnumerableSerializer : IBsonSerializer {
        #region private static fields
        private static GenericEnumerableSerializer singleton = new GenericEnumerableSerializer();
        #endregion

        #region constructors
        private GenericEnumerableSerializer() {
        }
        #endregion

        #region public static properties
        public static GenericEnumerableSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(HashSet<>), singleton);
            BsonSerializer.RegisterSerializer(typeof(LinkedList<>), singleton);
            BsonSerializer.RegisterSerializer(typeof(List<>), singleton);
            BsonSerializer.RegisterSerializer(typeof(Queue<>), singleton);
            BsonSerializer.RegisterSerializer(typeof(Stack<>), singleton);
            BsonSerializer.RegisterSerializer(typeof(SynchronizedCollection<>), singleton);
            BsonSerializer.RegisterSerializer(typeof(SynchronizedReadOnlyCollection<>), singleton);
            BsonSerializer.RegisterSerializer(typeof(ICollection<>), singleton);
            BsonSerializer.RegisterSerializer(typeof(IEnumerable<>), singleton);
            BsonSerializer.RegisterSerializer(typeof(IList<>), singleton);
        }
        #endregion

        #region public methods
        public object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            // TODO: verify nominalType is IEnumerable<T>
            var elementType = nominalType.GetGenericArguments()[0];
            var deserializePropertyHelperDefinition = this.GetType().GetMethod("DeserializeDocumentHelper");
            var deserializePropertyHelperInfo = deserializePropertyHelperDefinition.MakeGenericMethod(elementType);
            return deserializePropertyHelperInfo.Invoke(this, new object[] { bsonReader, nominalType });
        }

        public object DeserializeDocumentHelper<T>(
           BsonReader bsonReader,
           Type nominalType
       ) {
            ICollection<T> value;
            var discriminator = bsonReader.FindString("_t");
            if (discriminator == null) {
                throw new FileFormatException("Discriminator missing");
            }
            var actualType = BsonClassMap.LookupActualType(nominalType, discriminator);
            value = (ICollection<T>) Activator.CreateInstance(actualType); // deserialization requires ICollection<T> instead of IEnumerable<T>

            bsonReader.ReadStartDocument();
            bsonReader.VerifyString("_t", discriminator);
            bsonReader.ReadArrayName("_v");
            bsonReader.ReadStartDocument();
            while (bsonReader.HasElement()) {
                string elementName; // element names are ignored on input
                T item = BsonSerializer.DeserializeElement<T>(bsonReader, out elementName);
                value.Add(item);
            }
            bsonReader.ReadEndDocument();
            bsonReader.ReadEndDocument();

            return value;
        }

        public object DeserializeElement(
            BsonReader bsonReader,
            Type type,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(out name);
                return null;
            } else {
                bsonReader.ReadDocumentName(out name);
                return DeserializeDocument(bsonReader, type);
            }
        }

        public void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            // TODO: verify nominalType is IEnumerable<T>
            var elementType = nominalType.GetGenericArguments()[0];
            var serializeDocumentHelperDefinition = this.GetType().GetMethod("SerializeDocumentHelper");
            var serializeDocumentHelperInfo = serializeDocumentHelperDefinition.MakeGenericMethod(elementType);
            serializeDocumentHelperInfo.Invoke(this, new object[] { bsonWriter, document });
        }

        public void SerializeDocumentHelper<T>(
            BsonWriter bsonWriter,
            object document
        ) {
            var value = (IEnumerable<T>) document;
            var discriminator = BsonClassMap.GetTypeNameDiscriminator(value.GetType());

            bsonWriter.WriteStartDocument();
            bsonWriter.WriteString("_t", discriminator);
            bsonWriter.WriteArrayName("_v");
            bsonWriter.WriteStartDocument();
            int index = 0;
            foreach (var item in value) {
                var elementName = index.ToString();
                BsonSerializer.SerializeElement(bsonWriter, elementName, item, false);
                index++;
            }
            bsonWriter.WriteEndDocument();
            bsonWriter.WriteEndDocument();
        }

        public void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value,
            bool useCompactRepresentation
        ) {
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteDocumentName(name);
                SerializeDocument(bsonWriter, nominalType, value, false);
            }
        }
        #endregion
    }
}
