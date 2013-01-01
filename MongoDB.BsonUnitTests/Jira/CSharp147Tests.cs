/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp147
{
    [TestFixture]
    public class CSharp146Tests
    {
        public class Parent
        {
            public Child Child { get; set; }
        }

        public class Child
        {
            public Guid Id { get; set; }
            public int A { get; set; }
        }

        [Test]
        public void Test()
        {
            var p = new Parent { Child = new Child() };
            p.Child.A = 1;
            var json = p.ToJson();
            BsonSerializer.Deserialize<Parent>(json); // throws Unexpected element exception
        }
    }
}
