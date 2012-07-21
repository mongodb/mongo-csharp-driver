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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.IO;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a document from the system.profile collection.
    /// </summary>
    [Serializable]
    [BsonSerializer(typeof(SystemProfileInfoSerializer))]
    public class SystemProfileInfo
    {
        // private fields
        private readonly BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileInfo"/> class.
        /// </summary>
        public SystemProfileInfo()
        {
            _document = new BsonDocument();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileInfo"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        internal SystemProfileInfo(BsonDocument document)
        {
            _document = document ?? new BsonDocument();
        }

        // public properties
        /// <summary>
        /// Gets or sets the abbreviated profile info (only used when the profile info would have exceeded 100KB).
        /// </summary>
        public string Abbreviated
        {
            get { return (string)_document.GetValue("abbreviated", null); }
            set { _document.Set("abbreviated", value); }
        }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        public string Client
        {
            get { return (string)_document.GetValue("client", null); }
            set { _document.Set("client", value); }
        }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        public BsonDocument Command
        {
            get { return (BsonDocument)_document.GetValue("command", null); }
            set { _document.Set("command", value); }
        }

        /// <summary>
        /// Gets or sets the cursor Id.
        /// </summary>
        public long CursorId
        {
            get { return (long)_document.GetValue("cursorid", 0L); }
            set { _document.Set("cursorid", value); }
        }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        public TimeSpan Duration
        {
            get { return TimeSpan.FromMilliseconds(_document.GetValue("millis", 0).ToDouble()); }
            set { _document.Set("millis", (double)value.Milliseconds); }
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Error
        {
            get { return (string)_document.GetValue("err", null); }
            set { _document.Set("err", value); }
        }

        /// <summary>
        /// Gets or sets the exception message.
        /// </summary>
        public string Exception
        {
            get { return (string)_document.GetValue("exception", null); }
            set { _document.Set("exception", value); }
        }

        /// <summary>
        /// Gets or sets the exception code.
        /// </summary>
        public int ExceptionCode
        {
            get { return (int)_document.GetValue("exceptionCode", 0); }
            set { _document.Set("exceptionCode", value); }
        }

        /// <summary>
        /// Gets or sets whether exhaust was true.
        /// </summary>
        public bool Exhaust
        {
            get { return (bool)_document.GetValue("exhaust", false); }
            set { _document.Set("exhaust", value); }
        }

        /// <summary>
        /// Gets or sets whether fastMod was true.
        /// </summary>
        public bool FastMod
        {
            get { return (bool)_document.GetValue("fastmod", false); }
            set { _document.Set("fastmod", value); }
        }

        /// <summary>
        /// Gets or sets whether fastModInsert was true.
        /// </summary>
        public bool FastModInsert
        {
            get { return (bool)_document.GetValue("fastmodinsert", false); }
            set { _document.Set("fastmodinsert", value); }
        }

        /// <summary>
        /// Gets or sets whether idHack was true.
        /// </summary>
        public bool IdHack
        {
            get { return (bool)_document.GetValue("idhack", false); }
            set { _document.Set("idhack", value); }
        }

        /// <summary>
        /// Gets or sets the info string (only present with pre 2.0 servers).
        /// </summary>
        public string Info
        {
            get { return (string)_document.GetValue("info", null); }
            set { _document.Set("info", value); }
        }

        /// <summary>
        /// Gets or sets the number of key updates.
        /// </summary>
        public int KeyUpdates
        {
            get { return (int)_document.GetValue("keyUpdates", 0); }
            set { _document.Set("keyUpdates", value); }
        }

        /// <summary>
        /// Gets or sets the lock statistics.
        /// </summary>
        /// <value>
        /// The lock statistics.
        /// </value>
        public SystemProfileLockStatistics LockStatistics
        {
            get
            {
                BsonValue value;
                if (!_document.TryGetValue("lockStatMillis", out value))
                {
                    return null;
                }

                return new SystemProfileLockStatistics(_document.GetValue("lockStatMillis").AsBsonDocument);
            }
            set
            {
                BsonDocument lockStatsDocument = null;
                if (value != null)
                {
                    lockStatsDocument = value.Raw;
                }
                _document.Set("lockStatMillis", lockStatsDocument);
            }
        }

        /// <summary>
        /// Gets or sets whether moved was true.
        /// </summary>
        public bool Moved
        {
            get { return (bool)_document.GetValue("moved", false); }
            set { _document.Set("moved", value); }
        }

        /// <summary>
        /// Gets or sets the namespace.
        /// </summary>
        public string Namespace
        {
            get { return (string)_document.GetValue("ns", null); }
            set { _document.Set("ns", value); }
        }

        /// <summary>
        /// Gets or sets the number of documents returned.
        /// </summary>
        public int NumberReturned
        {
            get { return (int)_document.GetValue("nreturned", 0); }
            set { _document.Set("nreturned", value); }
        }

        /// <summary>
        /// Gets or sets the number of documents scanned.
        /// </summary>
        public int NumberScanned
        {
            get { return (int)_document.GetValue("nscanned", 0); }
            set { _document.Set("nscanned", value); }
        }

        /// <summary>
        /// Gets or sets the number of documents to return.
        /// </summary>
        public int NumberToReturn
        {
            get { return (int)_document.GetValue("ntoreturn", 0); }
            set { _document.Set("ntoreturn", value); }
        }

        /// <summary>
        /// Gets or sets the number of documents to skip.
        /// </summary>
        public int NumberToSkip
        {
            get { return (int)_document.GetValue("ntoskip", 0); }
            set { _document.Set("ntoskip", value); }
        }

        /// <summary>
        /// Gets or sets the number of yields.
        /// </summary>
        public int NumberOfYields
        {
            get { return (int)_document.GetValue("numYield", 0); }
            set { _document.Set("numYield", value); }
        }

        /// <summary>
        /// Gets or sets the operation.
        /// </summary>
        public string Op
        {
            get { return (string)_document.GetValue("op", null); }
            set { _document.Set("op", value); }
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        public BsonDocument Query
        {
            get { return (BsonDocument)_document.GetValue("query", null); }
            set { _document.Set("query", value); }
        }

        /// <summary>
        /// Gets the raw document.
        /// </summary>
        public BsonDocument Raw
        {
            get { return _document; }
        }

        /// <summary>
        /// Gets or sets the response length.
        /// </summary>
        public int ResponseLength
        {
            get { return (int)_document.GetValue("responseLength", 0); }
            set { _document.Set("responseLength", value); }
        }

        /// <summary>
        /// Gets or sets whether scanAndOrder was true.
        /// </summary>
        public bool ScanAndOrder
        {
            get { return (bool)_document.GetValue("scanAndOrder", false); }
            set { _document.Set("scanAndOrder", value); }
        }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp
        {
            get { return (DateTime)_document.GetValue("ts", DateTime.MinValue); }
            set { _document.Set("ts", value); }
        }

        /// <summary>
        /// Gets or sets the update object.
        /// </summary>
        public BsonDocument UpdateObject
        {
            get { return (BsonDocument)_document.GetValue("updateobj", null); }
            set { _document.Set("updateobj", value); }
        }

        /// <summary>
        /// Gets or sets whether upsert was true.
        /// </summary>
        public bool Upsert
        {
            get { return (bool)_document.GetValue("upsert", false); }
            set { _document.Set("upsert", value); }
        }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        public string User
        {
            get { return (string)_document.GetValue("user", null); }
            set { _document.Set("user", value); }
        }
    }

    /// <summary>
    /// Statistics about locks for a system.profile document.
    /// </summary>
    [Serializable]
    [BsonSerializer(typeof(SystemProfileLockStatisticsSerializer))]
    public class SystemProfileLockStatistics
    {
        // private fields
        private readonly BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileLockStatistics"/> class.
        /// </summary>
        public SystemProfileLockStatistics()
        {
            _document = new BsonDocument();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileLockStatistics"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        internal SystemProfileLockStatistics(BsonDocument document)
        {
            _document = document;
        }

        // public properties
        /// <summary>
        /// Gets the raw.
        /// </summary>
        public BsonDocument Raw
        {
            get { return _document; }
        }

        /// <summary>
        /// Gets or sets the time acquiring.
        /// </summary>
        public SystemProfileReadWriteLockStatistics TimeAcquiring
        {
            get { return GetReadWriteStatistics("timeAcquiring"); }
            set { SetReadWriteStatistics("timeAcquiring", value); }
        }

        /// <summary>
        /// Gets or sets the time locked.
        /// </summary>
        public SystemProfileReadWriteLockStatistics TimeLocked
        {
            get { return GetReadWriteStatistics("timeLocked"); }
            set { SetReadWriteStatistics("timeLocked", value); }
        }

        // private methods
        private SystemProfileReadWriteLockStatistics GetReadWriteStatistics(string name)
        {
            BsonValue doc;
            if (!_document.TryGetValue(name, out doc))
            {
                return null;
            }

            return new SystemProfileReadWriteLockStatistics(doc.AsBsonDocument);
        }

        private void SetReadWriteStatistics(string name, SystemProfileReadWriteLockStatistics value)
        {
            BsonDocument doc = null;
            if (value != null)
            {
                doc = value.Raw;
            }
            _document.Set(name, doc);
        }
    }

    /// <summary>
    /// Statistics about system.profile read and write time spent in locks.
    /// </summary>
    [Serializable]
    [BsonSerializer(typeof(SystemProfileReadWriteLockStatisticsSerializer))]
    public class SystemProfileReadWriteLockStatistics
    {
        // private fields
        private readonly BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileReadWriteLockStatistics"/> class.
        /// </summary>
        public SystemProfileReadWriteLockStatistics()
        {
            _document = new BsonDocument();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileReadWriteLockStatistics"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        internal SystemProfileReadWriteLockStatistics(BsonDocument document)
        {
            _document = document;
        }

        // public properties
        /// <summary>
        /// Gets the raw document underneath the lock statistics.
        /// </summary>
        public BsonDocument Raw
        {
            get { return _document; }
        }

        /// <summary>
        /// Gets or sets the time spent for a read.
        /// </summary>
        public TimeSpan Read
        {
            get { return GetTimeSpan("r"); }
            set { SetTimeSpan("r", value); }
        }

        /// <summary>
        /// Gets or sets the time spent for a write.
        /// </summary>
        public TimeSpan Write
        {
            get { return GetTimeSpan("w"); }
            set { SetTimeSpan("w", value); }
        }

        // private methods
        private TimeSpan GetTimeSpan(string name)
        {
            return TimeSpan.FromMilliseconds(_document.GetValue("r", 0).ToDouble());
        }

        private void SetTimeSpan(string name, TimeSpan value)
        {
            _document.Set(name, BsonInt64.Create(value.Milliseconds));
        }
    }

    /// <summary>
    /// Represents a serializer for SystemProfileInfo.
    /// </summary>
    public class SystemProfileInfoSerializer : BsonDocumentBackedClassSerializer<SystemProfileInfo>
    {
        // public static fields
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static SystemProfileInfoSerializer Instance = new SystemProfileInfoSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileInfoSerializer"/> class.
        /// </summary>
        public SystemProfileInfoSerializer()
        {
            RegisterMember("Abbreviated", "abbreviated", StringSerializer.Instance, typeof(string), null);
            RegisterMember("Client", "client", StringSerializer.Instance, typeof(string), null);
            RegisterMember("Command", "command", BsonDocumentSerializer.Instance, typeof(BsonDocument), null);
            RegisterMember("CursorId", "cursorid", Int64Serializer.Instance, typeof(long), null);
            RegisterMember("Duration", "millis", TimeSpanSerializer.Instance, typeof(TimeSpan), new TimeSpanSerializationOptions(BsonType.Double, TimeSpanUnits.Milliseconds));
            RegisterMember("Error", "err", StringSerializer.Instance, typeof(string), null);
            RegisterMember("Exception", "exception", StringSerializer.Instance, typeof(string), null);
            RegisterMember("ExceptionCode", "exceptionCode", Int32Serializer.Instance, typeof(int), null);
            RegisterMember("Exhaust", "exhaust", BooleanSerializer.Instance, typeof(bool), null);
            RegisterMember("FastMod", "fastmod", BooleanSerializer.Instance, typeof(bool), null);
            RegisterMember("FastModInsert", "fastmodinsert", BooleanSerializer.Instance, typeof(bool), null);
            RegisterMember("IdHack", "idhack", BooleanSerializer.Instance, typeof(bool), null);
            RegisterMember("Info", "info", StringSerializer.Instance, typeof(string), null);
            RegisterMember("KeyUpdates", "keyUpdates", Int32Serializer.Instance, typeof(int), null);
            RegisterMember("LockStatistics", "lockStatMillis", SystemProfileLockStatisticsSerializer.Instance, typeof(SystemProfileLockStatistics), null);
            RegisterMember("Moved", "moved", BooleanSerializer.Instance, typeof(bool), null);
            RegisterMember("Namespace", "ns", StringSerializer.Instance, typeof(string), null);
            RegisterMember("NumberReturned", "nreturned", Int32Serializer.Instance, typeof(int), null);
            RegisterMember("NumberScanned", "nscanned", Int32Serializer.Instance, typeof(int), null);
            RegisterMember("NumberToReturn", "ntoreturn", Int32Serializer.Instance, typeof(int), null);
            RegisterMember("NumberToSkip", "ntoskip", Int32Serializer.Instance, typeof(int), null);
            RegisterMember("NumberOfYields", "numYield", Int32Serializer.Instance, typeof(int), null);
            RegisterMember("Op", "op", StringSerializer.Instance, typeof(string), null);
            RegisterMember("Query", "query", BsonDocumentSerializer.Instance, typeof(BsonDocument), null);
            RegisterMember("ResponseLength", "responseLength", Int32Serializer.Instance, typeof(int), null);
            RegisterMember("ScanAndOrder", "scanAndOrder", BooleanSerializer.Instance, typeof(bool), null);
            RegisterMember("Timestamp", "ts", DateTimeSerializer.Instance, typeof(DateTime), null);
            RegisterMember("UpdateObject", "updateobj", BsonDocumentSerializer.Instance, typeof(BsonDocument), null);
            RegisterMember("Upsert", "upsert", BooleanSerializer.Instance, typeof(bool), null);
            RegisterMember("User", "user", StringSerializer.Instance, typeof(string), null);
        }

        // protected methods
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        protected override SystemProfileInfo CreateInstance(BsonDocument document)
        {
            return new SystemProfileInfo(document);
        }

        /// <summary>
        /// Gets the backing document.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns></returns>
        protected override BsonDocument GetBackingDocument(SystemProfileInfo instance)
        {
            return instance.Raw;
        }
    }

    /// <summary>
    /// Serializer for SystemProfileLockStatistics
    /// </summary>
    public class SystemProfileLockStatisticsSerializer : BsonDocumentBackedClassSerializer<SystemProfileLockStatistics>
    {
        // public static fields
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static SystemProfileLockStatisticsSerializer Instance = new SystemProfileLockStatisticsSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileLockStatisticsSerializer"/> class.
        /// </summary>
        public SystemProfileLockStatisticsSerializer()
        {
            RegisterMember("TimeAcquiring", "timeAcquiring", SystemProfileReadWriteLockStatisticsSerializer.Instance, typeof(SystemProfileReadWriteLockStatistics), null);
            RegisterMember("TimeLocked", "timeLocked", SystemProfileReadWriteLockStatisticsSerializer.Instance, typeof(SystemProfileReadWriteLockStatistics), null);
        }

        // protected methods
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        protected override SystemProfileLockStatistics CreateInstance(BsonDocument document)
        {
            return new SystemProfileLockStatistics(document);
        }

        /// <summary>
        /// Gets the backing document.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns></returns>
        protected override BsonDocument GetBackingDocument(SystemProfileLockStatistics instance)
        {
            return instance.Raw;
        }
    }

    /// <summary>
    /// Serializer for SystemProfileReadWriteLockStatistics
    /// </summary>
    public class SystemProfileReadWriteLockStatisticsSerializer : BsonDocumentBackedClassSerializer<SystemProfileReadWriteLockStatistics>
    {
        //public static fields
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static readonly SystemProfileReadWriteLockStatisticsSerializer Instance = new SystemProfileReadWriteLockStatisticsSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileReadWriteLockStatisticsSerializer"/> class.
        /// </summary>
        public SystemProfileReadWriteLockStatisticsSerializer()
        { 
            var timeSpanSerializationOptions = new TimeSpanSerializationOptions(BsonType.Double, TimeSpanUnits.Milliseconds);
            RegisterMember("Read", "r", TimeSpanSerializer.Instance, typeof(TimeSpan), timeSpanSerializationOptions);
            RegisterMember("Write", "w", TimeSpanSerializer.Instance, typeof(TimeSpan), timeSpanSerializationOptions);
        }

        // protected methods
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        protected override SystemProfileReadWriteLockStatistics CreateInstance(BsonDocument document)
        {
            return new SystemProfileReadWriteLockStatistics(document);
        }

        /// <summary>
        /// Gets the backing document.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns></returns>
        protected override BsonDocument GetBackingDocument(SystemProfileReadWriteLockStatistics instance)
        {
            return instance.Raw;
        }
    }
}