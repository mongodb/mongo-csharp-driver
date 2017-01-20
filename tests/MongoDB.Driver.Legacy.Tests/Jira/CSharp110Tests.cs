/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp110
{
    public class CSharp110Tests
    {
#pragma warning disable 649 // never assigned to
        private class C
        {
            public ObjectId Id;
            public int X;
        }
#pragma warning restore

        [Fact]
        public void TestFind()
        {
            var server = LegacyTestConfiguration.Server;
            var database = LegacyTestConfiguration.Database;
            var collection = LegacyTestConfiguration.GetCollection<C>();

            collection.RemoveAll();
            var c = new C { X = 1 };
            collection.Insert(c);
            c = new C { X = 2 };
            collection.Insert(c);

            var query = Query.EQ("X", 2);
            foreach (var document in collection.Find(query))
            {
                Assert.NotEqual(ObjectId.Empty, document.Id);
                Assert.Equal(2, document.X);
            }
        }
    }
}
