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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4747Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Builder_Set_with_one_int_field_name_and_constant_with_int_representation_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set("X", 2);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { X : 2 } }");

            var result = aggregate.Single();
            result.X.Should().Be(2);
        }

        [Fact]
        public void Builder_Set_with_one_int_field_name_and_constant_with_string_representation_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set("Y", 2);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { Y : '2' } }");

            var result = aggregate.Single();
            result.Y.Should().Be(2);
        }

        [Fact]
        public void Builder_Set_with_one_enum_field_name_and_constant_with_int_representation_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set("E", E.B);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { E : 2 } }");

            var result = aggregate.Single();
            result.E.Should().Be(E.B);
        }

        [Fact]
        public void Builder_Set_with_one_enum_field_name_and_constant_with_string_representation_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set("F", E.B);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { F : 'B' } }");

            var result = aggregate.Single();
            result.F.Should().Be(E.B);
        }

        [Fact]
        public void Builder_Set_with_multiple_int_field_names_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set("X", 2)
                .Set("Y", 3)
                .Set("Z", 4);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { X : 2, Y : '3', Z : 4 } }");

            var result = aggregate.As(BsonDocumentSerializer.Instance).Single();
            result["X"].AsInt32.Should().Be(2);
            result["Y"].AsString.Should().Be("3");
            result["Z"].AsInt32.Should().Be(4);
        }

        [Fact]
        public void Builder_Set_with_multiple_enum_field_names_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set("E", E.B)
                .Set("F", E.B)
                .Set("G", E.B);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { E : 2, F : 'B', G : 2 } }");

            var result = aggregate.As(BsonDocumentSerializer.Instance).Single();
            result["E"].AsInt32.Should().Be(2);
            result["F"].AsString.Should().Be("B");
            result["G"].AsInt32.Should().Be(2);
        }

        [Fact]
        public void Builder_Set_with_duplicate_int_field_names_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set("X", 2)
                .Set("X", 3); // last one wins

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { X : 3 } }");

            var result = aggregate.Single();
            result.X.Should().Be(3);
        }

        [Fact]
        public void Builder_Set_with_one_int_field_expression_and_constant_with_int_representation_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set(x => x.X, 2);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { X : 2 } }");

            var result = aggregate.Single();
            result.X.Should().Be(2);
        }

        [Fact]
        public void Builder_Set_with_one_int_field_expression_and_constant_with_string_representation_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set(x => x.Y, 2);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { Y : '2' } }");

            var result = aggregate.Single();
            result.Y.Should().Be(2);
        }

        [Fact]
        public void Builder_Set_with_one_enum_field_expression_and_constant_with_int_representation_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set(x => x.E, E.B);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { E : 2 } }");

            var result = aggregate.Single();
            result.E.Should().Be(E.B);
        }

        [Fact]
        public void Builder_Set_with_one_enum_field_expression_and_constant_with_string_representation_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set(x => x.F, E.B);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { F : 'B' } }");

            var result = aggregate.Single();
            result.F.Should().Be(E.B);
        }

        [Fact]
        public void Builder_Set_with_multiple_int_field_expressions_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set(x => x.X, 2)
                .Set(x => x.Y, 3)
                .Set("Z", 4);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { X : 2, Y : '3', Z : 4 } }");

            var result = aggregate.As(BsonDocumentSerializer.Instance).Single();
            result["X"].AsInt32.Should().Be(2);
            result["Y"].AsString.Should().Be("3");
            result["Z"].AsInt32.Should().Be(4);
        }

        [Fact]
        public void Builder_Set_with_multiple_enum_field_expressions_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set(x => x.E, E.B)
                .Set(x => x.F, E.B)
                .Set("G", E.B);

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { E : 2, F : 'B', G : 2 } }");

            var result = aggregate.As(BsonDocumentSerializer.Instance).Single();
            result["E"].AsInt32.Should().Be(2);
            result["F"].AsString.Should().Be("B");
            result["G"].AsInt32.Should().Be(2);
        }

        [Fact]
        public void Builder_Set_with_duplicate_int_field_expressions_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();
            var fields = Builders<C>.SetFields
                .Set(x => x.X, 2)
                .Set(x => x.X, 3); // last one wins

            var aggregate = collection.Aggregate()
                .Set(fields);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { X : 3 } }");

            var result = aggregate.Single();
            result.X.Should().Be(3);
        }

        [Fact]
        public void Set_with_new_anonymous_class_with_empty_members_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();

            var aggregate = collection.Aggregate()
                .Set(x => new { });

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { } }");

            var result = aggregate.Single();
            result.X.Should().Be(1);
        }

        [Fact]
        public void Set_with_new_anonymous_class_with_members_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();

            var aggregate = collection.Aggregate()
                .Set(x => new { X = 2, E = E.B, F = x.F, G = E.B, Z = 4 });

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { X : 2, E : 2, F : '$F', G : 2, Z : 4 } }");

            var result = aggregate.As(BsonDocumentSerializer.Instance).Single();
            result["X"].AsInt32.Should().Be(2);
            result["E"].AsInt32.Should().Be(2);
            result["F"].AsString.Should().Be("A");
            result["G"].AsInt32.Should().Be(2);
            result["Z"].AsInt32.Should().Be(4);
        }

        [Fact]
        public void Set_with_new_default_constructor_and_no_member_initializers_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();

            var aggregate = collection.Aggregate()
                .Set(x => new C());

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { } }");

            var result = aggregate.Single();
            result.X.Should().Be(1);
        }

        [Fact]
        public void Set_with_new_default_constructor_and_empty_member_initializers_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();

            var aggregate = collection.Aggregate()
                .Set(x => new C { });

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { } }");

            var result = aggregate.Single();
            result.X.Should().Be(1);
        }

        [Fact]
        public void Set_with_new_default_constructor_and_member_initializers_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();

            var aggregate = collection.Aggregate()
                .Set(x => new C { X = 2, E = E.B, F = x.F });

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { X : 2, E : 2, F : '$F' } }");

            var result = aggregate.Single();
            result.X.Should().Be(2);
            result.E.Should().Be(E.B);
            result.F.Should().Be(E.A);
        }

        [Fact]
        public void Set_with_new_copy_constructor_and_no_member_initializers_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();

            var aggregate = collection.Aggregate()
                .Set(x => new C(x));

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { } }");

            var result = aggregate.Single();
            result.X.Should().Be(1);
        }

        [Fact]
        public void Set_with_new_copy_constructor_and_empty_member_initializers_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();

            var aggregate = collection.Aggregate()
                .Set(x => new C(x) { });

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { } }");

            var result = aggregate.Single();
            result.X.Should().Be(1);
        }

        [Fact]
        public void Set_with_new_copy_constructor_and_member_initializers_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();

            var aggregate = collection.Aggregate()
                .Set(x => new C(x) { X = 2, E = E.B, F = x.F });

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { X : 2, E : 2, F : '$F' } }");

            var result = aggregate.Single();
            result.X.Should().Be(2);
            result.E.Should().Be(E.B);
            result.F.Should().Be(E.A);
        }

        [Fact]
        public void Set_struct_with_new_default_constructor_and_no_member_initializers_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();

            var aggregate = collection.Aggregate()
                .Set(x => new S());

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { } }");

            var result = aggregate.Single();
            result.X.Should().Be(1);
        }

        [Fact]
        public void Set_struct_with_new_default_constructor_and_empty_member_initializers_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();

            var aggregate = collection.Aggregate()
                .Set(x => new S { });

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { } }");

            var result = aggregate.Single();
            result.X.Should().Be(1);
        }

        [Fact]
        public void Set_struct_with_new_default_constructor_and_member_initializers_should_work()
        {
            RequireServer.Check().Supports(Feature.SetStage);
            var collection = GetCollection();

            var aggregate = collection.Aggregate()
                .Set(x => new S { X = 2 });

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $set : { X : 2 } }");

            var result = aggregate.Single();
            result.X.Should().Be(2);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, X = 1, Y = 1, E = E.A, F = E.A });
            return collection;
        }

        private class C
        {
            public C()
            {
            }

            public C(C c)
            {
                Id = c.Id;
                X = c.X;
                Y = c.Y;
            }

            public int Id { get; set; }
            public int X { get; set; }
            [BsonRepresentation(BsonType.String)] public int Y { get; set; }
            public E E { get; set; }
            [BsonRepresentation(BsonType.String)] public E F { get; set; }
        }

        private struct S
        {
            // struct has an implicit default constructor

            public S(S c)
            {
                Id = c.Id;
                X = c.X;
            }

            public int Id { get; set; }
            public int X { get; set; }
        }

        private enum E { A = 1, B = 2 };
    }
}
