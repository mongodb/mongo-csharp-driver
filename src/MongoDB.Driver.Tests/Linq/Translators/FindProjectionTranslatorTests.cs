/* Copyright 2010-2015 MongoDB Inc.
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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Translators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    public class FindProjectionTranslatorTests
    {
        [Fact]
        public void Should_not_translate_identity()
        {
            var result = Project(p => p, "{ _id: 0, A: \"Jack\", B: \"Awesome\" }");

            result.Projection.Should().BeNull();

            result.Value.A.Should().Be("Jack");
            result.Value.B.Should().Be("Awesome");
        }

        [Fact]
        public void Should_not_translate_when_identity_is_present()
        {
            var result = Project(p => new { P = p, Combined = p.A + " " + p.B }, "{ _id: 0, A: \"Jack\", B: \"Awesome\" }");

            result.Projection.Should().BeNull();

            result.Value.P.A.Should().Be("Jack");
            result.Value.P.B.Should().Be("Awesome");
            result.Value.Combined.Should().Be("Jack Awesome");
        }

        [Fact]
        public void Should_include_id_if_specified()
        {
            var result = Project(p => new { p.Id, p.A }, "{ _id: 1, A: \"Jack\" }");

            result.Projection.Should().Be("{ _id: 1, A: 1 }");

            result.Value.Id.Should().Be(1);
            result.Value.A.Should().Be("Jack");
        }

        [Fact]
        public void Should_translate_a_single_top_level_field()
        {
            var result = Project(p => p.A, "{ A: \"Jack\" }");

            result.Projection.Should().Be("{ A: 1, _id: 0 }");

            result.Value.Should().Be("Jack");
        }

        [Fact]
        public void Should_translate_a_single_top_level_computed_field()
        {
            var result = Project(p => p.A + " " + p.B, "{ A: \"Jack\", B: \"Awesome\" }");

            result.Projection.Should().Be("{ A: 1, B: 1, _id: 0 }");

            result.Value.Should().Be("Jack Awesome");
        }

        [Fact]
        public void Should_translate_a_single_top_level_field_with_an_operation()
        {
            var result = Project(p => p.A.ToLowerInvariant(), "{ A: \"Jack\" }");

            result.Projection.Should().Be("{ A: 1, _id: 0 }");

            result.Value.Should().Be("jack");
        }

        [Fact]
        public void Should_translate_a_new_expression_with_a_single_top_level_field()
        {
            var result = Project(p => new { p.A }, "{ A: \"Jack\" }");

            result.Projection.Should().Be("{ A: 1, _id: 0 }");

            result.Value.A.Should().Be("Jack");
        }

        [Fact]
        public void Should_translate_a_new_expression_with_a_single_top_level_computed_field()
        {
            var result = Project(p => new { FullName = p.A + " " + p.B }, "{ A: \"Jack\", B: \"Awesome\" }");

            result.Projection.Should().Be("{ A: 1, B: 1, _id: 0 }");

            result.Value.FullName.Should().Be("Jack Awesome");
        }

        [Fact]
        public void Should_translate_when_a_top_level_field_is_repeated()
        {
            var result = Project(p => new { FirstName = p.A, FullName = p.A + " " + p.B }, "{ A: \"Jack\", B: \"Awesome\" }");

            result.Projection.Should().Be("{ A: 1, B: 1, _id: 0 }");

            result.Value.FirstName.Should().Be("Jack");
            result.Value.FullName.Should().Be("Jack Awesome");
        }

        [Fact]
        public void Should_translate_with_a_single_nested_field()
        {
            var result = Project(p => p.C.E.F, "{ C: { E: { F: 2 } } }");

            result.Projection.Should().Be("{ \"C.E.F\": 1, _id: 0 }");

            result.Value.Should().Be(2);
        }

        [Fact]
        public void Should_translate_with_a_single_computed_nested_field()
        {
            var result = Project(p => p.C.E.F + 10, "{ C: { E: { F: 2 } } }");

            result.Projection.Should().Be("{ \"C.E.F\": 1, _id: 0 }");

            result.Value.Should().Be(12);
        }

        [Fact]
        public void Should_translate_with_a_hierarchical_redundancy()
        {
            var result = Project(p => new { p.C, F = p.C.E.F }, "{ C: { D: \"CEO\", E: { F: 2 } } }");

            result.Projection.Should().Be("{ \"C\": 1, _id: 0 }");

            result.Value.C.D.Should().Be("CEO");
            result.Value.C.E.F.Should().Be(2);
            result.Value.F.Should().Be(2);
        }

        [Fact]
        public void Should_translate_a_single_top_level_array()
        {
            var result = Project(p => p.G, "{ G: [{ D: \"Uno\", E : { F: 1 } }, { D: \"Dos\", E: { F: 2 } }] }");

            result.Projection.Should().Be("{ \"G\": 1, _id: 0 }");

            result.Value.Count().Should().Be(2);
            result.Value.ElementAt(0).D.Should().Be("Uno");
            result.Value.ElementAt(0).E.F.Should().Be(1);
            result.Value.ElementAt(1).D.Should().Be("Dos");
            result.Value.ElementAt(1).E.F.Should().Be(2);
        }

        [Fact]
        public void Should_translate_through_a_single_top_level_array_using_Select()
        {
            var result = Project(p => p.G.Select(x => x.D), "{ G: [{ D: \"Uno\" }, { D: \"Dos\" }] }");

            result.Projection.Should().Be("{ \"G.D\": 1, _id: 0 }");

            result.Value.Count().Should().Be(2);
            result.Value.ElementAt(0).Should().Be("Uno");
            result.Value.ElementAt(1).Should().Be("Dos");
        }

        [Fact]
        public void Should_translate_through_a_single_top_level_array_using_SelectMany()
        {
            var result = Project(p => p.G.SelectMany(x => x.E.I), "{ G: [{ E: { I: [\"a\", \"b\"] } }, { E: { I: [\"c\", \"d\"] } }] }");

            result.Projection.Should().Be("{ \"G.E.I\": 1, _id: 0 }");

            result.Value.Count().Should().Be(4);
            result.Value.Should().BeEquivalentTo("a", "b", "c", "d");
        }

        [Fact]
        public void Should_translate_through_a_single_top_level_array_to_a_binary_operation()
        {
            var result = Project(p => p.G.Select(x => x.E.F + x.E.H), "{ G: [{ E: { F: 2, H: 3 } }, { E: { F: 6, H: 7 } }] }");

            result.Projection.Should().Be("{ \"G.E.F\": 1, \"G.E.H\": 1, _id: 0 }");

            result.Value.Count().Should().Be(2);
            result.Value.ElementAt(0).Should().Be(5);
            result.Value.ElementAt(1).Should().Be(13);
        }

        [Fact]
        public void Should_translate_through_a_single_top_level_array_when_array_identity_is_present()
        {
            var result = Project(p => new { p.G, Sums = p.G.Select(x => x.E.F + x.E.H) }, "{ G: [{ D: \"Uno\", E: { F: 2, H: 3 } }, { D: \"Dos\", E: { F: 6, H: 7 } }] }");

            result.Projection.Should().Be("{ G: 1, _id: 0 }");

            result.Value.G.ElementAt(0).D.Should().Be("Uno");
            result.Value.G.ElementAt(1).D.Should().Be("Dos");
            result.Value.Sums.Count().Should().Be(2);
            result.Value.Sums.ElementAt(0).Should().Be(5);
            result.Value.Sums.ElementAt(1).Should().Be(13);
        }

        [Fact]
        public void Should_translate_with_a_parent_field_in_a_child_selector()
        {
            var result = Project(p => p.G.Select(x => new { A = p.A, D = x.D }), "{ A: \"Yay\", G: [{ D: \"Uno\" }, { D: \"Dos\" }] }");

            result.Projection.Should().Be("{ \"A\": 1, \"G.D\": 1, _id: 0 }");

            result.Value.ElementAt(0).A.Should().Be("Yay");
            result.Value.ElementAt(0).D.Should().Be("Uno");
            result.Value.ElementAt(1).A.Should().Be("Yay");
            result.Value.ElementAt(1).D.Should().Be("Dos");
        }

        private ProjectedResult<T> Project<T>(Expression<Func<Root, T>> projector, string json)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Root>();
            var projectionInfo = FindProjectionTranslator.Translate<Root, T>(projector, serializer, BsonSerializer.SerializerRegistry);

            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                return new ProjectedResult<T>
                {
                    Projection = projectionInfo.Document,
                    Value = projectionInfo.ProjectionSerializer.Deserialize(context)
                };
            }
        }

        private class ProjectedResult<T>
        {
            public BsonDocument Projection;
            public T Value;
        }

        private class Root
        {
            public int Id { get; set; }

            public string A { get; set; }

            public string B { get; set; }

            public C C { get; set; }

            public IEnumerable<C> G { get; set; }
        }

        public class C
        {
            public string D { get; set; }

            public E E { get; set; }
        }

        public class E
        {
            public int F { get; set; }

            public int H { get; set; }

            public IEnumerable<string> I { get; set; }
        }
    }
}
