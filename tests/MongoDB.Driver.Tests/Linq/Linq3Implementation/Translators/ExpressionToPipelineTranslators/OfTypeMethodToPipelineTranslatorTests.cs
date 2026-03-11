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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    public class OfTypeMethodToPipelineTranslatorTests: LinqIntegrationTest<OfTypeMethodToPipelineTranslatorTests.ClassFixture>
    {
        public OfTypeMethodToPipelineTranslatorTests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void OfType_should_return_expected_results()
        {
            var collection = Fixture.Collection;

            AssertTypeOf<Account>(collection, "{ $match: { _t : 'Account' } }", 1, 2, 3);
            AssertTypeOf<Company>(collection, "{ $match: { _t : 'Company' } }", 1, 2);
            AssertTypeOf<Contact>(collection, "{ $match: { _t : 'Contact' } }", 3);
        }

        private void AssertTypeOf<TDocument>(IMongoCollection<Entity> collection, string expectedStage, params int[] expectedIds)
            where TDocument: Entity
        {
            var queryable = collection
                .AsQueryable()
                .OfType<TDocument>();

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().BeEquivalentTo(expectedIds);
        }

        [BsonDiscriminator(RootClass = true)]
        [BsonKnownTypes(typeof(Account), typeof(Contact), typeof(Company))]
        public abstract class Entity
        {
            public int Id { get; set; }
        }

        public abstract class Account : Entity
        {
        }

        public class Contact : Account
        {
        }

        public class Company : Account
        {
        }

        public sealed class ClassFixture : MongoCollectionFixture<Entity>
        {
            protected override IEnumerable<Entity> InitialData =>
            [
                new Company { Id = 1 },
                new Company { Id = 2 },
                new Contact { Id = 3 }
            ];
        }
    }
}
