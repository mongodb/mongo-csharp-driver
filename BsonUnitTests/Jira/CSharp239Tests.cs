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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.BsonUnitTests.Jira.CSharp239
{
    [TestFixture]
    public class CSharp239Tests
    {
        public class Tree
        {
            public string Node;
            [BsonIgnoreIfNull]
            public Tree Left;
            [BsonIgnoreIfNull]
            public Tree Right;
        }


        [Test]
        public void TestSerialization()
        {
            var obj = new Tree
            {
                Node = "top",
                Left = new Tree { Node = "left" },
                Right = new Tree { Node = "right" }
            };
            var json = obj.ToJson();
            var expected = "{ 'Node' : 'top', 'Left' : { 'Node' : 'left' }, 'Right' : { 'Node' : 'right' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<Tree>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
