/* Copyright 2010-present MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3922Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Select_with_anonymous_class_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(c => new { R = c.X })
                .Select(a => new { S = a.R });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { R : '$X', _id : 0 } }",
                "{ $project : { S : '$R', _id : 0 } }");
        }

        [Fact]
        public void Select_with_constructor_call_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(c => new D(c.X))
                .Select(d => new { R = d.X, S = d.Y })
                .Select(a => new { T = a.R, U = a.S });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { X : '$X', _id : 0 } }",
                "{ $project : { R : '$X', S : '$Y', _id : 0 } }",
                "{ $project : { T : '$R', U : '$S', _id : 0 } }");
        }

        [Fact]
        public void Select_with_constructor_call_and_property_set_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(c => new D(c.X) { Y = 123 })
                .Select(d => new { R = d.X, S = d.Y })
                .Select(a => new { T = a.R, U = a.S });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { X : '$X', Y : { $literal : 123 }, _id : 0 } }",
                "{ $project : { R : '$X', S : '$Y', _id : 0 } }",
                "{ $project : { T : '$R', U : '$S', _id : 0 } }");
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        private class D
        {
            public D(int x)
            {
                X = x;
            }

            public D(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}
