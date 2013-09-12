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

using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp803Tests
    {
        private abstract class BaseClassWithProperty
        {
            public abstract int Id { get; set; }
        }

        private class PropertyImpl : BaseClassWithProperty
        {
            public override int Id { get; set; }
        }

        [Test]
        public void TestSerialization()
        {
            var impl = new PropertyImpl { Id = 1 };
            var doc = impl.ToBsonDocument();
            var expected = new BsonDocument("_id", 1);
            Assert.AreEqual(expected, doc);
        }
    }
}
