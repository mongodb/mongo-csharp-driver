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

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public class EnumSerializer : BsonBaseSerializer {
        #region private static fields
        private static EnumSerializer intRepresentation = new EnumSerializer(BsonType.Int32);
        private static EnumSerializer stringRepresentation = new EnumSerializer(BsonType.String);
        #endregion

        #region private fields
        private BsonType representation;
        #endregion

        #region constructors
        private EnumSerializer(
            BsonType representation
        ) {
            this.representation = representation;
        }
        #endregion

        #region public static properties
        public static EnumSerializer IntRepresentation {
            get { return intRepresentation; }
        }

        public static EnumSerializer StringRepresentation {
            get { return stringRepresentation; }
        }
        #endregion

        #region public static methods
        public static EnumSerializer GetSerializer(
            object serializationOptions
        ) {
            if (serializationOptions == null) {
                return intRepresentation;
            } else {
                switch ((BsonType) serializationOptions) {
                    case BsonType.Int32: return intRepresentation;
                    case BsonType.String: return stringRepresentation;
                    default: throw new BsonInternalException("Unexpected representation");
                }
            }
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            VerifyNominalType(nominalType);
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.String) {
                var value = bsonReader.ReadString();
                return Enum.Parse(nominalType, value);
            } else {
                switch (Type.GetTypeCode(Enum.GetUnderlyingType(nominalType))) {
                    case TypeCode.Byte: return (byte) bsonReader.ReadInt32();
                    case TypeCode.Int16: return (short) bsonReader.ReadInt32();
                    case TypeCode.Int32: return bsonReader.ReadInt32();
                    case TypeCode.Int64: return bsonReader.ReadInt64();
                    case TypeCode.SByte: return (sbyte) bsonReader.ReadInt32();
                    case TypeCode.UInt16: return (ushort) bsonReader.ReadInt32();
                    case TypeCode.UInt32: return (uint) bsonReader.ReadInt32();
                    case TypeCode.UInt64: return (ulong) bsonReader.ReadInt64();
                    default: throw new BsonSerializationException("Unrecognized underlying type for enum");
                }
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            bool serializeIdFirst
        ) {
            VerifyNominalType(nominalType);
            switch (representation) {
                case BsonType.Int32:
                    switch (Type.GetTypeCode(Enum.GetUnderlyingType(nominalType))) {
                        case TypeCode.Byte: bsonWriter.WriteInt32((int) (byte) value); break;
                        case TypeCode.Int16: bsonWriter.WriteInt32((int) (short) value); break;
                        case TypeCode.Int32: bsonWriter.WriteInt32((int) value); break;
                        case TypeCode.Int64: bsonWriter.WriteInt64((long) value); break;
                        case TypeCode.SByte: bsonWriter.WriteInt32((int) (sbyte) value); break;
                        case TypeCode.UInt16: bsonWriter.WriteInt32((int) (ushort) value); break;
                        case TypeCode.UInt32: bsonWriter.WriteInt32((int) (uint) value); break;
                        case TypeCode.UInt64: bsonWriter.WriteInt64((long) (ulong) value); break;
                        default: throw new BsonSerializationException("Unrecognized underlying type for enum");
                    }
                    break;
                case BsonType.String:
                    bsonWriter.WriteString( value.ToString());
                    break;
                default:
                    throw new BsonInternalException("Unexpected EnumRepresentation");
            }
        }
        #endregion

        #region private methods
        private void VerifyNominalType(
            Type nominalType
        ) {
            if (!nominalType.IsEnum) {
                var message = string.Format("EnumSerializer cannot be used with type: {0}", nominalType.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }
}
