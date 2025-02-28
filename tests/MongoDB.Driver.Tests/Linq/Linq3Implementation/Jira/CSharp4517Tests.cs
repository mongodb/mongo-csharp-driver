﻿/* Copyright 2010-present MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4517Tests : LinqIntegrationTest<CSharp4517Tests.ClassFixture>
    {
        public CSharp4517Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Filter_with_comparison_of_different_types_should_throw()
        {
            var collection = Fixture.Collection;

            var queryable =
                collection.AsQueryable()
                .Where(x => x.Id == 1);

            var exception = Record.Exception(() => Translate(collection, queryable));

            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because operand types are not compatible with each other");
        }

        [Theory]
        [ParameterAttributeData]
        public void Expression_with_comparison_of_different_types_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            RequireServer.Check().Supports(Feature.FindProjectionExpressions);
            var collection = Fixture.Collection;
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable =
                collection.AsQueryable(translationOptions)
                .Select(x => new { R = x.Id == 1 });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, "{ $project : { _snippets : ['$_id'], _id : 0 } }");
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Select(r => r.R).Should().Equal(true);
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("because operand types are not compatible with each other");
            }
        }

        public class MyDocument
        {
            public MyId Id { get; set; }
            public string Name { get; set; }
        }

        [BsonSerializer(typeof(MyIdSerializer))]
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
        public class MyId
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
        {

            public MyId(int id)
            {
                Id = id;
            }

            public int Id { get; }

            public static bool operator ==(int id, MyId other) => id == other.Id;
            public static bool operator ==(MyId id, int other) => id.Id == other;
            public static bool operator !=(int id, MyId other) => !(id == other);
            public static bool operator !=(MyId id, int other) => !(id == other);
        }

        public class MyIdSerializer : SerializerBase<MyId>
        {
            public override MyId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                return new MyId(context.Reader.ReadInt32());
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MyId value)
            {
                context.Writer.WriteInt32(value.Id);
            }
        }

        public sealed class ClassFixture : MongoCollectionFixture<MyDocument>
        {
            protected override IEnumerable<MyDocument> InitialData =>
            [
                new MyDocument { Id = new MyId(1), Name = "abc" }
            ];
        }
    }
}
