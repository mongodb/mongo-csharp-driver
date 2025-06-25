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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp2107Tests : LinqIntegrationTest<CSharp2107Tests.ClassFixture>
    {
        public CSharp2107Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Aggregate_Project_should_work()
        {
            var collection = Fixture.Collection;

            var aggregate = collection.Aggregate()
                .Project(doc => new
                {
                    UserIsCustomer = doc.Users.Where(user => user.Identity.IdentityType == IdentityType.Type1)
                });

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $project : { UserIsCustomer : { $filter : { input : '$Users', as : 'user', cond : { $eq : ['$$user.Identity.IdentityType', 'Type1'] } } }, _id : 0 } }");

            var results = aggregate.ToList();
            results.Should().HaveCount(1);
            results[0].UserIsCustomer.Select(u => u.UserId).Should().Equal(1);
        }

        [Fact]
        public void Queryable_Select_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(doc => new
                {
                    UserIsCustomer = doc.Users.Where(user => user.Identity.IdentityType == IdentityType.Type1)
                });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { UserIsCustomer : { $filter : { input : '$Users', as : 'user', cond : { $eq : ['$$user.Identity.IdentityType', 'Type1'] } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].UserIsCustomer.Select(u => u.UserId).Should().Equal(1);
        }

        public class Customer
        {
            public int Id { get; set; }
            public IEnumerable<User> Users { get; set; }
        }

        public class User
        {
            public int UserId { get; set; }
            public Identity Identity { get; set; }
        }

        public class Identity
        {
            [BsonRepresentation(BsonType.String)]
            public IdentityType IdentityType { get; set; }
        }

        public enum IdentityType
        {
            Type1,
            Type2
        }

        public sealed class ClassFixture : MongoCollectionFixture<Customer>
        {
            protected override IEnumerable<Customer> InitialData =>
            [
                new Customer
                {
                    Id = 1,
                    Users = new[]
                    {
                        new User { UserId = 1, Identity = new Identity { IdentityType = IdentityType.Type1 } },
                        new User { UserId = 2, Identity = new Identity { IdentityType = IdentityType.Type2 } }
                    }
                }
            ];
        }
    }
}
