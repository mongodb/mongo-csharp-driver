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
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class SystemProfileInfoTests
    {
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
        public void TestAll()
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
                Moved = true,
                Namespace = "ns",
                NumberReturned = 5,
                NumberScanned = 6,
                NumberToReturn = 7,
                NumberToSkip = 8,
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
            Assert.AreEqual(info.Moved, rehydrated.Moved);
            Assert.AreEqual(info.Namespace, rehydrated.Namespace);
            Assert.AreEqual(info.NumberReturned, rehydrated.NumberReturned);
            Assert.AreEqual(info.NumberScanned, rehydrated.NumberScanned);
            Assert.AreEqual(info.NumberToReturn, rehydrated.NumberToReturn);
            Assert.AreEqual(info.NumberToSkip, rehydrated.NumberToSkip);
            Assert.AreEqual(info.Op, rehydrated.Op);
            Assert.AreEqual(info.Query, rehydrated.Query);
            Assert.AreEqual(info.ResponseLength, rehydrated.ResponseLength);
            Assert.AreEqual(info.ScanAndOrder, rehydrated.ScanAndOrder);
            Assert.AreEqual(info.Timestamp, rehydrated.Timestamp);
            Assert.AreEqual(info.UpdateObject, rehydrated.UpdateObject);
            Assert.AreEqual(info.Upsert, rehydrated.Upsert);
            Assert.AreEqual(info.User, rehydrated.User);
        }
    }
}
