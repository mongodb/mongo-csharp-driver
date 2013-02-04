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

using System.Linq;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp100
{
    [TestFixture]
    public class CSharp613Tests
    {
        public class C
        {
            public int _id;
            public short S;
        }

        [Test]
        public void TestShortToIntImplicitConversion()
        {
            var collection = Configuration.TestCollection;

            collection.RemoveAll();
            collection.Save(new C { S = 2 });
            var query =
                from x in collection.AsQueryable<C>()
                where x.S == 2
                select x;
            var res = query.FirstOrDefault();
            Assert.IsNotNull(res);
            Assert.That(res.S, Is.EqualTo(2));
        }
    }
}
