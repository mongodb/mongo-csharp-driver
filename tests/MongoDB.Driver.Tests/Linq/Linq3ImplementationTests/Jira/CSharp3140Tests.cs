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

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3140Tests : Linq3IntegrationTest
    {
        [Fact]
        public void AndAlso_with_first_clause_that_evaluates_to_true_should_simplify_to_second_clause()
        {
            var collection = GetCollection<Order>();
            var currentUser = new User { Id = 1, Factory = new Factory { Id = 1 } };
            var queryable = collection.AsQueryable()
                .Where(x => currentUser.Factory != null && x.FactoryId == currentUser.Factory.Id);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { FactoryId : 1 } }");
        }

        [Fact]
        public void AndAlso_with_first_clause_that_evaluates_to_false_should_simplify_to_false_and_should_not_evaluate_second_clause()
        {
            var collection = GetCollection<Order>();
            var currentUser = new User { Id = 1, Factory = null };
            var queryable = collection.AsQueryable()
                .Where(x => currentUser.Factory != null && x.FactoryId == currentUser.Factory.Id);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _id : { $type : -1 } } }");
        }

        [Fact]
        public void AndAlso_with_second_clause_that_evaluates_to_true_should_simplify_to_first_cluase()
        {
            var collection = GetCollection<Order>();
            var currentUser = new User { Id = 1, Factory = new Factory { Id = 1 } };
            var queryable = collection.AsQueryable()
                .Where(x => x.FactoryId == currentUser.Factory.Id && currentUser.Factory != null);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { FactoryId : 1 } }");
        }

        [Fact]
        public void AndAlso_with_second_clause_that_evaluates_to_false_should_simplify_to_false()
        {
            var collection = GetCollection<Order>();
            var currentUser = new User { Id = 1, Factory = null };
            var queryable = collection.AsQueryable()
                .Where(x => x.FactoryId != 0 && currentUser.Factory != null);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _id : { $type : -1 } } }");
        }

        [Fact]
        public void AndAlso_with_neither_clause_a_constant_should_work()
        {
            var collection = GetCollection<Order>();
            var queryable = collection.AsQueryable()
                .Where(x => x.FactoryId != 0 && x.FactoryId != 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $and : [{ FactoryId : { $ne : 0 } }, { FactoryId : { $ne : 1 } }] } }");
        }

        [Fact]
        public void Conditional_with_test_that_evaluates_to_true_should_simplify_to_if_true_clause()
        {
            var collection = GetCollection<Order>();
            var currentUser = new User { Id = 1, Factory = null };
            var queryable = collection.AsQueryable()
                .Where(x => x.FactoryId == (currentUser.Factory == null ? 0 : currentUser.Factory.Id));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { FactoryId : 0 } }");
        }

        [Fact]
        public void Conditional_with_test_that_evaluates_to_false_should_simplify_to_if_true_clause()
        {
            var collection = GetCollection<Order>();
            var currentUser = new User { Id = 1, Factory = new Factory { Id = 1 } };
            var queryable = collection.AsQueryable()
                .Where(x => x.FactoryId == (currentUser.Factory == null ? 0 : currentUser.Factory.Id));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { FactoryId : 1 } }");
        }

        [Fact]
        public void Conditional_with_test_that_is_not_a_constant_should_work()
        {
            var collection = GetCollection<Order>();
            var queryable = collection.AsQueryable()
                .Where(x => x.FactoryId == (x.Id == 0 ? 0 : 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $expr : { $eq : ['$FactoryId', { $cond : { if : { $eq : ['$_id', 0] }, then : 0, else : 1 } }] } } }");
        }

        [Fact]
        public void OrElse_with_first_clause_that_evaluates_to_false_should_simplify_to_second_clause()
        {
            var collection = GetCollection<Order>();
            var currentUser = new User { Id = 1, Factory = new Factory { Id = 1 } };
            var queryable = collection.AsQueryable()
                .Where(x => currentUser.Factory == null || x.FactoryId == currentUser.Factory.Id);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { FactoryId : 1 } }");
        }

        [Fact]
        public void OrElse_with_first_clause_that_evaluates_to_true_should_simplify_to_true_and_should_not_evaluate_second_clause()
        {
            var collection = GetCollection<Order>();
            var currentUser = new User { Id = 1, Factory = null };
            var queryable = collection.AsQueryable()
                .Where(x => currentUser.Factory == null || x.FactoryId == currentUser.Factory.Id);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { } }");
        }

        [Fact]
        public void OrElse_with_second_clause_that_evaluates_to_false_should_simplify_to_first_cluase()
        {
            var collection = GetCollection<Order>();
            var currentUser = new User { Id = 1, Factory = new Factory { Id = 1 } };
            var queryable = collection.AsQueryable()
                .Where(x => x.FactoryId == currentUser.Factory.Id || currentUser.Factory == null);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { FactoryId : 1 } }");
        }

        [Fact]
        public void OrElse_with_second_clause_that_evaluates_to_true_should_simplify_to_true()
        {
            var collection = GetCollection<Order>();
            var currentUser = new User { Id = 1, Factory = null };
            var queryable = collection.AsQueryable()
                .Where(x => x.FactoryId != 0 || currentUser.Factory == null);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { } }");
        }

        [Fact]
        public void OrElse_with_neither_clause_a_constant_should_work()
        {
            var collection = GetCollection<Order>();
            var queryable = collection.AsQueryable()
                .Where(x => x.FactoryId == 0 || x.FactoryId == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $or : [{ FactoryId : 0 }, { FactoryId : 1 }] } }");
        }

        public class Order
        {
            public int Id { get; set; }
            public int FactoryId { get; set; }
        }

        public class User
        {
            public int Id { get; set; }
            public Factory Factory { get; set; }
        }

        public class Factory
        {
            public int Id { get; set; }
        }
    }
}
