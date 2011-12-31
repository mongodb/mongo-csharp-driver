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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.IO;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a document from the system.profile collection.
    /// </summary>
    [Serializable]
    public class SystemProfileInfo : IBsonSerializable
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

        // explicit interface implementation
        object IBsonSerializable.Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            if (bsonReader.CurrentBsonType == Bson.BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                bsonReader.ReadStartDocument();
                BsonType bsonType;
                while ((bsonType = bsonReader.ReadBsonType()) != BsonType.EndOfDocument)
                {
                    var name = bsonReader.ReadName();
                    switch (name)
                    {
                        case "abbreviated":
                            _abbreviated = bsonReader.ReadString();
                            break;
                        case "client":
                            _client = bsonReader.ReadString();
                            break;
                        case "command":
                            _command = BsonDocument.ReadFrom(bsonReader);
                            break;
                        case "cursorid":
                            _cursorId = BsonValue.ReadFrom(bsonReader).ToInt64();
                            break;
                        case "err":
                            _error = bsonReader.ReadString();
                            break;
                        case "exception":
                            _exception = bsonReader.ReadString();
                            break;
                        case "exceptionCode":
                            _exceptionCode = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "exhaust":
                            _exhaust = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "fastmod":
                            _fastMod = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "fastmodinsert":
                            _fastModInsert = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "idhack":
                            _idHack = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "info":
                            _info = bsonReader.ReadString();
                            break;
                        case "keyUpdates":
                            _keyUpdates = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "millis":
                            _duration = TimeSpan.FromMilliseconds(BsonValue.ReadFrom(bsonReader).ToDouble());
                            break;
                        case "moved":
                            _moved = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "nreturned":
                            _numberReturned = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "ns":
                            _namespace = bsonReader.ReadString();
                            break;
                        case "nscanned":
                            _numberScanned = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "ntoreturn":
                            _numberToReturn = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "ntoskip":
                            _numberToSkip = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "op":
                            _op = bsonReader.ReadString();
                            break;
                        case "query":
                            _query = BsonDocument.ReadFrom(bsonReader);
                            break;
                        case "responseLength":
                            _responseLength = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "scanAndOrder":
                            _scanAndOrder = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "ts":
                            _timestamp = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(bsonReader.ReadDateTime());
                            break;
                        case "updateobj":
                            _updateObject = BsonDocument.ReadFrom(bsonReader);
                            break;
                        case "upsert":
                            _upsert = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "user":
                            _user = bsonReader.ReadString();
                            break;
                        default:
                            break; // ignore unknown elements
                    }
                }
                bsonReader.ReadEndDocument();
                return this;
            }
        }

        bool IBsonSerializable.GetDocumentId(out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            throw new NotSupportedException();
        }

        void IBsonSerializable.Serialize(BsonWriter bsonWriter, Type nominalType, IBsonSerializationOptions options)
        {
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteDateTime("ts", BsonUtils.ToMillisecondsSinceEpoch(_timestamp));
            if (_info != null)
            {
                bsonWriter.WriteString("info", _info);
            }
            if (_op != null)
            {
                bsonWriter.WriteString("op", _op);
            }
            if (_namespace != null)
            {
                bsonWriter.WriteString("ns", _namespace);
            }
            if (_command != null)
            {
                bsonWriter.WriteName("command");
                _command.WriteTo(bsonWriter);
            }
            if (_query != null)
            {
                bsonWriter.WriteName("query");
                _query.WriteTo(bsonWriter);
            }
            if (_updateObject != null)
            {
                bsonWriter.WriteName("updateobj");
                _updateObject.WriteTo(bsonWriter);
            }
            if (_cursorId != 0)
            {
                bsonWriter.WriteInt64("cursorid", _cursorId);
            }
            if (_numberToReturn != 0)
            {
                bsonWriter.WriteInt32("ntoreturn", _numberToReturn);
            }
            if (_numberToSkip != 0)
            {
                bsonWriter.WriteInt32("ntoskip", _numberToSkip);
            }
            if (_exhaust)
            {
                bsonWriter.WriteBoolean("exhaust", _exhaust);
            }
            if (_numberScanned != 0)
            {
                bsonWriter.WriteInt32("nscanned", _numberScanned);
            }
            if (_idHack)
            {
                bsonWriter.WriteBoolean("idhack", _idHack);
            }
            if (_scanAndOrder)
            {
                bsonWriter.WriteBoolean("scanAndOrder", _scanAndOrder);
            }
            if (_moved)
            {
                bsonWriter.WriteBoolean("moved", _moved);
            }
            if (_fastMod)
            {
                bsonWriter.WriteBoolean("fastmod", _fastMod);
            }
            if (_fastModInsert)
            {
                bsonWriter.WriteBoolean("fastmodinsert", _fastModInsert);
            }
            if (_upsert)
            {
                bsonWriter.WriteBoolean("upsert", _upsert);
            }
            if (_keyUpdates != 0)
            {
                bsonWriter.WriteInt32("keyUpdates", _keyUpdates);
            }
            if (_exception != null)
            {
                bsonWriter.WriteString("exception", _exception);
            }
            if (_exceptionCode != 0)
            {
                bsonWriter.WriteInt32("exceptionCode", _exceptionCode);
            }
            if (_numberReturned != 0)
            {
                bsonWriter.WriteInt32("nreturned", _numberReturned);
            }
            if (_responseLength != 0)
            {
                bsonWriter.WriteInt32("responseLength", _responseLength);
            }
            bsonWriter.WriteDouble("millis", _duration.TotalMilliseconds);
            if (_client != null)
            {
                bsonWriter.WriteString("client", _client);
            }
            if (_user != null)
            {
                bsonWriter.WriteString("user", _user);
            }
            if (_error != null)
            {
                bsonWriter.WriteString("err", _error);
            }
            if (_abbreviated != null)
            {
                bsonWriter.WriteString("abbreviated", _abbreviated);
            }
            bsonWriter.WriteEndDocument();
        }

        void IBsonSerializable.SetDocumentId(object id)
        {
            throw new NotSupportedException();
        }
    }
}
