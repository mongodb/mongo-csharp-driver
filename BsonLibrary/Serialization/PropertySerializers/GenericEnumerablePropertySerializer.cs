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

namespace MongoDB.BsonLibrary.Serialization.PropertySerializers {
    public class GenericEnumerablePropertySerializer : IBsonPropertySerializer {
        #region private static fields
        private static GenericEnumerablePropertySerializer singleton = new GenericEnumerablePropertySerializer();
        #endregion

        #region constructors
        private GenericEnumerablePropertySerializer() {
        }
        #endregion

        #region public static properties
        public static GenericEnumerablePropertySerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterPropertySerializer() {
            BsonClassMap.RegisterPropertySerializer(typeof(HashSet<>), singleton);
            BsonClassMap.RegisterPropertySerializer(typeof(LinkedList<>), singleton);
            BsonClassMap.RegisterPropertySerializer(typeof(List<>), singleton);
            BsonClassMap.RegisterPropertySerializer(typeof(Queue<>), singleton);
            BsonClassMap.RegisterPropertySerializer(typeof(Stack<>), singleton);
            BsonClassMap.RegisterPropertySerializer(typeof(SynchronizedCollection<>), singleton);
            BsonClassMap.RegisterPropertySerializer(typeof(SynchronizedReadOnlyCollection<>), singleton);
            BsonClassMap.RegisterPropertySerializer(typeof(ICollection<>), singleton);
            BsonClassMap.RegisterPropertySerializer(typeof(IEnumerable<>), singleton);
            BsonClassMap.RegisterPropertySerializer(typeof(IList<>), singleton);
        }
        #endregion

        #region public methods
        public void DeserializeProperty(
            BsonReader bsonReader,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var propertyType = propertyMap.PropertyInfo.PropertyType;
            var elementType = propertyType.GetGenericArguments()[0];
            var genericDeserializePropertyHelperInfo = this.GetType().GetMethod("DeserializePropertyHelper");
            var deserializePropertyHelperInfo = genericDeserializePropertyHelperInfo.MakeGenericMethod(elementType);
            deserializePropertyHelperInfo.Invoke(this, new object[] { bsonReader, obj, propertyMap });
        }

        public void DeserializePropertyHelper<T>(
           BsonReader bsonReader,
           object obj,
           BsonPropertyMap propertyMap
       ) {
            bsonReader.ReadDocumentName(propertyMap.ElementName);

            var discriminator = bsonReader.FindString("_t");
            if (discriminator == null) {
                throw new FileFormatException("Discriminator missing");
            }
            var actualType = Type.GetType(discriminator);
            if (actualType == null) {
                string message = string.Format("GetType returned null for discriminator: {0}", discriminator);
                throw new BsonSerializationException(message);
            }
            if (!propertyMap.PropertyInfo.PropertyType.IsAssignableFrom(actualType)) {
                throw new BsonSerializationException("Incompatible type found");
            }
            var value = (ICollection<T>) Activator.CreateInstance(actualType); // deserialization requires ICollection<T> instead of IEnumerable<T>

            bsonReader.ReadStartDocument();
            bsonReader.VerifyString("_t", discriminator);
            bsonReader.ReadArrayName("v");
            bsonReader.ReadStartDocument();
            BsonType bsonType;
            string elementName;
            while (bsonReader.HasElement(out bsonType, out elementName)) {
                bsonReader.ReadDocumentName(elementName); // TODO: handle primitive element types
                T item = BsonSerializer.Deserialize<T>(bsonReader);
                value.Add(item);
            }
            bsonReader.ReadEndDocument();
            bsonReader.ReadEndDocument();
            propertyMap.Setter(obj, value);
        }

        public void SerializeProperty(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var propertyType = propertyMap.PropertyInfo.PropertyType;
            var elementType = propertyType.GetGenericArguments()[0];
            var genericSerializePropertyHelperInfo = this.GetType().GetMethod("SerializePropertyHelper");
            var serializePropertyHelperInfo = genericSerializePropertyHelperInfo.MakeGenericMethod(elementType);
            serializePropertyHelperInfo.Invoke(this, new object[] { bsonWriter, obj, propertyMap });
        }

        public void SerializePropertyHelper<T>(
            BsonWriter bsonWriter,
            object obj,
            BsonPropertyMap propertyMap
        ) {
            var value = (IEnumerable<T>) propertyMap.Getter(obj);
            var discriminator = BsonClassMapSerializer.GetDiscriminatorTypeName(value.GetType());

            bsonWriter.WriteDocumentName(propertyMap.ElementName);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteString("_t", discriminator);
            bsonWriter.WriteArrayName("v");
            bsonWriter.WriteStartDocument();
            int index = 0;
            foreach (var item in value) {
                bsonWriter.WriteDocumentName(index.ToString()); // TODO: handle primitive element types
                BsonSerializer.Serialize(bsonWriter, item);
                index++;
            }
            bsonWriter.WriteEndDocument();
            bsonWriter.WriteEndDocument();
        }
        #endregion
    }
}
