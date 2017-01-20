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

using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp111
{
    public class CSharp111Tests
    {
#pragma warning disable 649 // never assigned to
        private class C
        {
            public ObjectId Id;
            public List<D> InnerObjects;
        }
#pragma warning restore

        private class D
        {
            public int X;
        }

        [Fact]
        public void TestAddToSetEach()
        {
            var server = LegacyTestConfiguration.Server;
            var database = LegacyTestConfiguration.Database;
            var collection = LegacyTestConfiguration.GetCollection<C>();

            collection.RemoveAll();
            var c = new C { InnerObjects = new List<D>() };
            collection.Insert(c);
            var id = c.Id;

            var query = Query.EQ("_id", id);
            var update = Update.AddToSet("InnerObjects", 1);
            collection.Update(query, update);
            var d1 = new D { X = 1 };
            update = Update.AddToSetWrapped("InnerObjects", d1);
            collection.Update(query, update);

            var d2 = new D { X = 2 };
            var d3 = new D { X = 3 };
            update = Update.AddToSetEachWrapped("InnerObjects", d1, d2, d3);
            collection.Update(query, update);

            var document = collection.FindOneAs<BsonDocument>();
            var json = document.ToJson();
            string expected;
            if (server.BuildInfo.Version >= new Version(2, 6, 0))
            {
                expected = "{ '_id' : ObjectId('#ID'), 'InnerObjects' : [1, { 'X' : 1 }, { 'X' : 2 }, { 'X' : 3 }] }";
            }
            else
            {
                // prior to version 2.5.2 the server would reorder the elements when Update was used
                expected = "{ 'InnerObjects' : [1, { 'X' : 1 }, { 'X' : 2 }, { 'X' : 3 }], '_id' : ObjectId('#ID') }";
            }
            expected = expected.Replace("#ID", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);
        }
    }
}
