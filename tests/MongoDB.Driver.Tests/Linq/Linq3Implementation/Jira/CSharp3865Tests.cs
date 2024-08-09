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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3865Tests
    {
        [Fact]
        public void Expression_using_ToLower_should_work()
        {
            var collection = GetCollection();

            var queryable =
                from product in collection.AsQueryable()
                select new { Result = product.FriendlyName.ToLower() };

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { Result : { $toLower : '$FriendlyName' }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Expression_using_ToLowerInvariant_should_work()
        {
            var collection = GetCollection();

            var queryable =
                from product in collection.AsQueryable()
                select new { Result = product.FriendlyName.ToLowerInvariant() };

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { Result : { $toLower : '$FriendlyName' }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Expression_using_ToUpper_should_work()
        {
            var collection = GetCollection();

            var queryable =
                from product in collection.AsQueryable()
                select new { Result = product.FriendlyName.ToUpper() };

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { Result : { $toUpper : '$FriendlyName' }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Expression_using_ToUpperInvariant_should_work()
        {
            var collection = GetCollection();

            var queryable =
                from product in collection.AsQueryable()
                select new { Result = product.FriendlyName.ToUpperInvariant() };

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { Result : { $toUpper : '$FriendlyName' }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Filter_using_ToLower_with_equality_operator_should_work()
        {
            var collection = GetCollection();
            var friendlyName = "Widget";

            var queryable =
                from product in collection.AsQueryable()
                where product.FriendlyName.ToLower() == friendlyName.ToLower()
                select product;

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { FriendlyName : /^widget$/is } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Filter_using_ToLower_with_Equals_should_work()
        {
            var collection = GetCollection();
            var friendlyName = "Widget";

            var queryable =
                from product in collection.AsQueryable()
                where product.FriendlyName.ToLower().Equals(friendlyName.ToLower())
                select product;

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { FriendlyName : /^widget$/is } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Filter_using_ToLowerInvariant_with_equality_operator_should_work()
        {
            var collection = GetCollection();
            var friendlyName = "Widget";

            var queryable =
                from product in collection.AsQueryable()
                where product.FriendlyName.ToLowerInvariant() == friendlyName.ToLower()
                select product;

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { FriendlyName : /^widget$/is } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Filter_using_ToLowerInvariant_with_Equals_should_work()
        {
            var collection = GetCollection();
            var friendlyName = "Widget";

            var queryable =
                from product in collection.AsQueryable()
                where product.FriendlyName.ToLowerInvariant().Equals(friendlyName.ToLower())
                select product;

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { FriendlyName : /^widget$/is } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Filter_using_ToUpper_with_equality_operator_should_work()
        {
            var collection = GetCollection();
            var friendlyName = "Widget";

            var queryable =
                from product in collection.AsQueryable()
                where product.FriendlyName.ToUpper() == friendlyName.ToUpper()
                select product;

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { FriendlyName : /^WIDGET$/is } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Filter_using_ToUpper_with_Equals_should_work()
        {
            var collection = GetCollection();
            var friendlyName = "Widget";

            var queryable =
                from product in collection.AsQueryable()
                where product.FriendlyName.ToUpper().Equals(friendlyName.ToUpper())
                select product;

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { FriendlyName : /^WIDGET$/is } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Filter_using_ToUpperInvariant_with_equality_operator_should_work()
        {
            var collection = GetCollection();
            var friendlyName = "Widget";

            var queryable =
                from product in collection.AsQueryable()
                where product.FriendlyName.ToUpperInvariant() == friendlyName.ToUpper()
                select product;

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { FriendlyName : /^WIDGET$/is } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void ToUpperInvariant_with_Equals_should_work()
        {
            var collection = GetCollection();
            var friendlyName = "Widget";

            var queryable =
                from product in collection.AsQueryable()
                where product.FriendlyName.ToUpperInvariant().Equals(friendlyName.ToUpper())
                select product;

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { FriendlyName : /^WIDGET$/is } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        private IMongoCollection<Product> GetCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            return database.GetCollection<Product>(DriverTestConfiguration.CollectionNamespace.CollectionName);
        }

        private class Product
        {
            public int Id { get; set; }
            public string FriendlyName { get; set; }
        }
    }
}
