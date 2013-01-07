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

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.BsonUnitTests.Jira.CSharp648
{
    [TestFixture]
    public class CSharp648Tests
    {
        public class C
        {
            public int Id;
            public U1 U1;
            public U2 U2;
            public U3 U3;
        }

        [BsonNoId] // suppressing the _id by using the [BsonNoId] attribute
        public class U1
        {
            public int Id;
        }

        public class U2
        {
            public int Id;
        }

        public class U3
        {
            public int Id;
        }

        static CSharp648Tests()
        {
            // suppressing the _id by registering the class map manually
            BsonClassMap.RegisterClassMap<U2>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(null);
            });

            // suppressing the _id by using the NoIdMemberConvention
            var pack = new ConventionPack();
            pack.Add(new NoIdMemberConvention());
            ConventionRegistry.Register("U3noid", pack, t => t == typeof(U3));
        }

        [Test]
        public void TestNoId()
        {
            var c = new C { Id = 1, U1 = new U1 { Id = 1 }, U2 = new U2 { Id = 2 }, U3 = new U3 { Id = 3 } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'U1' : { 'Id' : 1 }, 'U2' : { 'Id' : 2 }, 'U3' : { 'Id' : 3 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }
    }
}
