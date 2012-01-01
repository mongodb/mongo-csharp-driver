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
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.DriverOnlineTests.Jira.CSharp77
{
    [TestFixture]
    public class CSharp77Tests
    {
        private class Foo
        {
            public ObjectId _id { get; set; }
            public string Name { get; set; }
            public string Summary { get; set; }
        }

        [Test]
        public void TestSave()
        {
            var server = Configuration.TestServer;
            var database = Configuration.TestDatabase;
            var collection = Configuration.GetTestCollection<Foo>();

            var conventions = new ConventionProfile()
                .SetIdMemberConvention(new NamedIdMemberConvention("_id"));
            BsonClassMap.RegisterConventions(conventions, t => t == typeof(Foo));

            collection.RemoveAll();
            for (int i = 0; i < 10; i++)
            {
                var foo = new Foo
                {
                    _id = ObjectId.Empty,
                    Name = string.Format("Foo-{0}", i),
                    Summary = string.Format("Summary for Foo-{0}", i)
                };
                collection.Save(foo, SafeMode.True);
                var count = collection.Count();
                Assert.AreEqual(i + 1, count);
            }
        }
    }
}
