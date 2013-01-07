﻿/* Copyright 2010-2013 10gen Inc.
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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp238
{
    [TestFixture]
    public class CSharp238Tests
    {
        public class Point
        {
            public int X;
            public int Y;
        }

        public class C
        {
            public Dictionary<Point, Point> Points;
        }

        [Test]
        public void TestDeserializeDictionary()
        {
            var obj = new C { Points = new Dictionary<Point, Point>() };
            obj.Points.Add(new Point { X = 1, Y = 1 }, new Point { X = 2, Y = 2 });
            obj.Points.Add(new Point { X = 2, Y = 2 }, new Point { X = 3, Y = 3 });
            var json = obj.ToJson();
            var expected = "{ 'Points' : [#1, #2] }";
            expected = expected.Replace("#1", "[{ 'X' : 1, 'Y' : 1 }, { 'X' : 2, 'Y' : 2 }]");
            expected = expected.Replace("#2", "[{ 'X' : 2, 'Y' : 2 }, { 'X' : 3, 'Y' : 3 }]");
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
