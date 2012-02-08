﻿/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonElementTests
    {
        [Test]
        public void TestNewBsonArray()
        {
            BsonArray array;
            array = new BsonArray(new List<int>() { 1, 2, 3 });
            array = new BsonArray(new int[] { 4, 5, 6 });
        }

        [Test]
        public void TestStringElement()
        {
            BsonElement element = new BsonElement("abc", "def");
            string value = element.Value.AsString;
            Assert.AreEqual("abc", element.Name);
            Assert.AreEqual("def", value);
        }
    }
}
