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
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class FieldDefinitionTests
    {
        [Fact]
        public void Should_resolve_from_a_non_IBsonDocumentSerializer()
        {
            var subject = new StringFieldDefinition<string, string>("test");

            var renderedField = subject.Render(new StringSerializer(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("test");
            renderedField.UnderlyingSerializer.Should().BeNull();
            renderedField.FieldSerializer.Should().BeNull();
            renderedField.ValueSerializer.Should().BeOfType<StringSerializer>();
        }

        [Fact]
        public void Should_resolve_from_a_BsonDocumentSerializer()
        {
            var subject = new StringFieldDefinition<BsonDocument, BsonValue>("test");

            var renderedField = subject.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("test");
            renderedField.UnderlyingSerializer.Should().BeNull();
            renderedField.FieldSerializer.Should().BeNull();
            renderedField.ValueSerializer.Should().BeOfType<BsonValueSerializer>();
        }

        [Fact]
        public void Should_resolve_from_a_BsonDocumentSerializer_with_dots()
        {
            var subject = new StringFieldDefinition<BsonDocument, BsonValue>("test.one.two.three");

            var renderedField = subject.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("test.one.two.three");
            renderedField.UnderlyingSerializer.Should().BeNull();
            renderedField.FieldSerializer.Should().BeNull();
            renderedField.ValueSerializer.Should().BeOfType<BsonValueSerializer>();
        }

        [Fact]
        public void Should_resolve_top_level_field()
        {
            var subject = new StringFieldDefinition<Person, Name>("Name");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("name");
            renderedField.UnderlyingSerializer.Should().BeOfType<BsonClassMapSerializer<Name>>();
            renderedField.FieldSerializer.Should().BeSameAs(renderedField.UnderlyingSerializer);
            renderedField.ValueSerializer.Should().BeSameAs(renderedField.FieldSerializer);
        }

        [Fact]
        public void Should_resolve_a_nested_field()
        {
            var subject = new StringFieldDefinition<Person, string>("Name.First");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("name.fn");
            renderedField.UnderlyingSerializer.Should().BeOfType<StringSerializer>();
            renderedField.FieldSerializer.Should().BeSameAs(renderedField.UnderlyingSerializer);
            renderedField.ValueSerializer.Should().BeSameAs(renderedField.FieldSerializer);
        }

        [Fact]
        public void Should_resolve_a_nested_field_that_does_not_exist()
        {
            var subject = new StringFieldDefinition<Person, string>("Name.NoExisty");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("name.NoExisty");
            renderedField.UnderlyingSerializer.Should().BeNull();
            renderedField.FieldSerializer.Should().BeNull();
            renderedField.ValueSerializer.Should().BeOfType<StringSerializer>();
        }

        [Fact]
        public void Should_resolve_array_name()
        {
            var subject = new StringFieldDefinition<Person, string>("Pets.Type");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("pets.type");
            renderedField.UnderlyingSerializer.Should().BeOfType<StringSerializer>();
            renderedField.FieldSerializer.Should().BeSameAs(renderedField.UnderlyingSerializer);
            renderedField.ValueSerializer.Should().BeSameAs(renderedField.FieldSerializer);
        }

        [Fact]
        public void Should_resolve_array_name_with_multiple_dots()
        {
            var subject = new StringFieldDefinition<Person, string>("Pets.Name.First");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("pets.name.fn");
            renderedField.UnderlyingSerializer.Should().BeOfType<StringSerializer>();
            renderedField.FieldSerializer.Should().BeSameAs(renderedField.UnderlyingSerializer);
            renderedField.ValueSerializer.Should().BeSameAs(renderedField.FieldSerializer);
        }

        [Fact]
        public void Should_resolve_array_name_with_single_digit_indexer()
        {
            var subject = new StringFieldDefinition<Person, string>("Pets.3.Type");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("pets.3.type");
            renderedField.UnderlyingSerializer.Should().BeOfType<StringSerializer>();
            renderedField.FieldSerializer.Should().BeSameAs(renderedField.UnderlyingSerializer);
            renderedField.ValueSerializer.Should().BeSameAs(renderedField.FieldSerializer);
        }

        [Fact]
        public void Should_resolve_array_name_with_a_multi_digit_indexer()
        {
            var subject = new StringFieldDefinition<Person, string>("Pets.42.Type");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("pets.42.type");
            renderedField.UnderlyingSerializer.Should().BeOfType<StringSerializer>();
            renderedField.FieldSerializer.Should().BeSameAs(renderedField.UnderlyingSerializer);
            renderedField.ValueSerializer.Should().BeSameAs(renderedField.FieldSerializer);
        }

        [Fact]
        public void Should_resolve_array_name_with_positional_operator()
        {
            var subject = new StringFieldDefinition<Person, string>("Pets.$.Type");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("pets.$.type");
            renderedField.UnderlyingSerializer.Should().BeOfType<StringSerializer>();
            renderedField.FieldSerializer.Should().BeSameAs(renderedField.UnderlyingSerializer);
            renderedField.ValueSerializer.Should().BeSameAs(renderedField.FieldSerializer);
        }

        [Fact]
        public void Should_resolve_array_name_with_positional_operator_with_multiple_dots()
        {
            var subject = new StringFieldDefinition<Person, string>("Pets.$.Name.Last");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("pets.$.name.ln");
            renderedField.UnderlyingSerializer.Should().BeOfType<StringSerializer>();
            renderedField.FieldSerializer.Should().BeSameAs(renderedField.UnderlyingSerializer);
            renderedField.ValueSerializer.Should().BeSameAs(renderedField.FieldSerializer);
        }

        [Fact]
        public void Should_resolve_an_enum_with_field_type()
        {
            var subject = new ExpressionFieldDefinition<Person, Gender>(x => x.Gender);

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("g");
            renderedField.UnderlyingSerializer.Should().BeOfType<EnumSerializer<Gender>>();
            renderedField.FieldSerializer.Should().BeSameAs(renderedField.UnderlyingSerializer);
            renderedField.ValueSerializer.Should().BeSameAs(renderedField.FieldSerializer);
        }

        [Fact]
        public void Should_resolve_an_enum_without_field_type()
        {
            Expression<Func<Person, object>> exp = x => x.Gender;
            var subject = new ExpressionFieldDefinition<Person>(exp);

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("g");
            renderedField.FieldSerializer.Should().BeOfType<EnumSerializer<Gender>>();
        }

        [Fact]
        public void Should_assign_a_non_typed_field_definition_from_a_typed_field_definition()
        {
            FieldDefinition<Person, Gender> subject = new ExpressionFieldDefinition<Person, Gender>(x => x.Gender);
            FieldDefinition<Person> subject2 = subject;

            var renderedField = subject2.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("g");
            renderedField.FieldSerializer.Should().BeOfType<EnumSerializer<Gender>>();
        }

        [Fact]
        public void Should_resolve_an_array_field_with_field_lambda()
        {
            FieldDefinition<Person, IEnumerable<Gender>> subject = new ExpressionFieldDefinition<Person, IEnumerable<Gender>>(x => x.Genders);

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("gs");
            renderedField.UnderlyingSerializer.Should().BeOfType<ImpliedImplementationInterfaceSerializer<IEnumerable<Gender>, List<Gender>>>();
            renderedField.FieldSerializer.Should().BeSameAs(renderedField.UnderlyingSerializer);
            renderedField.ValueSerializer.Should().BeSameAs(renderedField.FieldSerializer);

        }

        [Fact]
        public void Should_resolve_an_array_field_with_field_name()
        {
            FieldDefinition<Person, IEnumerable<Gender>> subject = new StringFieldDefinition<Person, IEnumerable<Gender>>("Genders");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry);

            renderedField.FieldName.Should().Be("gs");
            renderedField.UnderlyingSerializer.Should().BeOfType<ImpliedImplementationInterfaceSerializer<IEnumerable<Gender>, List<Gender>>>();
            renderedField.FieldSerializer.Should().BeSameAs(renderedField.UnderlyingSerializer);
            renderedField.ValueSerializer.Should().BeSameAs(renderedField.FieldSerializer);

        }

        [Fact]
        public void Should_resolve_an_array_field_with_field_name_and_scalar_value_and_scalar_value_is_allowed()
        {
            FieldDefinition<Person, Gender> subject = new StringFieldDefinition<Person, Gender>("Genders");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry, allowScalarValueForArrayField: true);

            renderedField.FieldName.Should().Be("gs");
            renderedField.UnderlyingSerializer.Should().BeOfType<ImpliedImplementationInterfaceSerializer<IEnumerable<Gender>, List<Gender>>>();
            renderedField.FieldSerializer.Should().BeNull();
            renderedField.ValueSerializer.Should().BeOfType<EnumSerializer<Gender>>();

        }

        [Fact]
        public void Should_resolve_an_array_field_with_field_name_and_scalar_value_and_scalar_value_is_not_allowed()
        {
            FieldDefinition<Person, Gender> subject = new StringFieldDefinition<Person, Gender>("Genders");

            var renderedField = subject.Render(BsonSerializer.SerializerRegistry.GetSerializer<Person>(), BsonSerializer.SerializerRegistry, allowScalarValueForArrayField: false);

            renderedField.FieldName.Should().Be("gs");
            renderedField.UnderlyingSerializer.Should().BeOfType<ImpliedImplementationInterfaceSerializer<IEnumerable<Gender>, List<Gender>>>();
            renderedField.FieldSerializer.Should().BeNull();
            renderedField.ValueSerializer.ValueType.Should().Be(typeof(Gender));
            renderedField.ValueSerializer.GetType().Name.Should().Be("ConvertIfPossibleSerializer`2");

        }

        private class Person
        {
            [BsonElement("name")]
            public Name Name { get; set; }

            [BsonElement("pets")]
            public IEnumerable<Pet> Pets { get; set; }

            [BsonElement("g")]
            public Gender Gender { get; set; }

            [BsonElement("gs")]
            public IEnumerable<Gender> Genders { get; set; }
        }

        private class Name
        {
            [BsonElement("fn")]
            public string First { get; set; }
            [BsonElement("ln")]
            public string Last { get; set; }
        }

        private class Pet
        {
            [BsonElement("type")]
            public string Type { get; set; }

            [BsonElement("name")]
            public Name Name { get; set; }
        }

        private enum Gender
        {
            Male,
            Female
        }
    }
}
