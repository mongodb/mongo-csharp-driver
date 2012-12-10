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
    public class SystemProfileInfo : BsonDocumentBackedClass
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileInfo"/> class.
        /// </summary>
        public SystemProfileInfo()
            : this(new BsonDocument())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileInfo"/> class.
        /// </summary>
        /// <param name="backingDocument">The backing document.</param>
        internal SystemProfileInfo(BsonDocument backingDocument)
            : base(backingDocument, SystemProfileInfoSerializer.Instance)
        { }

        // public properties
        /// <summary>
        /// Gets or sets the abbreviated profile info (only used when the profile info would have exceeded 100KB).
        /// </summary>
        public string Abbreviated
        {
            get { return GetValue<string>("Abbreviated", null); }
            set { SetValue("Abbreviated", value); }
        }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        public string Client
        {
            get { return GetValue<string>("Client", null); }
            set { SetValue("Client", value); }
        }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        public BsonDocument Command
        {
            get { return GetValue<BsonDocument>("Command", null); }
            set { SetValue("Command", value); }
        }

        /// <summary>
        /// Gets or sets the cursor Id.
        /// </summary>
        public long CursorId
        {
            get { return (long)GetValue("CursorId", 0L); }
            set { SetValue("CursorId", value); }
        }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        public TimeSpan Duration
        {
            get { return GetValue<TimeSpan>("Duration", TimeSpan.Zero); }
            set { SetValue("Duration", value); }
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Error
        {
            get { return GetValue<string>("Error", null); }
            set { SetValue("Error", value); }
        }

        /// <summary>
        /// Gets or sets the exception message.
        /// </summary>
        public string Exception
        {
            get { return GetValue<string>("Exception", null); }
            set { SetValue("Exception", value); }
        }

        /// <summary>
        /// Gets or sets the exception code.
        /// </summary>
        public int ExceptionCode
        {
            get { return GetValue<int>("ExceptionCode", 0); }
            set { SetValue("ExceptionCode", value); }
        }

        /// <summary>
        /// Gets or sets whether exhaust was true.
        /// </summary>
        public bool Exhaust
        {
            get { return GetValue<bool>("Exhaust", false); }
            set { SetValue("Exhaust", value); }
        }

        /// <summary>
        /// Gets or sets whether fastMod was true.
        /// </summary>
        public bool FastMod
        {
            get { return GetValue<bool>("FastMod", false); }
            set { SetValue("FastMod", value); }
        }

        /// <summary>
        /// Gets or sets whether fastModInsert was true.
        /// </summary>
        public bool FastModInsert
        {
            get { return GetValue<bool>("FastModInsert", false); }
            set { SetValue("FastModInsert", value); }
        }

        /// <summary>
        /// Gets or sets whether idHack was true.
        /// </summary>
        public bool IdHack
        {
            get { return GetValue<bool>("IdHack", false); }
            set { SetValue("IdHack", value); }
        }

        /// <summary>
        /// Gets or sets the info string (only present with pre 2.0 servers).
        /// </summary>
        public string Info
        {
            get { return GetValue<string>("Info", null); }
            set { SetValue("Info", value); }
        }

        /// <summary>
        /// Gets or sets the number of key updates.
        /// </summary>
        public int KeyUpdates
        {
            get { return GetValue<int>("KeyUpdates", 0); }
            set { SetValue("KeyUpdates", value); }
        }

        /// <summary>
        /// Gets or sets the lock statistics.
        /// </summary>
        public SystemProfileLockStatistics LockStatistics
        {
            get { return GetValue<SystemProfileLockStatistics>("LockStatistics", null); }
            set { SetValue("LockStatistics", value); }
        }

        /// <summary>
        /// Gets or sets whether moved was true.
        /// </summary>
        public bool Moved
        {
            get { return GetValue<bool>("Moved", false); }
            set { SetValue("Moved", value); }
        }

        /// <summary>
        /// Gets or sets the namespace.
        /// </summary>
        public string Namespace
        {
            get { return GetValue<string>("Namespace", null); }
            set { SetValue("Namespace", value); }
        }

        /// <summary>
        /// Gets or sets the number of documents returned.
        /// </summary>
        public int NumberReturned
        {
            get { return GetValue<int>("NumberReturned", 0); }
            set { SetValue("NumberReturned", value); }
        }

        /// <summary>
        /// Gets or sets the number of documents scanned.
        /// </summary>
        public int NumberScanned
        {
            get { return GetValue<int>("NumberScanned", 0); }
            set { SetValue("NumberScanned", value); }
        }

        /// <summary>
        /// Gets or sets the number of documents to return.
        /// </summary>
        public int NumberToReturn
        {
            get { return GetValue<int>("NumberToReturn", 0); }
            set { SetValue("NumberToReturn", value); }
        }

        /// <summary>
        /// Gets or sets the number of documents to skip.
        /// </summary>
        public int NumberToSkip
        {
            get { return GetValue<int>("NumberToSkip", 0); }
            set { SetValue("NumberToSkip", value); }
        }

        /// <summary>
        /// Gets or sets the number of yields.
        /// </summary>
        public int NumberOfYields
        {
            get { return GetValue<int>("NumberOfYields", 0); }
            set { SetValue("NumberOfYields", value); }
        }

        /// <summary>
        /// Gets or sets the operation.
        /// </summary>
        public string Op
        {
            get { return GetValue<string>("Op", null); }
            set { SetValue("Op", value); }
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        public BsonDocument Query
        {
            get { return GetValue<BsonDocument>("Query", null); }
            set { SetValue("Query", value); }
        }

        /// <summary>
        /// Gets the raw document.
        /// </summary>
        public BsonDocument RawDocument
        {
            get { return BackingDocument; }
        }

        /// <summary>
        /// Gets or sets the response length.
        /// </summary>
        public int ResponseLength
        {
            get { return GetValue<int>("ResponseLength", 0); }
            set { SetValue("ResponseLength", value); }
        }

        /// <summary>
        /// Gets or sets whether scanAndOrder was true.
        /// </summary>
        public bool ScanAndOrder
        {
            get { return GetValue<bool>("ScanAndOrder", false); }
            set { SetValue("ScanAndOrder", value); }
        }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp
        {
            get { return GetValue<DateTime>("Timestamp", DateTime.MinValue); }
            set { SetValue("Timestamp", value); }
        }

        /// <summary>
        /// Gets or sets the update object.
        /// </summary>
        public BsonDocument UpdateObject
        {
            get { return GetValue<BsonDocument>("UpdateObject", null); }
            set { SetValue("UpdateObject", value); }
        }

        /// <summary>
        /// Gets or sets whether upsert was true.
        /// </summary>
        public bool Upsert
        {
            get { return GetValue<bool>("Upsert", false); }
            set { SetValue("Upsert", value); }
        }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        public string User
        {
            get { return GetValue<string>("User", null); }
            set { SetValue("User", value); }
        }
    }

    /// <summary>
    /// Statistics about locks for a system.profile document.
    /// </summary>
    [Serializable]
    [BsonSerializer(typeof(SystemProfileLockStatisticsSerializer))]
    public class SystemProfileLockStatistics : BsonDocumentBackedClass
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileLockStatistics"/> class.
        /// </summary>
        public SystemProfileLockStatistics()
            : this(new BsonDocument())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileLockStatistics"/> class.
        /// </summary>
        /// <param name="backingDocument">The backing document.</param>
        internal SystemProfileLockStatistics(BsonDocument backingDocument)
            : base(backingDocument, SystemProfileLockStatisticsSerializer.Instance)
        { }

        // public properties
        /// <summary>
        /// Gets the raw document.
        /// </summary>
        public BsonDocument RawDocument
        {
            get { return BackingDocument; }
        }

        /// <summary>
        /// Gets or sets the time acquiring.
        /// </summary>
        public SystemProfileReadWriteLockStatistics TimeAcquiring
        {
            get { return GetValue<SystemProfileReadWriteLockStatistics>("TimeAcquiring", null); }
            set { SetValue("TimeAcquiring", value); }
        }

        /// <summary>
        /// Gets or sets the time locked.
        /// </summary>
        public SystemProfileReadWriteLockStatistics TimeLocked
        {
            get { return GetValue<SystemProfileReadWriteLockStatistics>("TimeLocked", null); }
            set { SetValue("TimeLocked", value); }
        }
    }

    /// <summary>
    /// Statistics about system.profile read and write time spent in locks.
    /// </summary>
    [Serializable]
    [BsonSerializer(typeof(SystemProfileReadWriteLockStatisticsSerializer))]
    public class SystemProfileReadWriteLockStatistics : BsonDocumentBackedClass
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileReadWriteLockStatistics"/> class.
        /// </summary>
        public SystemProfileReadWriteLockStatistics()
            : this(new BsonDocument())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileReadWriteLockStatistics"/> class.
        /// </summary>
        /// <param name="backingDocument">The backing document.</param>
        internal SystemProfileReadWriteLockStatistics(BsonDocument backingDocument)
            : base(backingDocument, SystemProfileReadWriteLockStatisticsSerializer.Instance)
        { }

        // public properties
        /// <summary>
        /// Gets the raw document.
        /// </summary>
        public BsonDocument RawDocument
        {
            get { return BackingDocument; }
        }

        /// <summary>
        /// Gets or sets the time spent for a read.
        /// </summary>
        public TimeSpan Read
        {
            get { return GetValue("Read", TimeSpan.Zero); }
            set { SetValue("Read", value); }
        }

        /// <summary>
        /// Gets or sets the time spent for a write.
        /// </summary>
        public TimeSpan Write
        {
            get { return GetValue("Write", TimeSpan.Zero); }
            set { SetValue("Write", value); }
        }
    }

    /// <summary>
    /// Represents a serializer for SystemProfileInfo.
    /// </summary>
    public class SystemProfileInfoSerializer : BsonDocumentBackedClassSerializer<SystemProfileInfo>
    {
        // private static fields
        private static readonly SystemProfileInfoSerializer __instance = new SystemProfileInfoSerializer();

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
            RegisterMember("LockStatistics", "lockStats", SystemProfileLockStatisticsSerializer.Instance, typeof(SystemProfileLockStatistics), null);
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

        // public static properties
        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static SystemProfileInfoSerializer Instance
        {
            get { return __instance; }
        }

        // protected methods
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="backingDocument">The backing document.</param>
        /// <returns>A SystemProfileInfo instance.</returns>
        protected override SystemProfileInfo CreateInstance(BsonDocument backingDocument)
        {
            return new SystemProfileInfo(backingDocument);
        }
    }

    /// <summary>
    /// Serializer for SystemProfileLockStatistics
    /// </summary>
    public class SystemProfileLockStatisticsSerializer : BsonDocumentBackedClassSerializer<SystemProfileLockStatistics>
    {
        // private static fields
        private static readonly SystemProfileLockStatisticsSerializer __instance = new SystemProfileLockStatisticsSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileLockStatisticsSerializer"/> class.
        /// </summary>
        public SystemProfileLockStatisticsSerializer()
        {
            RegisterMember("TimeAcquiring", "timeAcquiringMicros", SystemProfileReadWriteLockStatisticsSerializer.Instance, typeof(SystemProfileReadWriteLockStatistics), null);
            RegisterMember("TimeLocked", "timeLockedMicros", SystemProfileReadWriteLockStatisticsSerializer.Instance, typeof(SystemProfileReadWriteLockStatistics), null);
        }

        // public static properties
        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static SystemProfileLockStatisticsSerializer Instance
        {
            get { return __instance; }
        }

        // protected methods
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="backingDocument">The backing document.</param>
        /// <returns>A SystemProfileLockStatistics instance.</returns>
        protected override SystemProfileLockStatistics CreateInstance(BsonDocument backingDocument)
        {
            return new SystemProfileLockStatistics(backingDocument);
        }
    }

    /// <summary>
    /// Serializer for SystemProfileReadWriteLockStatistics
    /// </summary>
    public class SystemProfileReadWriteLockStatisticsSerializer : BsonDocumentBackedClassSerializer<SystemProfileReadWriteLockStatistics>
    {
        // private static fields
        private static readonly SystemProfileReadWriteLockStatisticsSerializer __instance = new SystemProfileReadWriteLockStatisticsSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemProfileReadWriteLockStatisticsSerializer"/> class.
        /// </summary>
        public SystemProfileReadWriteLockStatisticsSerializer()
        { 
            var timeSpanSerializationOptions = new TimeSpanSerializationOptions(BsonType.Double, TimeSpanUnits.Microseconds);
            RegisterMember("Read", "r", TimeSpanSerializer.Instance, typeof(TimeSpan), timeSpanSerializationOptions);
            RegisterMember("Write", "w", TimeSpanSerializer.Instance, typeof(TimeSpan), timeSpanSerializationOptions);
        }

        // public static properties
        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static SystemProfileReadWriteLockStatisticsSerializer Instance
        {
            get { return __instance; }
        }

        // protected methods
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="backingDocument">The backing document.</param>
        /// <returns>A SystemProfileReadWriteLockStatistics instance.</returns>
        protected override SystemProfileReadWriteLockStatistics CreateInstance(BsonDocument backingDocument)
        {
            return new SystemProfileReadWriteLockStatistics(backingDocument);
        }
    }
}