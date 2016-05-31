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
using System.Linq;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp471Tests
    {
        public class Base
        {
            public Guid Id { get; set; }

            public string A { get; set; }
        }

        public class T1 : Base
        {
            public string B { get; set; }
        }

        public class T2 : Base
        {
            public string C { get; set; }
        }

        [Fact]
        public void CastTest()
        {
            var db = LegacyTestConfiguration.Database;
            var collection = db.GetCollection<Base>("castTest");
            collection.Drop();

            var t1 = new T1 { Id = Guid.NewGuid(), A = "T1.A", B = "T1.B" };
            var t2 = new T2 { Id = Guid.NewGuid(), A = "T2.A" };
            collection.Insert(t1);
            collection.Insert(t2);

            var query = from t in collection.AsQueryable()
                        where t is T1 && ((T1)t).B == "T1.B" 
                        select t;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(collection, translatedQuery.Collection);
            Assert.Same(typeof(Base), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(Base t) => ((t is T1) && ((T1)t.B == \"T1.B\"))", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"_t\" : \"T1\", \"B\" : \"T1.B\" }", selectQuery.BuildQuery().ToString());

            var results = query.ToList();
            Assert.Equal(1, results.Count);
            Assert.IsType<T1>(results[0]);
            Assert.Equal("T1.A", results[0].A);
        }
    }
}