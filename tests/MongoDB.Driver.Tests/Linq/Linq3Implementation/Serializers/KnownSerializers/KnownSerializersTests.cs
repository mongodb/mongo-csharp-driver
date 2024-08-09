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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Serializers.KnownSerializers
{
    public class KnownSerializersTests
    {
        #region static
        static KnownSerializersTests()
        {
            BsonClassMap.RegisterClassMap<C3>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(p => p.Es).SetSerializer(new EnumSerializer<E>(BsonType.String));
            });
        }
        #endregion

        public enum E { A, B };

        private class C1
        {
            public E Ei { get; set; }
        }

        private class C2
        {
            public E Ei { get; set; }
            [BsonRepresentation(BsonType.String)]
            public E Es { get; set; }
        }

        private class C3
        {
            public E Es { get; set; }
        }

        private class Results
        {
            public bool Result { get; set; }
        }

        [Theory]
        [InlineData(E.A, "{ \"Result\" : { \"$eq\" : [ \"$Ei\", 0 ] }, \"_id\" : 0 }")]
        [InlineData(E.B, "{ \"Result\" : { \"$eq\" : [ \"$Ei\", 1 ] }, \"_id\" : 0 }")]
        public void Where_operator_equal_should_render_correctly(E value, string expectedProjection)
        {
            var subject = GetSubject<C1>();

            var queryable = subject.Select(x => new Results { Result = x.Ei == value });

            AssertProjection<C1, Results>(queryable, expectedProjection);
        }

        [Theory]
        [InlineData(E.A, "{ \"Result\" : { \"$eq\" : [ \"$Es\", \"A\" ] }, \"_id\" : 0 }")]
        [InlineData(E.B, "{ \"Result\" : { \"$eq\" : [ \"$Es\", \"B\" ] }, \"_id\" : 0 }")]
        public void Where_operator_equal_should_render_enum_as_string(E value, string expectedProjection)
        {
            var subject = GetSubject<C2>();

            var queryable = subject.Select(x => new Results { Result = x.Es == value });

            AssertProjection<C2, Results>(queryable, expectedProjection);
        }

        [Theory]
        [InlineData(E.A, "{ \"Result\" : { \"$eq\" : [ \"$Es\", \"A\" ] }, \"_id\" : 0 }")]
        [InlineData(E.B, "{ \"Result\" : { \"$eq\" : [ \"$Es\", \"B\" ] }, \"_id\" : 0 }")]
        public void Where_operator_equal_should_render_enum_as_string_when_configured_with_class_map(E value, string expectedProjection)
        {
            var subject = GetSubject<C3>();

            var queryable = subject.Select(x => new Results { Result = x.Es == value });

            AssertProjection<C3, Results>(queryable, expectedProjection);
        }

        [Theory]
        [InlineData(E.A, "{ \"Result\" : { \"$and\" : [{ \"$eq\" : [ \"$Ei\", 0 ] }, { \"$eq\" : [ \"$Es\", \"A\" ] }]}, \"_id\" : 0 }")]
        [InlineData(E.B, "{ \"Result\" : { \"$and\" : [{ \"$eq\" : [ \"$Ei\", 1 ] }, { \"$eq\" : [ \"$Es\", \"B\" ] }]}, \"_id\" : 0 }")]
        public void Where_operator_equal_should_render_string_enum_as_string_and_int32_enum_as_int32(E value, string expectedProjection)
        {
            var subject = GetSubject<C2>();

            var queryable = subject.Select(x => new Results { Result = x.Ei == value && x.Es == value });

            AssertProjection<C2, Results>(queryable, expectedProjection);
        }

        private IQueryable<TDocument> GetSubject<TDocument>()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<TDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            return collection.AsQueryable();
        }

        private void AssertProjection<TDocument, TOutput>(IQueryable<TOutput> queryable, string expectedProjection)
        {
            var stages = Translate<TDocument, TOutput>(queryable);
            stages.Should().HaveCount(1);
            stages[0].Should().Be($"{{ \"$project\" : {expectedProjection} }}");
        }

        private BsonDocument[] Translate<TDocument, TOutput>(IQueryable<TOutput> queryable)
        {
            var provider = (MongoQueryProvider<TDocument>)queryable.Provider;
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TOutput>(provider, queryable.Expression);
            return executableQuery.Pipeline.Stages.Select(s => (BsonDocument)s.Render()).ToArray();
        }
    }
}
