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
        private string abbreviated;
        private string client;
        private BsonDocument command;
        private long cursorId;
        private TimeSpan duration;
        private string error;
        private string exception;
        private int exceptionCode;
        private bool exhaust;
        private bool fastMod;
        private bool fastModInsert;
        private bool idHack;
        private string info;
        private int keyUpdates;
        private bool moved;
        private string @namespace;
        private int numberReturned;
        private int numberScanned;
        private int numberToReturn;
        private int numberToSkip;
        private string op;
        private BsonDocument query;
        private int responseLength;
        private bool scanAndOrder;
        private DateTime timestamp;
        private BsonDocument updateObject;
        private bool upsert;
        private string user;

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
            get { return abbreviated; }
            set { abbreviated = value; }
        }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        public string Client
        {
            get { return client; }
            set { client = value; }
        }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        public BsonDocument Command
        {
            get { return command; }
            set { command = value; }
        }

        /// <summary>
        /// Gets or sets the cursor Id.
        /// </summary>
        public long CursorId
        {
            get { return cursorId; }
            set { cursorId = value; }
        }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        public TimeSpan Duration
        {
            get { return duration; }
            set { duration = value; }
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Error
        {
            get { return error; }
            set { error = value; }
        }

        /// <summary>
        /// Gets or sets the exception message.
        /// </summary>
        public string Exception
        {
            get { return exception; }
            set { exception = value; }
        }

        /// <summary>
        /// Gets or sets the exception code.
        /// </summary>
        public int ExceptionCode
        {
            get { return exceptionCode; }
            set { exceptionCode = value; }
        }

        /// <summary>
        /// Gets or sets whether exhaust was true.
        /// </summary>
        public bool Exhaust
        {
            get { return exhaust; }
            set { exhaust = value; }
        }

        /// <summary>
        /// Gets or sets whether fastMod was true.
        /// </summary>
        public bool FastMod
        {
            get { return fastMod; }
            set { fastMod = value; }
        }

        /// <summary>
        /// Gets or sets whether fastModInsert was true.
        /// </summary>
        public bool FastModInsert
        {
            get { return fastModInsert; }
            set { fastModInsert = value; }
        }

        /// <summary>
        /// Gets or sets whether idHack was true.
        /// </summary>
        public bool IdHack
        {
            get { return idHack; }
            set { idHack = value; }
        }

        /// <summary>
        /// Gets or sets the info string (only present with pre 2.0 servers).
        /// </summary>
        public string Info
        {
            get { return info; }
            set { info = value; }
        }

        /// <summary>
        /// Gets or sets the number of key updates.
        /// </summary>
        public int KeyUpdates
        {
            get { return keyUpdates; }
            set { keyUpdates = value; }
        }

        /// <summary>
        /// Gets or sets whether moved was true.
        /// </summary>
        public bool Moved
        {
            get { return moved; }
            set { moved = value; }
        }

        /// <summary>
        /// Gets or sets the namespace.
        /// </summary>
        public string Namespace
        {
            get { return @namespace; }
            set { @namespace = value; }
        }

        /// <summary>
        /// Gets or sets the number of documents returned.
        /// </summary>
        public int NumberReturned
        {
            get { return numberReturned; }
            set { numberReturned = value; }
        }

        /// <summary>
        /// Gets or sets the number of documents scanned.
        /// </summary>
        public int NumberScanned
        {
            get { return numberScanned; }
            set { numberScanned = value; }
        }

        /// <summary>
        /// Gets or sets the number of documents to return.
        /// </summary>
        public int NumberToReturn
        {
            get { return numberToReturn; }
            set { numberToReturn = value; }
        }

        /// <summary>
        /// Gets or sets the number of documents to skip.
        /// </summary>
        public int NumberToSkip
        {
            get { return numberToSkip; }
            set { numberToSkip = value; }
        }

        /// <summary>
        /// Gets or sets the operation.
        /// </summary>
        public string Op
        {
            get { return op; }
            set { op = value; }
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        public BsonDocument Query
        {
            get { return query; }
            set { query = value; }
        }

        /// <summary>
        /// Gets or sets the response length.
        /// </summary>
        public int ResponseLength
        {
            get { return responseLength; }
            set { responseLength = value; }
        }

        /// <summary>
        /// Gets or sets whether scanAndOrder was true.
        /// </summary>
        public bool ScanAndOrder
        {
            get { return scanAndOrder; }
            set { scanAndOrder = value; }
        }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp
        {
            get { return timestamp; }
            set { timestamp = value; }
        }

        /// <summary>
        /// Gets or sets the update object.
        /// </summary>
        public BsonDocument UpdateObject
        {
            get { return updateObject; }
            set { updateObject = value; }
        }

        /// <summary>
        /// Gets or sets whether upsert was true.
        /// </summary>
        public bool Upsert
        {
            get { return upsert; }
            set { upsert = value; }
        }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        public string User
        {
            get { return user; }
            set { user = value; }
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
                            abbreviated = bsonReader.ReadString();
                            break;
                        case "client":
                            client = bsonReader.ReadString();
                            break;
                        case "command":
                            command = BsonDocument.ReadFrom(bsonReader);
                            break;
                        case "cursorid":
                            cursorId = BsonValue.ReadFrom(bsonReader).ToInt64();
                            break;
                        case "err":
                            error = bsonReader.ReadString();
                            break;
                        case "exception":
                            exception = bsonReader.ReadString();
                            break;
                        case "exceptionCode":
                            exceptionCode = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "exhaust":
                            exhaust = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "fastmod":
                            fastMod = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "fastmodinsert":
                            fastModInsert = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "idhack":
                            idHack = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "info":
                            info = bsonReader.ReadString();
                            break;
                        case "keyUpdates":
                            keyUpdates = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "millis":
                            duration = TimeSpan.FromMilliseconds(BsonValue.ReadFrom(bsonReader).ToDouble());
                            break;
                        case "moved":
                            moved = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "nreturned":
                            numberReturned = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "ns":
                            @namespace = bsonReader.ReadString();
                            break;
                        case "nscanned":
                            numberScanned = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "ntoreturn":
                            numberToReturn = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "ntoskip":
                            numberToSkip = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "op":
                            op = bsonReader.ReadString();
                            break;
                        case "query":
                            query = BsonDocument.ReadFrom(bsonReader);
                            break;
                        case "responseLength":
                            responseLength = BsonValue.ReadFrom(bsonReader).ToInt32();
                            break;
                        case "scanAndOrder":
                            scanAndOrder = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "ts":
                            timestamp = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(bsonReader.ReadDateTime());
                            break;
                        case "updateobj":
                            updateObject = BsonDocument.ReadFrom(bsonReader);
                            break;
                        case "upsert":
                            upsert = BsonValue.ReadFrom(bsonReader).ToBoolean();
                            break;
                        case "user":
                            user = bsonReader.ReadString();
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
            bsonWriter.WriteDateTime("ts", BsonUtils.ToMillisecondsSinceEpoch(timestamp));
            if (info != null)
            {
                bsonWriter.WriteString("info", info);
            }
            if (op != null)
            {
                bsonWriter.WriteString("op", op);
            }
            if (@namespace != null)
            {
                bsonWriter.WriteString("ns", @namespace);
            }
            if (command != null)
            {
                bsonWriter.WriteName("command");
                command.WriteTo(bsonWriter);
            }
            if (query != null)
            {
                bsonWriter.WriteName("query");
                query.WriteTo(bsonWriter);
            }
            if (updateObject != null)
            {
                bsonWriter.WriteName("updateobj");
                updateObject.WriteTo(bsonWriter);
            }
            if (cursorId != 0)
            {
                bsonWriter.WriteInt64("cursorid", cursorId);
            }
            if (numberToReturn != 0)
            {
                bsonWriter.WriteInt32("ntoreturn", numberToReturn);
            }
            if (numberToSkip != 0)
            {
                bsonWriter.WriteInt32("ntoskip", numberToSkip);
            }
            if (exhaust)
            {
                bsonWriter.WriteBoolean("exhaust", exhaust);
            }
            if (numberScanned != 0)
            {
                bsonWriter.WriteInt32("nscanned", numberScanned);
            }
            if (idHack)
            {
                bsonWriter.WriteBoolean("idhack", idHack);
            }
            if (scanAndOrder)
            {
                bsonWriter.WriteBoolean("scanAndOrder", scanAndOrder);
            }
            if (moved)
            {
                bsonWriter.WriteBoolean("moved", moved);
            }
            if (fastMod)
            {
                bsonWriter.WriteBoolean("fastmod", fastMod);
            }
            if (fastModInsert)
            {
                bsonWriter.WriteBoolean("fastmodinsert", fastModInsert);
            }
            if (upsert)
            {
                bsonWriter.WriteBoolean("upsert", upsert);
            }
            if (keyUpdates != 0)
            {
                bsonWriter.WriteInt32("keyUpdates", keyUpdates);
            }
            if (exception != null)
            {
                bsonWriter.WriteString("exception", exception);
            }
            if (exceptionCode != 0)
            {
                bsonWriter.WriteInt32("exceptionCode", exceptionCode);
            }
            if (numberReturned != 0)
            {
                bsonWriter.WriteInt32("nreturned", numberReturned);
            }
            if (responseLength != 0)
            {
                bsonWriter.WriteInt32("responseLength", responseLength);
            }
            bsonWriter.WriteDouble("millis", duration.TotalMilliseconds);
            if (client != null)
            {
                bsonWriter.WriteString("client", client);
            }
            if (user != null)
            {
                bsonWriter.WriteString("user", user);
            }
            if (error != null)
            {
                bsonWriter.WriteString("err", error);
            }
            if (abbreviated != null)
            {
                bsonWriter.WriteString("abbreviated", abbreviated);
            }
            bsonWriter.WriteEndDocument();
        }

        void IBsonSerializable.SetDocumentId(object id)
        {
            throw new NotSupportedException();
        }
    }
}
