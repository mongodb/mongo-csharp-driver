/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp418
{
    public class CSharp418Tests
    {
        public class C
        {
            public ObjectId Id;
            public int X;
        }

        public class D : C
        {
            public int Y;
        }

        private MongoCollection<D> _collection;

        public CSharp418Tests()
        {
            _collection = LegacyTestConfiguration.GetCollection<D>();
        }

        [Fact]
        public void TestQueryAgainstInheritedField()
        {
            _collection.Drop();
            _collection.Insert(new D { X = 1, Y = 2 });

            var query = from d in _collection.AsQueryable<D>()
                        where d.X == 1
                        select d;

            var translatedQuery = MongoQueryTranslator.Translate(query);
            Assert.IsType<SelectQuery>(translatedQuery);
            Assert.Same(_collection, translatedQuery.Collection);
            Assert.Same(typeof(D), translatedQuery.DocumentType);

            var selectQuery = (SelectQuery)translatedQuery;
            Assert.Equal("(D d) => (d.X == 1)", ExpressionFormatter.ToString(selectQuery.Where));
            Assert.Null(selectQuery.OrderBy);
            Assert.Null(selectQuery.Projection);
            Assert.Null(selectQuery.Skip);
            Assert.Null(selectQuery.Take);

            Assert.Equal("{ \"X\" : 1 }", selectQuery.BuildQuery().ToJson());
            Assert.Equal(1, query.ToList().Count);
        }
    }
}
