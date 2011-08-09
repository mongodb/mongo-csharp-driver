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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers {
    /// <summary>
    /// Represents a serializer for BsonArrays.
    /// </summary>
    public class BsonArraySerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonArraySerializer instance = new BsonArraySerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonArraySerializer class.
        /// </summary>
        public BsonArraySerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonArraySerializer class.
        /// </summary>
        public static BsonArraySerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonArray));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonArray.ReadFrom(bsonReader);
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var array = (BsonArray) value;
                array.WriteTo(bsonWriter);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonBinaryDatas.
    /// </summary>
    public class BsonBinaryDataSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonBinaryDataSerializer instance = new BsonBinaryDataSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonBinaryDataSerializer class.
        /// </summary>
        public BsonBinaryDataSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonBinaryDataSerializer class.
        /// </summary>
        public static BsonBinaryDataSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonBinaryData));

            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    GuidRepresentation guidRepresentation;
                    bsonReader.ReadBinaryData(out bytes, out subType, out guidRepresentation);
                    return new BsonBinaryData(bytes, subType, guidRepresentation);
                default:
                    var message = string.Format("Cannot deserialize BsonBinaryData from BsonType {0}.", bsonType);
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var binaryData = (BsonBinaryData) value;
                var bytes = binaryData.Bytes;
                var subType = binaryData.SubType;
                var guidRepresentation = binaryData.GuidRepresentation;
                if (subType == BsonBinarySubType.UuidStandard || subType == BsonBinarySubType.UuidLegacy) {
                    var writerGuidRepresentation = bsonWriter.Settings.GuidRepresentation;
                    if (writerGuidRepresentation != GuidRepresentation.Unspecified) {
                        if (guidRepresentation == GuidRepresentation.Unspecified) {
                            var message = string.Format("Cannot serialize BsonBinaryData with GuidRepresentation Unspecified to destination with GuidRepresentation {1}.", writerGuidRepresentation);
                            throw new BsonSerializationException(message);
                        }
                        if (guidRepresentation != writerGuidRepresentation) {
                            var guid = GuidConverter.FromBytes(bytes, guidRepresentation);
                            bytes = GuidConverter.ToBytes(guid, writerGuidRepresentation);
                            subType = (writerGuidRepresentation == GuidRepresentation.Standard) ? BsonBinarySubType.UuidStandard : BsonBinarySubType.UuidLegacy;
                            guidRepresentation = writerGuidRepresentation;
                        }
                    }
                }
                bsonWriter.WriteBinaryData(bytes, subType, guidRepresentation);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonBooleans.
    /// </summary>
    public class BsonBooleanSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonBooleanSerializer instance = new BsonBooleanSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonBooleanSerializer class.
        /// </summary>
        public BsonBooleanSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonBooleanSerializer class.
        /// </summary>
        public static BsonBooleanSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonBoolean));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonBoolean.Create((bool) BooleanSerializer.Instance.Deserialize(bsonReader, typeof(bool), options));
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bsonBoolean = (BsonBoolean) value;
                BooleanSerializer.Instance.Serialize(bsonWriter, nominalType, bsonBoolean.Value, options);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonDateTimes.
    /// </summary>
    public class BsonDateTimeSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonDateTimeSerializer instance = new BsonDateTimeSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonDateTimeSerializer class.
        /// </summary>
        public BsonDateTimeSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonDateTimeSerializer class.
        /// </summary>
        public static BsonDateTimeSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonDateTime));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                var dateTimeOptions = (options == null) ? DateTimeSerializationOptions.Defaults : (DateTimeSerializationOptions) options;
                long? millisecondsSinceEpoch = null;
                long? ticks = null;
 
                switch (bsonType) {
                    case BsonType.DateTime:
                        millisecondsSinceEpoch = bsonReader.ReadDateTime();
                        break;
                    case BsonType.Document:
                        bsonReader.ReadStartDocument();
                        millisecondsSinceEpoch = bsonReader.ReadDateTime("DateTime");
                        bsonReader.ReadName("Ticks");
                        var ticksValue = BsonValue.ReadFrom(bsonReader);
                        if (!ticksValue.IsBsonUndefined) {
                            ticks = ticksValue.ToInt64();
                        }
                        bsonReader.ReadEndDocument();
                        break;
                    case BsonType.Int64:
                        ticks = bsonReader.ReadInt64();
                        break;
                    case BsonType.String:
                        // note: we're not using XmlConvert because of bugs in Mono
                        DateTime dateTime;
                        if (dateTimeOptions.DateOnly) {
                            dateTime = DateTime.SpecifyKind(DateTime.ParseExact(bsonReader.ReadString(), "yyyy-MM-dd", null), DateTimeKind.Utc);
                        } else {
                            var formats = new string[] {
                            "yyyy-MM-ddK",
                            "yyyy-MM-ddTHH:mm:ssK",
                            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
                        };
                            dateTime = DateTime.ParseExact(bsonReader.ReadString(), formats, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                        }
                        ticks = dateTime.Ticks;
                        break;
                    default:
                        var message = string.Format("Cannot deserialize DateTime from BsonType {0}.", bsonType);
                        throw new FileFormatException(message);
                }

                BsonDateTime bsonDateTime;
                if (ticks.HasValue) {
                    bsonDateTime = BsonDateTime.Create(new DateTime(ticks.Value, DateTimeKind.Utc));
                } else {
                    bsonDateTime = BsonDateTime.Create(millisecondsSinceEpoch.Value);
                }

                if (dateTimeOptions.DateOnly) {
                    var dateTime = bsonDateTime.Value;
                    if (dateTime.TimeOfDay != TimeSpan.Zero) {
                        throw new FileFormatException("TimeOfDay component for DateOnly DateTime value is not zero.");
                    }
                    bsonDateTime = BsonDateTime.Create(DateTime.SpecifyKind(dateTime, dateTimeOptions.Kind)); // not ToLocalTime or ToUniversalTime!
                } else {
                    if (bsonDateTime.IsValidDateTime) {
                        var dateTime = bsonDateTime.Value;
                        switch (dateTimeOptions.Kind) {
                            case DateTimeKind.Local:
                            case DateTimeKind.Unspecified:
                                dateTime = BsonUtils.ToLocalTime(dateTime, dateTimeOptions.Kind);
                                break;
                            case DateTimeKind.Utc:
                                dateTime = BsonUtils.ToUniversalTime(dateTime);
                                break;
                        }
                        bsonDateTime = BsonDateTime.Create(dateTime);
                    } else {
                        if (dateTimeOptions.Kind != DateTimeKind.Utc) {
                            throw new FileFormatException("BsonDateTime is outside the range of .NET DateTime.");
                        }
                    }
                }

                return bsonDateTime;
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bsonDateTime = (BsonDateTime) value;
                var dateTimeOptions = (options == null) ? DateTimeSerializationOptions.Defaults : (DateTimeSerializationOptions) options;

                DateTime utcDateTime = DateTime.MinValue;
                long millisecondsSinceEpoch;
                if (dateTimeOptions.DateOnly) {
                    if (bsonDateTime.Value.TimeOfDay != TimeSpan.Zero) {
                        throw new BsonSerializationException("TimeOfDay component is not zero.");
                    }
                    utcDateTime = DateTime.SpecifyKind(bsonDateTime.Value, DateTimeKind.Utc); // not ToLocalTime
                    millisecondsSinceEpoch = BsonUtils.ToMillisecondsSinceEpoch(utcDateTime);
                } else {
                    if (bsonDateTime.IsValidDateTime) {
                        utcDateTime = BsonUtils.ToUniversalTime(bsonDateTime.Value);
                    }
                    millisecondsSinceEpoch = bsonDateTime.MillisecondsSinceEpoch;
                }

                switch (dateTimeOptions.Representation) {
                    case BsonType.DateTime:
                        bsonWriter.WriteDateTime(millisecondsSinceEpoch);
                        break;
                    case BsonType.Document:
                        bsonWriter.WriteStartDocument();
                        bsonWriter.WriteDateTime("DateTime", millisecondsSinceEpoch);
                        if (bsonDateTime.IsValidDateTime) {
                            bsonWriter.WriteInt64("Ticks", utcDateTime.Ticks);
                        } else {
                            bsonWriter.WriteUndefined("Ticks");
                        }
                        bsonWriter.WriteEndDocument();
                        break;
                    case BsonType.Int64:
                        if (bsonDateTime.IsValidDateTime) {
                            bsonWriter.WriteInt64(utcDateTime.Ticks);
                        } else {
                            throw new BsonSerializationException("BsonDateTime is not a valid DateTime.");
                        }
                        break;
                    case BsonType.String:
                        if (dateTimeOptions.DateOnly) {
                            bsonWriter.WriteString(bsonDateTime.Value.ToString("yyyy-MM-dd"));
                        } else {
                            // we're not using XmlConvert.ToString because of bugs in Mono
                            var dateTime = bsonDateTime.Value;
                            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue) {
                                // serialize MinValue and MaxValue as Unspecified so we do NOT get the time zone offset
                                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
                            } else if (dateTime.Kind == DateTimeKind.Unspecified) {
                                // serialize Unspecified as Local se we get the time zone offset
                                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                            }
                            bsonWriter.WriteString(dateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFK"));
                        }
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid representation for type DateTime.", dateTimeOptions.Representation);
                        throw new BsonSerializationException(message);
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonDocuments.
    /// </summary>
    public class BsonDocumentSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonDocumentSerializer instance = new BsonDocumentSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonDocumentSerializer class.
        /// </summary>
        public BsonDocumentSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonDocumentSerializer class.
        /// </summary>
        public static BsonDocumentSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonDocument));

            return BsonDocument.ReadFrom(bsonReader);
        }

        /// <summary>
        /// Gets the document Id.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="id">The Id.</param>
        /// <param name="idNominalType">The nominal type of the Id.</param>
        /// <param name="idGenerator">The IdGenerator for the Id type.</param>
        /// <returns>True if the document has an Id.</returns>
        public override bool GetDocumentId(
            object document,
            out object id,
            out Type idNominalType,
            out IIdGenerator idGenerator
        ) {
            var bsonDocument = (BsonDocument) document;
            return bsonDocument.GetDocumentId(out id, out idNominalType, out idGenerator);
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var document = (BsonDocument) value;
                document.Serialize(bsonWriter, nominalType, options);
            }
        }

        /// <summary>
        /// Sets the document Id.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="id">The Id.</param>
        public override void SetDocumentId(
            object document,
            object id
        ) {
            var bsonDocument = (BsonDocument) document;
            bsonDocument.SetDocumentId(id);
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonDocumentWrappers.
    /// </summary>
    public class BsonDocumentWrapperSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonDocumentWrapperSerializer instance = new BsonDocumentWrapperSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonDocumentWrapperSerializer class.
        /// </summary>
        public BsonDocumentWrapperSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonDocumentWrapperSerializer class.
        /// </summary>
        public static BsonDocumentWrapperSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            throw new NotSupportedException();
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var documentWrapper = (BsonDocumentWrapper) value;
                documentWrapper.Serialize(bsonWriter, nominalType, options);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonDoubles.
    /// </summary>
    public class BsonDoubleSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonDoubleSerializer instance = new BsonDoubleSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonDoubleSerializer class.
        /// </summary>
        public BsonDoubleSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonDoubleSerializer class.
        /// </summary>
        public static BsonDoubleSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonDouble));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonDouble.Create((double) DoubleSerializer.Instance.Deserialize(bsonReader, typeof(double), options));
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bsonDouble = (BsonDouble) value;
                DoubleSerializer.Instance.Serialize(bsonWriter, nominalType, bsonDouble.Value, options);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonInt32s.
    /// </summary>
    public class BsonInt32Serializer : BsonBaseSerializer {
        #region private static fields
        private static BsonInt32Serializer instance = new BsonInt32Serializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonInt32Serializer class.
        /// </summary>
        public BsonInt32Serializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonInt32Serializer class.
        /// </summary>
        public static BsonInt32Serializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonInt32));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonInt32.Create((int) Int32Serializer.Instance.Deserialize(bsonReader, typeof(int), options));
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bsonInt32 = (BsonInt32) value;
                Int32Serializer.Instance.Serialize(bsonWriter, nominalType, bsonInt32.Value, options);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonInt64s.
    /// </summary>
    public class BsonInt64Serializer : BsonBaseSerializer {
        #region private static fields
        private static BsonInt64Serializer instance = new BsonInt64Serializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonInt64Serializer class.
        /// </summary>
        public BsonInt64Serializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonInt64Serializer class.
        /// </summary>
        public static BsonInt64Serializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonInt64));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonInt64.Create((long) Int64Serializer.Instance.Deserialize(bsonReader, typeof(long), options));
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bsonInt64 = (BsonInt64) value;
                Int64Serializer.Instance.Serialize(bsonWriter, nominalType, bsonInt64.Value, options);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonJavaScripts.
    /// </summary>
    public class BsonJavaScriptSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonJavaScriptSerializer instance = new BsonJavaScriptSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonJavaScriptSerializer class.
        /// </summary>
        public BsonJavaScriptSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonJavaScriptSerializer class.
        /// </summary>
        public static BsonJavaScriptSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonJavaScript));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                var code = bsonReader.ReadJavaScript();
                return new BsonJavaScript(code);
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var script = (BsonJavaScript) value;
                bsonWriter.WriteJavaScript(script.Code);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonJavaScriptWithScopes.
    /// </summary>
    public class BsonJavaScriptWithScopeSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonJavaScriptWithScopeSerializer instance = new BsonJavaScriptWithScopeSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonJavaScriptWithScopeSerializer class.
        /// </summary>
        public BsonJavaScriptWithScopeSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonJavaScriptWithScopeSerializer class.
        /// </summary>
        public static BsonJavaScriptWithScopeSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonJavaScriptWithScope));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                var code = bsonReader.ReadJavaScriptWithScope();
                var scope = BsonDocument.ReadFrom(bsonReader);
                return new BsonJavaScriptWithScope(code, scope);
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var script = (BsonJavaScriptWithScope) value;
                bsonWriter.WriteJavaScriptWithScope(script.Code);
                script.Scope.WriteTo(bsonWriter);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonMaxKeys.
    /// </summary>
    public class BsonMaxKeySerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonMaxKeySerializer instance = new BsonMaxKeySerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonMaxKeySerializer class.
        /// </summary>
        public BsonMaxKeySerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonMaxKeySerializer class.
        /// </summary>
        public static BsonMaxKeySerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonMaxKey));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadMaxKey();
                return BsonMaxKey.Value;
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                bsonWriter.WriteMaxKey();
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonMinKeys.
    /// </summary>
    public class BsonMinKeySerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonMinKeySerializer instance = new BsonMinKeySerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonMinKeySerializer class.
        /// </summary>
        public BsonMinKeySerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonMinKeySerializer class.
        /// </summary>
        public static BsonMinKeySerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonMinKey));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadMinKey();
                return BsonMinKey.Value;
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                bsonWriter.WriteMinKey();
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonNulls.
    /// </summary>
    public class BsonNullSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonNullSerializer instance = new BsonNullSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonNullSerializer class.
        /// </summary>
        public BsonNullSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonNullSerializer class.
        /// </summary>
        public static BsonNullSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonNull));

            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return BsonNull.Value;
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    var csharpNull = bsonReader.ReadBoolean("$csharpnull");
                    bsonReader.ReadEndDocument();
                    return csharpNull ? null : BsonNull.Value;
                default:
                    var message = string.Format("Cannot deserialize BsonNull from BsonType {0}.", bsonType);
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
            if (value == null) {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteBoolean("$csharpnull", true);
                bsonWriter.WriteEndDocument();
            } else {
                bsonWriter.WriteNull();
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonObjectIds.
    /// </summary>
    public class BsonObjectIdSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonObjectIdSerializer instance = new BsonObjectIdSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonObjectIdSerializer class.
        /// </summary>
        public BsonObjectIdSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonObjectIdSerializer class.
        /// </summary>
        public static BsonObjectIdSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonObjectId));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonObjectId.Create((ObjectId) ObjectIdSerializer.Instance.Deserialize(bsonReader, typeof(ObjectId), options));
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bsonObjectId = (BsonObjectId) value;
                ObjectIdSerializer.Instance.Serialize(bsonWriter, nominalType, bsonObjectId.Value, options);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonRegularExpressions.
    /// </summary>
    public class BsonRegularExpressionSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonRegularExpressionSerializer instance = new BsonRegularExpressionSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonRegularExpressionSerializer class.
        /// </summary>
        public BsonRegularExpressionSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonRegularExpressionSerializer class.
        /// </summary>
        public static BsonRegularExpressionSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonRegularExpression));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                string regexPattern, regexOptions;
                bsonReader.ReadRegularExpression(out regexPattern, out regexOptions);
                return new BsonRegularExpression(regexPattern, regexOptions);
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var regex = (BsonRegularExpression) value;
                bsonWriter.WriteRegularExpression(regex.Pattern, regex.Options);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonStrings.
    /// </summary>
    public class BsonStringSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonStringSerializer instance = new BsonStringSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonStringSerializer class.
        /// </summary>
        public BsonStringSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonStringSerializer class.
        /// </summary>
        public static BsonStringSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonString));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonString.Create((string) StringSerializer.Instance.Deserialize(bsonReader, typeof(string), options));
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bsonString = (BsonString) value;
                StringSerializer.Instance.Serialize(bsonWriter, nominalType, bsonString.Value, options);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonSymbols.
    /// </summary>
    public class BsonSymbolSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonSymbolSerializer instance = new BsonSymbolSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonSymbolSerializer class.
        /// </summary>
        public BsonSymbolSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonSymbolSerializer class.
        /// </summary>
        public static BsonSymbolSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonSymbol));

            var bsonType = bsonReader.CurrentBsonType;
            switch (bsonType) {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.String:
                    return BsonSymbol.Create(bsonReader.ReadString());
                case BsonType.Symbol:
                    return BsonSymbol.Create(bsonReader.ReadSymbol());
                default:
                    var message = string.Format("Cannot deserialize BsonSymbol from BsonType {0}.", bsonType);
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var symbol = (BsonSymbol) value;
                var representation = (options == null) ? BsonType.Symbol : ((RepresentationSerializationOptions) options).Representation;
                switch (representation) {
                    case BsonType.String:
                        bsonWriter.WriteString(symbol.Name);
                        break;
                    case BsonType.Symbol:
                        bsonWriter.WriteSymbol(symbol.Name);
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid BsonSymbol value.", representation);
                        throw new BsonSerializationException(message);
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonTimestamps.
    /// </summary>
    public class BsonTimestampSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonTimestampSerializer instance = new BsonTimestampSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonTimestampSerializer class.
        /// </summary>
        public BsonTimestampSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonTimestampSerializer class.
        /// </summary>
        public static BsonTimestampSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonTimestamp));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonTimestamp.Create(bsonReader.ReadTimestamp());
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var timestamp = (BsonTimestamp) value;
                bsonWriter.WriteTimestamp(timestamp.Value);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonUndefineds.
    /// </summary>
    public class BsonUndefinedSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonUndefinedSerializer instance = new BsonUndefinedSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonUndefinedSerializer class.
        /// </summary>
        public BsonUndefinedSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonUndefinedSerializer class.
        /// </summary>
        public static BsonUndefinedSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            VerifyTypes(nominalType, actualType, typeof(BsonUndefined));

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadUndefined();
                return BsonUndefined.Value;
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                bsonWriter.WriteUndefined();
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for BsonValues.
    /// </summary>
    public class BsonValueSerializer : BsonBaseSerializer {
        #region private static fields
        private static BsonValueSerializer instance = new BsonValueSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonValueSerializer class.
        /// </summary>
        public BsonValueSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the BsonValueSerializer class.
        /// </summary>
        public static BsonValueSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType, // ignored
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                return BsonValue.ReadFrom(bsonReader);
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
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var bsonValue = (BsonValue) value;
                bsonValue.WriteTo(bsonWriter);
            }
        }
        #endregion
    }
}
