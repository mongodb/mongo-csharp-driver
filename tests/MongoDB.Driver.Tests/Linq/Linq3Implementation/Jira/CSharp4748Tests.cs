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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4748Tests : LinqIntegrationTest<CSharp4748Tests.CollectionFixture>
    {
        static CSharp4748Tests()
        {
            BsonClassMap.RegisterClassMap<Translation>(doc =>
            {
                doc.AutoMap();
                doc.MapMember(a => a.Languages)
                    .SetSerializer(
                        new DictionaryInterfaceImplementerSerializer<Dictionary<LanguageEnum, string>>(
                            DictionaryRepresentation.Document,
                            new EnumSerializer<LanguageEnum>(BsonType.String),
                            new StringSerializer()
                    ));
            });
        }

        public CSharp4748Tests(ITestOutputHelper testOutputHelper, CollectionFixture fixture)
            : base(testOutputHelper, fixture)
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_with_ContainsKey_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = Fixture.GetCollection(linqProvider);
            var language = LanguageEnum.en;

            var queryable = collection.AsQueryable()
                .Where(x => x.Languages.ContainsKey(language));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Languages.en' : { $exists : true } } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_ContainsKey_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = Fixture.GetCollection(linqProvider);
            var language = LanguageEnum.en;

            var queryable = collection.AsQueryable()
                .Select(x => x.Languages.ContainsKey(language));

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $ne : [{ $type : '$Languages.en' }, 'missing'] }, _id : 0 } }");

                var results = queryable.ToList();
                results.Should().Equal(true, false);
            }
        }

        public class Translation
        {
            public int Id { get; set; }
            public Dictionary<LanguageEnum, string> Languages { get; set; } = new();
        }

        public enum LanguageEnum
        {
            en,
            nl,
            fr,
            de,
            pl,
        }

        public class CollectionFixture : TemporaryCollectionFixture<Translation>
        {
            protected override IEnumerable<Translation> GetInitialData()
                => new[]
                {
                    new Translation { Id = 1, Languages = new Dictionary<LanguageEnum, string> { { LanguageEnum.en, "English" } } },
                    new Translation { Id = 2, Languages = new Dictionary<LanguageEnum, string> { { LanguageEnum.fr, "French" } } }
                };
        }
    }
}
