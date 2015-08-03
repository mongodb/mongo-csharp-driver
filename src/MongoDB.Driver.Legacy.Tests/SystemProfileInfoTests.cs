/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class SystemProfileInfoTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = LegacyTestConfiguration.Server;
            _database = LegacyTestConfiguration.Database;
            _collection = LegacyTestConfiguration.Collection;
        }

        [Test]
        public void TestMinimal()
        {
            var info = new SystemProfileInfo
            {
                Timestamp = new DateTime(2011, 10, 7, 0, 0, 0, DateTimeKind.Utc),
                Duration = TimeSpan.FromMilliseconds(123)
            };
            var expected = "{ 'ts' : ISODate('2011-10-07T00:00:00Z'), 'millis' : 123.0 }".Replace("'", "\"");
            Assert.AreEqual(expected, info.ToJson());
        }

        [Test]
        public void TestDeserializeAll()
        {
            var info = new SystemProfileInfo
            {
                Abbreviated = "abbreviated",
                Client = "client",
                Command = new BsonDocument("command", 1),
                CursorId = 1,
                Duration = TimeSpan.FromMilliseconds(2),
                Error = "err",
                Exception = "exception",
                ExceptionCode = 3,
                Exhaust = true,
                FastMod = true,
                FastModInsert = true,
                IdHack = true,
                Info = "info",
                KeyUpdates = 4,
                LockStatistics = new SystemProfileLockStatistics
                {
                    TimeAcquiring = new SystemProfileReadWriteLockStatistics
                    {
                        DatabaseReadLock = TimeSpan.FromMilliseconds(10),
                        DatabaseWriteLock = TimeSpan.FromMilliseconds(20),
                        GlobalReadLock = TimeSpan.FromMilliseconds(30),
                        GlobalWriteLock = TimeSpan.FromMilliseconds(40)
                    },
                    TimeLocked = new SystemProfileReadWriteLockStatistics
                    {
                        DatabaseReadLock = TimeSpan.FromMilliseconds(50),
                        DatabaseWriteLock = TimeSpan.FromMilliseconds(60),
                        GlobalReadLock = TimeSpan.FromMilliseconds(70),
                        GlobalWriteLock = TimeSpan.FromMilliseconds(80)
                    }
                },
                Moved = true,
                Namespace = "ns",
                NumberMoved = 11,
                NumberReturned = 5,
                NumberScanned = 6,
                NumberToReturn = 7,
                NumberToSkip = 8,
                NumberUpdated = 9,
                NumberOfYields = 10,
                Op = "op",
                Query = new BsonDocument("query", 1),
                ResponseLength = 9,
                ScanAndOrder = true,
                Timestamp = new DateTime(2011, 10, 7, 1, 2, 3, DateTimeKind.Utc),
                UpdateObject = new BsonDocument("updateObject", 1),
                Upsert = true,
                User = "user"
            };
            var json = info.ToJson(new JsonWriterSettings { Indent = true });
            var rehydrated = BsonSerializer.Deserialize<SystemProfileInfo>(json);
            Assert.AreEqual(info.Abbreviated, rehydrated.Abbreviated);
            Assert.AreEqual(info.Client, rehydrated.Client);
            Assert.AreEqual(info.Command, rehydrated.Command);
            Assert.AreEqual(info.CursorId, rehydrated.CursorId);
            Assert.AreEqual(info.Duration, rehydrated.Duration);
            Assert.AreEqual(info.Error, rehydrated.Error);
            Assert.AreEqual(info.Exception, rehydrated.Exception);
            Assert.AreEqual(info.ExceptionCode, rehydrated.ExceptionCode);
            Assert.AreEqual(info.Exhaust, rehydrated.Exhaust);
            Assert.AreEqual(info.FastMod, rehydrated.FastMod);
            Assert.AreEqual(info.FastModInsert, rehydrated.FastModInsert);
            Assert.AreEqual(info.IdHack, rehydrated.IdHack);
            Assert.AreEqual(info.Info, rehydrated.Info);
            Assert.AreEqual(info.KeyUpdates, rehydrated.KeyUpdates);
            Assert.AreEqual(info.LockStatistics.RawDocument, rehydrated.LockStatistics.RawDocument);
            Assert.AreEqual(info.LockStatistics.TimeAcquiring.DatabaseReadLock, rehydrated.LockStatistics.TimeAcquiring.DatabaseReadLock);
            Assert.AreEqual(info.LockStatistics.TimeAcquiring.DatabaseWriteLock, rehydrated.LockStatistics.TimeAcquiring.DatabaseWriteLock);
            Assert.AreEqual(info.LockStatistics.TimeAcquiring.GlobalReadLock, rehydrated.LockStatistics.TimeAcquiring.GlobalReadLock);
            Assert.AreEqual(info.LockStatistics.TimeAcquiring.GlobalWriteLock, rehydrated.LockStatistics.TimeAcquiring.GlobalWriteLock);
            Assert.AreEqual(info.LockStatistics.TimeLocked.DatabaseReadLock, rehydrated.LockStatistics.TimeLocked.DatabaseReadLock);
            Assert.AreEqual(info.LockStatistics.TimeLocked.DatabaseWriteLock, rehydrated.LockStatistics.TimeLocked.DatabaseWriteLock);
            Assert.AreEqual(info.LockStatistics.TimeLocked.GlobalReadLock, rehydrated.LockStatistics.TimeLocked.GlobalReadLock);
            Assert.AreEqual(info.LockStatistics.TimeLocked.GlobalWriteLock, rehydrated.LockStatistics.TimeLocked.GlobalWriteLock);
            Assert.AreEqual(info.Moved, rehydrated.Moved);
            Assert.AreEqual(info.Namespace, rehydrated.Namespace);
            Assert.AreEqual(info.NumberMoved, rehydrated.NumberMoved);
            Assert.AreEqual(info.NumberReturned, rehydrated.NumberReturned);
            Assert.AreEqual(info.NumberScanned, rehydrated.NumberScanned);
            Assert.AreEqual(info.NumberToReturn, rehydrated.NumberToReturn);
            Assert.AreEqual(info.NumberToSkip, rehydrated.NumberToSkip);
            Assert.AreEqual(info.NumberUpdated, rehydrated.NumberUpdated);
            Assert.AreEqual(info.NumberOfYields, rehydrated.NumberOfYields);
            Assert.AreEqual(info.Op, rehydrated.Op);
            Assert.AreEqual(info.Query, rehydrated.Query);
            Assert.AreEqual(info.ResponseLength, rehydrated.ResponseLength);
            Assert.AreEqual(info.ScanAndOrder, rehydrated.ScanAndOrder);
            Assert.AreEqual(info.Timestamp, rehydrated.Timestamp);
            Assert.AreEqual(info.UpdateObject, rehydrated.UpdateObject);
            Assert.AreEqual(info.Upsert, rehydrated.Upsert);
            Assert.AreEqual(info.User, rehydrated.User);
        }

        [Test]
        public void TestDeserializeSystemProfileInfoReturnedFromServer()
        {
            if (_server.Primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                _database.SetProfilingLevel(ProfilingLevel.None);
                var systemProfileCollection = _database.GetCollection<SystemProfileInfo>("system.profile");
                if (systemProfileCollection.Exists())
                {
                    systemProfileCollection.Drop();
                }

                _database.SetProfilingLevel(ProfilingLevel.All);
                try
                {
                    _collection.Insert(new BsonDocument("foo", 1));
                }
                finally
                {
                    _database.SetProfilingLevel(ProfilingLevel.None);
                }

                var systemProfileInfo = systemProfileCollection.FindOne();

                // since we don't know what most of the values should be simply call all the properties and make sure they don't throw
                var abbreviated = systemProfileInfo.Abbreviated;
                var client = systemProfileInfo.Client;
                var command = systemProfileInfo.Command;
                var cursorId = systemProfileInfo.CursorId;
                var duration = systemProfileInfo.Duration;
                var error = systemProfileInfo.Error;
                var exception = systemProfileInfo.Exception;
                var exceptionCode = systemProfileInfo.ExceptionCode;
                var exhaust = systemProfileInfo.Exhaust;
                var fastMod = systemProfileInfo.FastMod;
                var fastModInsert = systemProfileInfo.FastModInsert;
                var idHack = systemProfileInfo.IdHack;
                var info = systemProfileInfo.Info;
                var keyUpdates = systemProfileInfo.KeyUpdates;
                var lockStatistics = systemProfileInfo.LockStatistics;
                var moved = systemProfileInfo.Moved;
                var ns = systemProfileInfo.Namespace;
                var numberMoved = systemProfileInfo.NumberMoved;
                var numberOfYields = systemProfileInfo.NumberOfYields;
                var numberReturned = systemProfileInfo.NumberReturned;
                var numberScanned = systemProfileInfo.NumberScanned;
                var numberToReturn = systemProfileInfo.NumberToReturn;
                var numberToSkip = systemProfileInfo.NumberToSkip;
                var numberUpdated = systemProfileInfo.NumberUpdated;
                var op = systemProfileInfo.Op;
                var query = systemProfileInfo.Query;
                var rawDocument = systemProfileInfo.RawDocument;
                var responseLength = systemProfileInfo.ResponseLength;
                var scanAndOrder = systemProfileInfo.ScanAndOrder;
                var timestamp = systemProfileInfo.Timestamp;
                var updateObject = systemProfileInfo.UpdateObject;
                var upsert = systemProfileInfo.Upsert;
                var user = systemProfileInfo.User;

                if (lockStatistics != null)
                {
                    var timeAcquiring = lockStatistics.TimeAcquiring;
                    var timeAcquiringDatabaseReadLock = timeAcquiring.DatabaseReadLock;
                    var timeAcquiringDatabaseWriteLock = timeAcquiring.DatabaseWriteLock;
                    var timeAcquiringGlobalReadLock = timeAcquiring.GlobalReadLock;
                    var timeAcquiringGlobalWriteLock = timeAcquiring.GlobalWriteLock;

                    var timeLocked = lockStatistics.TimeLocked;
                    var timeLockedDatabaseReadLock = timeLocked.DatabaseReadLock;
                    var timeLockedDatabaseWriteLock = timeLocked.DatabaseWriteLock;
                    var timeLockedGlobalReadLock = timeLocked.GlobalReadLock;
                    var timeLockedGlobalWriteLock = timeLocked.GlobalWriteLock;
                }
            }
        }

        [Test]
        public void TestLockStatsAreStoredInMicroSeconds()
        {
            string json = @"
            {
                ""lockStats"" : {
                    ""timeLockedMicros"" : { 
                        ""r"" : NumberLong(500), 
                        ""R"" : NumberLong(600), 
                        ""w"" : NumberLong(1000) 
                        ""W"" : NumberLong(1100) 
                    },
                    ""timeAcquiringMicros"" : { 
                        ""r"" : NumberLong(2500), 
                        ""R"" : NumberLong(2600), 
                        ""w"" : NumberLong(10000) 
                        ""W"" : NumberLong(11000) 
                    }
                }
            }";
            var rehydrated = BsonSerializer.Deserialize<SystemProfileInfo>(json);
            
            // 1 tick = 10 microseconds. Can't use TimeSpan.FromMilliseconds(0.5) because
            // TimeSpan.FromMilliseconds(0.5).TotalMilliseconds gives 1.0 (which makes no sense but that's the way it is)
            // to get precision below 1 millisecond you must use ticks.
            Assert.AreEqual(TimeSpan.FromTicks(5000), rehydrated.LockStatistics.TimeLocked.DatabaseReadLock);
            Assert.AreEqual(TimeSpan.FromTicks(6000), rehydrated.LockStatistics.TimeLocked.GlobalReadLock);
            Assert.AreEqual(TimeSpan.FromTicks(10000), rehydrated.LockStatistics.TimeLocked.DatabaseWriteLock);
            Assert.AreEqual(TimeSpan.FromTicks(11000), rehydrated.LockStatistics.TimeLocked.GlobalWriteLock);
            Assert.AreEqual(TimeSpan.FromTicks(25000), rehydrated.LockStatistics.TimeAcquiring.DatabaseReadLock);
            Assert.AreEqual(TimeSpan.FromTicks(26000), rehydrated.LockStatistics.TimeAcquiring.GlobalReadLock);
            Assert.AreEqual(TimeSpan.FromTicks(100000), rehydrated.LockStatistics.TimeAcquiring.DatabaseWriteLock);
            Assert.AreEqual(TimeSpan.FromTicks(110000), rehydrated.LockStatistics.TimeAcquiring.GlobalWriteLock);
        }
    }
}
