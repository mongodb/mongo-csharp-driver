﻿/* Copyright 2010-2011 10gen Inc.
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

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers {
    /// <summary>
    /// Represents a serializer for enums.
    /// </summary>
    public class EnumSerializer : BsonBaseSerializer {
        #region private static fields
        private static EnumSerializer instance = new EnumSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the EnumSerializer class.
        /// </summary>
        public EnumSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the EnumSerializer class.
        /// </summary>
        public static EnumSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            VerifyDeserializeType(nominalType);
            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Int32: return Enum.ToObject(nominalType, bsonReader.ReadInt32());
                case BsonType.Int64: return Enum.ToObject(nominalType, bsonReader.ReadInt64());
                case BsonType.Double: return Enum.ToObject(nominalType, (long) bsonReader.ReadDouble());
                case BsonType.String: return Enum.Parse(nominalType, bsonReader.ReadString());
                default:
                    var message = string.Format("Cannot deserialize {0} from BsonType: {1}", nominalType.FullName, bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var actualType = value.GetType();
            VerifySerializeTypes(nominalType, actualType);
            var representation = (options == null) ? 0 : ((RepresentationSerializationOptions) options).Representation;
            switch (representation) {
                case 0:
                    var underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(actualType));
                    if (underlyingTypeCode == TypeCode.Int64 || underlyingTypeCode == TypeCode.UInt64) {
                        goto case BsonType.Int64;
                    } else {
                        goto case BsonType.Int32;
                    }
                case BsonType.Int32:
                    bsonWriter.WriteInt32(Convert.ToInt32(value));
                    break;
                case BsonType.Int64:
                    bsonWriter.WriteInt64(Convert.ToInt64(value));
                    break;
                case BsonType.String:
                    bsonWriter.WriteString(value.ToString());
                    break;
                default:
                    throw new BsonInternalException("Unexpected EnumRepresentation");
            }
        }
        #endregion

        #region private methods
        private void VerifyDeserializeType(
            Type nominalType
        ) {
            if (!nominalType.IsEnum) {
                var message = string.Format("EnumSerializer.Deserialize cannot be used with nominal type: {0}", nominalType.FullName);
                throw new BsonSerializationException(message);
            }
        }

        private void VerifySerializeTypes(
            Type nominalType,
            Type actualType
        ) {
            // problem is that the actual type of a nullable type that is not null will appear as the non-nullable type
            // we want to allow both Enum and Nullable<Enum> here 
            bool isNullableOfActual = (
                nominalType.IsGenericType &&
                nominalType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                nominalType.GetGenericArguments().Single() == actualType
            );
            if (nominalType != typeof(object) && nominalType != actualType && !isNullableOfActual) {
                var message = string.Format("EnumSerializer.Serialize cannot be used with nominal type: {0}", nominalType.FullName);
                throw new BsonSerializationException(message);
            }           
            
            if (!actualType.IsEnum) {
                var message = string.Format("EnumSerializer.Serialize cannot be used with actual type: {0}", actualType.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }
}
