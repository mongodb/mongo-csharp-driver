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

using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class RenderDollarFormTests
    {
        [Theory]
        [MemberData(nameof(Correct_form_should_be_rendered_test_cases))]
        public void Correct_form_should_be_rendered(FilterDefinition<BsonDocument> filterDefinition, bool renderDollarForm, string expectedFilter)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<BsonDocument>();
            var expectedFilterDocument = BsonDocument.Parse(expectedFilter);

            var actualFilter = filterDefinition.Render(new(documentSerializer, serializerRegistry, renderDollarForm: renderDollarForm));

            actualFilter.Should().Be(expectedFilterDocument);
        }

        public static IEnumerable<object[]> Correct_form_should_be_rendered_test_cases()
        {
            return new object[][]
            {
                // $eq
                new object[] { Eq("a", 1), false, "{ a : 1 }" },
                new object[] { Eq("a", 1), true, "{ a: { $eq: 1 } }" },

                // $and
                new object[] { Gt("a", 1) & Gt("b", 2), false, "{ a : { $gt : 1 }, b : { $gt : 2 } }" },
                new object[] { Gt("a", 1) & Gt("b", 2), true, "{ $and: [ { a : { $gt : 1 }}, { b : { $gt : 2 }} ] }" },
                new object[] { Gt("a", 1) & Gt("b", 2) & Gt("c", 3), false, "{ a : { $gt : 1 }, b : { $gt : 2 }, c : { $gt : 3 } }" },
                new object[] { Gt("a", 1) & Gt("b", 2) & Gt("c", 3), true, "{ $and: [ { a : { $gt : 1 }}, { b : { $gt : 2 }}, { c : { $gt : 3 }} ] }" },

                // nested $eq
                new object[] { Eq("a", 1) | Eq("b", 2), false, "{ $or: [{ a : 1 }, { b : 2 }] }" },
                new object[] { Eq("a", 1) | Eq("b", 2), true, "{ $or: [{ a : { $eq: 1 }}, { b : { $eq : 2 }}] }" },
                new object[] { !(Eq("a", 1) | Eq("b", 2)), false, "{ $nor: [ { a : 1 }, { b : 2 }] }" },
                new object[] { !(Eq("a", 1) | Eq("b", 2)), true, "{ $nor: [{ a : { $eq: 1 }}, { b : { $eq: 2 }}] }" },

                // nested $and
                new object[] { Gt("a", 1) & Gt("b", 2) | Gt("c", 3) & Gt("d", 4), false, "{ $or: [{ a : { $gt : 1 }, b : { $gt : 2 }}, { c : { $gt : 3 }, d : { $gt : 4 }}] }" },
                new object[] { Gt("a", 1) & Gt("b", 2) | Gt("c", 3) & Gt("d", 4), true, "{ $or: [{ $and: [ { a : { $gt : 1 }}, { b : { $gt : 2 }} ] }, { $and: [ { c : { $gt : 3 }}, { d : { $gt : 4 }} ] }] }" },

                // $eq and $and
                new object[] { Eq("a", 1) & Eq("b", 2), false, "{ a : 1, b : 2 }" },
                new object[] { Eq("a", 1) & Eq("b", 2), true, "{ $and: [{ a: { $eq: 1 }}, { b: { $eq: 2 }}] }" },

                // $eq and $and nested
                new object[] { !(Eq("a", 1) & Eq("b", 2)), false, "{ $nor: [{ a : 1, b : 2 }] }" },
                new object[] { !(Eq("a", 1) & Eq("b", 2)), true, "{ $nor: [{ $and: [{ a: { $eq: 1 }}, { b: { $eq: 2 }}] }] }" },

                // always dollar form
                new object[] { Eq("a", 1) & Eq("a", 2), false, "{ $and : [{ a : 1 }, { a : 2 }] }" },
                new object[] { Eq("a", 1) & Eq("a", 2), true, "{ $and : [{ a : { $eq: 1 } }, { a : { $eq: 2 } }] }" },
                new object[] { Gt("a", 1) & Gt("a", 2), false, "{ $and : [{ a : { $gt : 1 } }, { a : { $gt : 2 } }] }" },
                new object[] { Gt("a", 1) & Gt("a", 2), true, "{ $and : [{ a : { $gt : 1 } }, { a : { $gt : 2 } }] }" },
            };
        }

        private static FilterDefinition<BsonDocument> Eq(string field, int value) => GetBuilder().Eq(field, value);
        private static FilterDefinition<BsonDocument> Gt(string field, int value) => GetBuilder().Gt(field, value);

        private static FilterDefinitionBuilder<BsonDocument> GetBuilder() => new();
    }
}
