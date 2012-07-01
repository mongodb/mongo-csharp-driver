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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.IO;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a document from the system.profile collection.
    /// </summary>
    [Serializable]
    public class SystemProfileInfo
    {
        // private fields
        private string _abbreviated;
        private string _client;
        private BsonDocument _command;
        private long _cursorId;
        private TimeSpan _duration;
        private string _error;
        private string _exception;
        private int _exceptionCode;
        private bool _exhaust;
        private bool _fastMod;
        private bool _fastModInsert;
        private bool _idHack;
        private string _info;
        private int _keyUpdates;
        private bool _moved;
        private string _namespace;
        private int _numberReturned;
        private int _numberScanned;
        private int _numberToReturn;
        private int _numberToSkip;
        private string _op;
        private BsonDocument _query;
        private int _responseLength;
        private bool _scanAndOrder;
        private DateTime _timestamp;
        private BsonDocument _updateObject;
        private bool _upsert;
        private string _user;

        // constructors
        /// <summary>
        /// Initializes a new instance of the SystemProfileInfo class.
        /// </summary>
        public SystemProfileInfo()
        {
        }

        // public properties
        /// <summary>
        /// Gets or sets the abbreviated profile info (only used when the profile info would have exceeded 100KB).
        /// </summary>
        public string Abbreviated
        {
            get { return _abbreviated; }
            set { _abbreviated = value; }
        }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        public string Client
        {
            get { return _client; }
            set { _client = value; }
        }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        public BsonDocument Command
        {
            get { return _command; }
            set { _command = value; }
        }

        /// <summary>
        /// Gets or sets the cursor Id.
        /// </summary>
        public long CursorId
        {
            get { return _cursorId; }
            set { _cursorId = value; }
        }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        public TimeSpan Duration
        {
            get { return _duration; }
            set { _duration = value; }
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        /// <summary>
        /// Gets or sets the exception message.
        /// </summary>
        public string Exception
        {
            get { return _exception; }
            set { _exception = value; }
        }

        /// <summary>
        /// Gets or sets the exception code.
        /// </summary>
        public int ExceptionCode
        {
            get { return _exceptionCode; }
            set { _exceptionCode = value; }
        }

        /// <summary>
        /// Gets or sets whether exhaust was true.
        /// </summary>
        public bool Exhaust
        {
            get { return _exhaust; }
            set { _exhaust = value; }
        }

        /// <summary>
        /// Gets or sets whether fastMod was true.
        /// </summary>
        public bool FastMod
        {
            get { return _fastMod; }
            set { _fastMod = value; }
        }

        /// <summary>
        /// Gets or sets whether fastModInsert was true.
        /// </summary>
        public bool FastModInsert
        {
            get { return _fastModInsert; }
            set { _fastModInsert = value; }
        }

        /// <summary>
        /// Gets or sets whether idHack was true.
        /// </summary>
        public bool IdHack
        {
            get { return _idHack; }
            set { _idHack = value; }
        }

        /// <summary>
        /// Gets or sets the info string (only present with pre 2.0 servers).
        /// </summary>
        public string Info
        {
            get { return _info; }
            set { _info = value; }
        }

        /// <summary>
        /// Gets or sets the number of key updates.
        /// </summary>
        public int KeyUpdates
        {
            get { return _keyUpdates; }
            set { _keyUpdates = value; }
        }

        /// <summary>
        /// Gets or sets whether moved was true.
        /// </summary>
        public bool Moved
        {
            get { return _moved; }
            set { _moved = value; }
        }

        /// <summary>
        /// Gets or sets the namespace.
        /// </summary>
        public string Namespace
        {
            get { return _namespace; }
            set { _namespace = value; }
        }

        /// <summary>
        /// Gets or sets the number of documents returned.
        /// </summary>
        public int NumberReturned
        {
            get { return _numberReturned; }
            set { _numberReturned = value; }
        }

        /// <summary>
        /// Gets or sets the number of documents scanned.
        /// </summary>
        public int NumberScanned
        {
            get { return _numberScanned; }
            set { _numberScanned = value; }
        }

        /// <summary>
        /// Gets or sets the number of documents to return.
        /// </summary>
        public int NumberToReturn
        {
            get { return _numberToReturn; }
            set { _numberToReturn = value; }
        }

        /// <summary>
        /// Gets or sets the number of documents to skip.
        /// </summary>
        public int NumberToSkip
        {
            get { return _numberToSkip; }
            set { _numberToSkip = value; }
        }

        /// <summary>
        /// Gets or sets the operation.
        /// </summary>
        public string Op
        {
            get { return _op; }
            set { _op = value; }
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        public BsonDocument Query
        {
            get { return _query; }
            set { _query = value; }
        }

        /// <summary>
        /// Gets or sets the response length.
        /// </summary>
        public int ResponseLength
        {
            get { return _responseLength; }
            set { _responseLength = value; }
        }

        /// <summary>
        /// Gets or sets whether scanAndOrder was true.
        /// </summary>
        public bool ScanAndOrder
        {
            get { return _scanAndOrder; }
            set { _scanAndOrder = value; }
        }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        /// <summary>
        /// Gets or sets the update object.
        /// </summary>
        public BsonDocument UpdateObject
        {
            get { return _updateObject; }
            set { _updateObject = value; }
        }

        /// <summary>
        /// Gets or sets whether upsert was true.
        /// </summary>
        public bool Upsert
        {
            get { return _upsert; }
            set { _upsert = value; }
        }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        public string User
        {
            get { return _user; }
            set { _user = value; }
        }
    }

    /// <summary>
    /// Represents a serializer for SystemProfileInfo.
    /// </summary>
    public class SystemProfileInfoSerializer : BsonBaseSerializer, IBsonDocumentSerializer
    {
        // public methods
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
            IBsonSerializationOptions options)
        {
            VerifyTypes(nominalType, actualType, typeof(SystemProfileInfo));

            if (bsonReader.GetCurrentBsonType() == Bson.BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                var profileInfo = new SystemProfileInfo();

                bsonReader.ReadStartDocument();
                BsonType bsonType;
                while ((bsonType = bsonReader.ReadBsonType()) != BsonType.EndOfDocument)
                {
                    var name = bsonReader.ReadName();
                    switch (name)
                    {
                        case "abbreviated":
                            profileInfo.Abbreviated = bsonReader.ReadString();
                            break;
                        case "client":
                            profileInfo.Client = bsonReader.ReadString();
                            break;
                        case "command":
                            profileInfo.Command = BsonDocument.ReadFrom(bsonReader);
                            break;
                        case "cursorid":
                            profileInfo.CursorId = BsonValue.ReadFrom(bsonReader).ToInt64();
                            break;
                        case "err":
                            profileInfo.Error = bsonReader.ReadString();
                            break;
                        case "exception":
                            profileInfo.Exception = bsonReader.ReadString();
                            break;
                        case "exceptionCode":
                            profileInfo.ExceptionCode = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "exhaust":
                            profileInfo.Exhaust = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "fastmod":
                            profileInfo.FastMod = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "fastmodinsert":
                            profileInfo.FastModInsert = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "idhack":
                            profileInfo.IdHack = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "info":
                            profileInfo.Info = bsonReader.ReadString();
                            break;
                        case "keyUpdates":
                            profileInfo.KeyUpdates = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "millis":
                            profileInfo.Duration = TimeSpan.FromMilliseconds(BsonValue.ReadFrom(bsonReader).ToDouble());
                            break;
                        case "moved":
                            profileInfo.Moved = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "nreturned":
                            profileInfo.NumberReturned = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "ns":
                            profileInfo.Namespace = bsonReader.ReadString();
                            break;
                        case "nscanned":
                            profileInfo.NumberScanned = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "ntoreturn":
                            profileInfo.NumberToReturn = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "ntoskip":
                            profileInfo.NumberToSkip = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "op":
                            profileInfo.Op = bsonReader.ReadString();
                            break;
                        case "query":
                            profileInfo.Query = BsonDocument.ReadFrom(bsonReader);
                            break;
                        case "responseLength":
                            profileInfo.ResponseLength = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "scanAndOrder":
                            profileInfo.ScanAndOrder = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "ts":
                            profileInfo.Timestamp = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(bsonReader.ReadDateTime());
                            break;
                        case "updateobj":
                            profileInfo.UpdateObject = BsonDocument.ReadFrom(bsonReader);
                            break;
                        case "upsert":
                            profileInfo.Upsert = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "user":
                            profileInfo.User = bsonReader.ReadString();
                            break;
                        default:
                            bsonReader.SkipValue(); // ignore unknown elements
                            break;
                    }
                }
                bsonReader.ReadEndDocument();

                return profileInfo;
            }
        }

        /// <summary>
        /// Gets the serialization info for a member.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <returns>The serialization info for the member.</returns>
        public BsonSerializationInfo GetMemberSerializationInfo(string memberName)
        {
            string elementName;
            IBsonSerializer serializer;
            Type nominalType;
            IBsonSerializationOptions serializationOptions = null;

            switch (memberName)
            {
                case "Abbreviated":
                    elementName = "abbreviated";
                    serializer = StringSerializer.Instance;
                    nominalType = typeof(string);
                    break;
                case "Client":
                    elementName = "client";
                    serializer = StringSerializer.Instance;
                    nominalType = typeof(string);
                    break;
                case "Command":
                    elementName = "command";
                    serializer = BsonDocumentSerializer.Instance;
                    nominalType = typeof(BsonDocument);
                    break;
                case "CursorId":
                    elementName = "cursorid";
                    serializer = Int64Serializer.Instance;
                    nominalType = typeof(long);
                    break;
                case "Duration":
                    elementName = "millis";
                    serializer = TimeSpanSerializer.Instance;
                    nominalType = typeof(TimeSpan);
                    serializationOptions = new TimeSpanSerializationOptions(BsonType.Double, TimeSpanUnits.Milliseconds);
                    break;
                case "Error":
                    elementName = "err";
                    serializer = StringSerializer.Instance;
                    nominalType = typeof(string);
                    break;
                case "Exception":
                    elementName = "exception";
                    serializer = StringSerializer.Instance;
                    nominalType = typeof(string);
                    break;
                case "ExceptionCode":
                    elementName = "exceptionCode";
                    serializer = Int32Serializer.Instance;
                    nominalType = typeof(int);
                    break;
                case "Exhaust":
                    elementName = "exhaust";
                    serializer = BooleanSerializer.Instance;
                    nominalType = typeof(bool);
                    break;
                case "FastMod":
                    elementName = "fastmod";
                    serializer = BooleanSerializer.Instance;
                    nominalType = typeof(bool);
                    break;
                case "FastModInsert":
                    elementName = "fastmodinsert";
                    serializer = BooleanSerializer.Instance;
                    nominalType = typeof(bool);
                    break;
                case "IdHack":
                    elementName = "idhack";
                    serializer = BooleanSerializer.Instance;
                    nominalType = typeof(bool);
                    break;
                case "Info":
                    elementName = "info";
                    serializer = StringSerializer.Instance;
                    nominalType = typeof(string);
                    break;
                case "KeyUpdates":
                    elementName = "keyUpdates";
                    serializer = Int32Serializer.Instance;
                    nominalType = typeof(int);
                    break;
                case "Moved":
                    elementName = "moved";
                    serializer = BooleanSerializer.Instance;
                    nominalType = typeof(bool);
                    break;
                case "Namespace":
                    elementName = "ns";
                    serializer = StringSerializer.Instance;
                    nominalType = typeof(string);
                    break;
                case "NumberReturned":
                    elementName = "nreturned";
                    serializer = Int32Serializer.Instance;
                    nominalType = typeof(int);
                    break;
                case "NumberScanned":
                    elementName = "nscanned";
                    serializer = Int32Serializer.Instance;
                    nominalType = typeof(int);
                    break;
                case "NumberToReturn":
                    elementName = "ntoreturn";
                    serializer = Int32Serializer.Instance;
                    nominalType = typeof(int);
                    break;
                case "NumberToSkip":
                    elementName = "ntoskip";
                    serializer = Int32Serializer.Instance;
                    nominalType = typeof(int);
                    break;
                case "Op":
                    elementName = "op";
                    serializer = StringSerializer.Instance;
                    nominalType = typeof(string);
                    break;
                case "Query":
                    elementName = "query";
                    serializer = BsonDocumentSerializer.Instance;
                    nominalType = typeof(BsonDocument);
                    break;
                case "ResponseLength":
                    elementName = "responseLength";
                    serializer = Int32Serializer.Instance;
                    nominalType = typeof(int);
                    break;
                case "ScanAndOrder":
                    elementName = "scanAndOrder";
                    serializer = BooleanSerializer.Instance;
                    nominalType = typeof(bool);
                    break;
                case "Timestamp":
                    elementName = "ts";
                    serializer = DateTimeSerializer.Instance;
                    nominalType = typeof(DateTime);
                    break;
                case "UpdateObject":
                    elementName = "updateobj";
                    serializer = BsonDocumentSerializer.Instance;
                    nominalType = typeof(BsonDocument);
                    break;
                case "Upsert":
                    elementName = "upsert";
                    serializer = BooleanSerializer.Instance;
                    nominalType = typeof(bool);
                    break;
                case "User":
                    elementName = "user";
                    serializer = StringSerializer.Instance;
                    nominalType = typeof(string);
                    break;
                default:
                    var message = string.Format("{0} is not a member of SystemProfileInfo.", memberName);
                    throw new ArgumentOutOfRangeException("memberName", message);
            }

            return new BsonSerializationInfo(elementName, serializer, nominalType, serializationOptions);
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
            IBsonSerializationOptions options)
        {
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var profileInfo = (SystemProfileInfo)value;

                bsonWriter.WriteStartDocument();
                bsonWriter.WriteDateTime("ts", BsonUtils.ToMillisecondsSinceEpoch(profileInfo.Timestamp));
                if (profileInfo.Info != null)
                {
                    bsonWriter.WriteString("info", profileInfo.Info);
                }
                if (profileInfo.Op != null)
                {
                    bsonWriter.WriteString("op", profileInfo.Op);
                }
                if (profileInfo.Namespace != null)
                {
                    bsonWriter.WriteString("ns", profileInfo.Namespace);
                }
                if (profileInfo.Command != null)
                {
                    bsonWriter.WriteName("command");
                    profileInfo.Command.WriteTo(bsonWriter);
                }
                if (profileInfo.Query != null)
                {
                    bsonWriter.WriteName("query");
                    profileInfo.Query.WriteTo(bsonWriter);
                }
                if (profileInfo.UpdateObject != null)
                {
                    bsonWriter.WriteName("updateobj");
                    profileInfo.UpdateObject.WriteTo(bsonWriter);
                }
                if (profileInfo.CursorId != 0)
                {
                    bsonWriter.WriteInt64("cursorid", profileInfo.CursorId);
                }
                if (profileInfo.NumberToReturn != 0)
                {
                    bsonWriter.WriteInt32("ntoreturn", profileInfo.NumberToReturn);
                }
                if (profileInfo.NumberToSkip != 0)
                {
                    bsonWriter.WriteInt32("ntoskip", profileInfo.NumberToSkip);
                }
                if (profileInfo.Exhaust)
                {
                    bsonWriter.WriteBoolean("exhaust", profileInfo.Exhaust);
                }
                if (profileInfo.NumberScanned != 0)
                {
                    bsonWriter.WriteInt32("nscanned", profileInfo.NumberScanned);
                }
                if (profileInfo.IdHack)
                {
                    bsonWriter.WriteBoolean("idhack", profileInfo.IdHack);
                }
                if (profileInfo.ScanAndOrder)
                {
                    bsonWriter.WriteBoolean("scanAndOrder", profileInfo.ScanAndOrder);
                }
                if (profileInfo.Moved)
                {
                    bsonWriter.WriteBoolean("moved", profileInfo.Moved);
                }
                if (profileInfo.FastMod)
                {
                    bsonWriter.WriteBoolean("fastmod", profileInfo.FastMod);
                }
                if (profileInfo.FastModInsert)
                {
                    bsonWriter.WriteBoolean("fastmodinsert", profileInfo.FastModInsert);
                }
                if (profileInfo.Upsert)
                {
                    bsonWriter.WriteBoolean("upsert", profileInfo.Upsert);
                }
                if (profileInfo.KeyUpdates != 0)
                {
                    bsonWriter.WriteInt32("keyUpdates", profileInfo.KeyUpdates);
                }
                if (profileInfo.Exception != null)
                {
                    bsonWriter.WriteString("exception", profileInfo.Exception);
                }
                if (profileInfo.ExceptionCode != 0)
                {
                    bsonWriter.WriteInt32("exceptionCode", profileInfo.ExceptionCode);
                }
                if (profileInfo.NumberReturned != 0)
                {
                    bsonWriter.WriteInt32("nreturned", profileInfo.NumberReturned);
                }
                if (profileInfo.ResponseLength != 0)
                {
                    bsonWriter.WriteInt32("responseLength", profileInfo.ResponseLength);
                }
                bsonWriter.WriteDouble("millis", profileInfo.Duration.TotalMilliseconds);
                if (profileInfo.Client != null)
                {
                    bsonWriter.WriteString("client", profileInfo.Client);
                }
                if (profileInfo.User != null)
                {
                    bsonWriter.WriteString("user", profileInfo.User);
                }
                if (profileInfo.Error != null)
                {
                    bsonWriter.WriteString("err", profileInfo.Error);
                }
                if (profileInfo.Abbreviated != null)
                {
                    bsonWriter.WriteString("abbreviated", profileInfo.Abbreviated);
                }
                bsonWriter.WriteEndDocument();
            }
        }
    }
}
