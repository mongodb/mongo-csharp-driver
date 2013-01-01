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

using System.Collections.Generic;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp100
{
    [TestFixture]
    public class CSharp100Tests
    {
        [DataContract]
        private class ParentClass
        {
            [BsonId]
            public ObjectId Id { get; set; }
        }

        [DataContract]
        private class ChildClass : ParentClass
        {
            [DataMember(Order = 1)]
            [BsonIgnoreIfNull]
            public IList<SomeClass> SomeProperty { get; set; }
        }

        [DataContract]
        public class SomeClass
        {
        }

        [Test]
        public void TestDeserializationOfTwoBs()
        {
            var server = Configuration.TestServer;
            var database = Configuration.TestDatabase;
            var collection = Configuration.TestCollection;

            collection.RemoveAll();
            var obj = new ChildClass { SomeProperty = null };
            collection.Save(obj);
            obj = new ChildClass { SomeProperty = new List<SomeClass>() };
            collection.Save(obj);
            obj = new ChildClass { SomeProperty = new List<SomeClass> { new SomeClass() } };
            collection.Save(obj);
            obj = new ChildClass { SomeProperty = new List<SomeClass> { new SomeClass(), new SomeClass() } };
            collection.Save(obj);
            obj = new ChildClass { SomeProperty = new[] { new SomeClass(), new SomeClass() } };
            collection.Save(obj);
        }
    }
}
