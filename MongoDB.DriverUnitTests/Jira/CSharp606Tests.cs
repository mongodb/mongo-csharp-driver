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

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp606
{
    [TestFixture]
    public class CSharp606Tests
    {
        [Test]
        public void TestTypeIsOnProperty()
        {
            IMongoQuery query = Query<TestClass>.Where(x => x.Prop is B);

            Assert.AreEqual("{ \"Prop._t\" : \"B\" }", query.ToString());
        }

        [Test]
        public void TestTypeOfComparisonOnProperty()
        {
            IMongoQuery query = Query<TestClass>.Where(x => x.Prop.GetType() == typeof(B));

            Assert.AreEqual("{ \"Prop._t.0\" : { \"$exists\" : false }, \"Prop._t\" : \"B\" }", query.ToString());
        }

        private class TestClass
        {
            public ObjectId Id { get; set; }

            public A Prop { get; set; }
        }
        private class A
        {
            public string String { get; set; }
        }

        private class B : A
        {
            public int Int { get; set; }
        }
    }
}