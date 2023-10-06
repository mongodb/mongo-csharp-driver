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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4566Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Filter_with_byte_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.Byte == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ Byte : 1 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_char_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.Char == 'a');

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ Char : 97 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_char_with_string_representation_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.CharWithStringRepresentation == 'a');

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ CharWithStringRepresentation : 'a' }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_decimal_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.Decimal == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ Decimal : '1' }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_decimal_with_numeric_representation_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.DecimalWithNumericRepresentation == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ DecimalWithNumericRepresentation : { $numberDecimal : '1' } }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_Decimal128_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.Decimal128 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ Decimal128 : '1' }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_Decimal128_with_numeric_representation_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.Decimal128WithNumericRepresentation == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ Decimal128WithNumericRepresentation : { $numberDecimal : '1' } }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_Double_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.Double == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ Double : 1.0 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_Int16_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.Int16 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ Int16 : 1 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_Int32_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.Int32 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ Int32 : 1 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_Int64_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.Int64 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ Int64 : { $numberLong : 1 } }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_SByte_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.SByte == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ SByte : 1 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_Single_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.Single == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ Single : 1.0 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_Single_compared_to_Double_constant_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.Single == 1.0);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ Single : 1.0 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_UInt16_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.UInt16 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ UInt16 : 1 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_UInt32_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.UInt32 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ UInt32 : 1 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_UInt64_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.UInt64 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ UInt64 : { $numberLong : 1 } }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [InlineData((byte)1, "{ NullableByte : 1 }", 1, LinqProvider.V2)]
        [InlineData((byte)1, "{ NullableByte : 1 }", 1, LinqProvider.V3)]
        [InlineData(null, "{ NullableByte : null }", 3, LinqProvider.V2)]
        [InlineData(null, "{ NullableByte : null }", 3, LinqProvider.V3)]
        public void Filter_with_nullable_byte_should_work(byte? value, string expectedFilter, int expectedId, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableByte == value);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be(expectedFilter);

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(expectedId);
        }

        [Theory]
        [InlineData('a', "{ NullableChar : 97 }", 1, LinqProvider.V2)]
        [InlineData('a', "{ NullableChar : 97 }", 1, LinqProvider.V3)]
        [InlineData(null, "{ NullableChar : null }", 3, LinqProvider.V2)]
        [InlineData(null, "{ NullableChar : null }", 3, LinqProvider.V3)]
        public void Filter_with_nullable_char_should_work(char? value, string expectedFilter, int expectedId, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableChar == value);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be(expectedFilter);

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(expectedId);
        }

        [Theory]
        [InlineData('a', "{ NullableCharWithStringRepresentation : 97 }", new int[0], LinqProvider.V2)] // note: LINQ2 translated is wrong
        [InlineData('a', "{ NullableCharWithStringRepresentation : 'a' }", new[] { 1 }, LinqProvider.V3)]
        [InlineData(null, "{ NullableCharWithStringRepresentation : null }", new[] { 3 }, LinqProvider.V2)]
        [InlineData(null, "{ NullableCharWithStringRepresentation : null }", new[] { 3 }, LinqProvider.V3)]
        public void Filter_with_nullable_char_with_string_representation_should_work(char? value, string expectedFilter, int[] expectedIds, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableCharWithStringRepresentation == value);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be(expectedFilter);

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(expectedIds);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_decimal_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableDecimal == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableDecimal : '1' }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_decimal_with_numeric_representation_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableDecimalWithNumericRepresentation == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableDecimalWithNumericRepresentation : { $numberDecimal : '1' } }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_Decimal128_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableDecimal128 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableDecimal128 : '1' }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_Decimal128_with_numeric_representation_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableDecimal128WithNumericRepresentation == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableDecimal128WithNumericRepresentation : { $numberDecimal : '1' } }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_Double_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableDouble == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableDouble : 1.0 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_Int16_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableInt16 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableInt16 : 1 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_Int32_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableInt32 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableInt32 : 1 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_Int64_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableInt64 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableInt64 : { $numberLong : 1 } }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_SByte_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableSByte == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableSByte : 1 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_Single_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableSingle == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableSingle : 1.0 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_Single_compared_to_Double_constant_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableSingle == 1.0);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableSingle : 1.0 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_UInt16_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableUInt16 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableUInt16 : 1 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_UInt32_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableUInt32 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableUInt32 : 1 }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_with_nullable_UInt64_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var find = collection
                .Find(x => x.NullableUInt64 == 1);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ NullableUInt64 : { $numberLong : 1 } }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_byte_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Byte == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Byte', 1] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Byte', 1] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_char_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Char == 'a');

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Char', 97] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Char', 97] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_char_with_string_representation_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.CharWithStringRepresentation == 'a');

            var stages = Translate(collection, queryable);
            var results = queryable.ToList();
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$CharWithStringRepresentation', 97] }, _id : 0 } }"); // note: LINQ2 translation is wrong
                results.Should().Equal(false, false, false); // note: LINQ2 results are wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$CharWithStringRepresentation', 'a'] }, _id : 0 } }");
                results.Should().Equal(true, false, false);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_decimal_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Decimal == 1);

            var stages = Translate(collection, queryable);
            var results = queryable.ToList();
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Decimal', { $numberDecimal : '1' }] }, _id : 0 } }"); // note: LINQ2 translation is wrong
                results.Should().Equal(false, false, false); // note: LINQ2 results are wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Decimal', '1'] }, _id : 0 } }");
                results.Should().Equal(true, false, false);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_decimal_with_numeric_representation_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.DecimalWithNumericRepresentation == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$DecimalWithNumericRepresentation', { $numberDecimal : '1' }] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$DecimalWithNumericRepresentation', { $numberDecimal : '1' }] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_Decimal128_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Decimal128 == 1);

            var stages = Translate(collection, queryable);
            var results = queryable.ToList();
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Decimal128', { $numberDecimal : '1' }] }, _id : 0 } }"); // note: LINQ2 translation is wrong
                results.Should().Equal(false, false, false); // note: LINQ2 results are wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Decimal128', '1'] }, _id : 0 } }");
                results.Should().Equal(true, false, false);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_Decimal128_with_numeric_representation_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Decimal128WithNumericRepresentation == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Decimal128WithNumericRepresentation', { $numberDecimal : '1' }] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Decimal128WithNumericRepresentation', { $numberDecimal : '1' }] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_Double_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Double == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Double', 1.0] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Double', 1.0] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_Int16_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Int16 == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Int16', 1] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Int16', 1] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_Int32_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Int32 == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Int32', 1] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Int32', 1] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_Int64_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Int64 == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Int64', { $numberLong : 1 }] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Int64', { $numberLong : 1 }] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_SByte_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.SByte == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$SByte', 1] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$SByte', 1] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_Single_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Single == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Single', 1.0] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Single', 1.0] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_Single_compared_to_Double_constant_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Single == 1.0);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Single', 1.0] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Single', 1.0] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_Single_compared_to_Double_field_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.Single == x.Double);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$Single', '$Double'] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$Single', '$Double'] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, true, true);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_UInt16_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.UInt16 == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$UInt16', 1] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$UInt16', 1] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_UInt32_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.UInt32 == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$UInt32', { $numberLong : 1 }] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$UInt32', 1] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_UInt64_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.UInt64 == 1);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$UInt64', { $numberLong : 1 }] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$UInt64', { $numberLong : 1 }] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Theory]
        [InlineData((byte)1, "{ $project : { __fld0 : { $eq : ['$NullableByte', 1] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData((byte)1, "{ $project : { _v : { $eq : ['$NullableByte', 1] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableByte', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableByte', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_byte_should_work(byte? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableByte == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData('a', "{ $project : { __fld0 : { $eq : ['$NullableChar', 97] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData('a', "{ $project : { _v : { $eq : ['$NullableChar', 97] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableChar', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableChar', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_char_should_work(char? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableChar == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData('a', "{ $project : { __fld0 : { $eq : ['$NullableCharWithStringRepresentation', 97] }, _id : 0 } }", new[] { false, false, false }, LinqProvider.V2)] // note: LINQ2 translation is wrong
        [InlineData('a', "{ $project : { _v : { $eq : ['$NullableCharWithStringRepresentation', 'a'] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableCharWithStringRepresentation', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableCharWithStringRepresentation', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_char_with_string_representation_should_work(char? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableCharWithStringRepresentation == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(1, "{ $project : { __fld0 : { $eq : ['$NullableDecimal', { $numberDecimal : '1' }] }, _id : 0 } }", new[] { false, false, false }, LinqProvider.V2)] // note: LINQ2 translation is wrong
        [InlineData(1, "{ $project : { _v : { $eq : ['$NullableDecimal', '1'] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableDecimal', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableDecimal', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_decimal_should_work(int? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableDecimal == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(1, "{ $project : { __fld0 : { $eq : ['$NullableDecimalWithNumericRepresentation', { $numberDecimal : '1' }] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData(1, "{ $project : { _v : { $eq : ['$NullableDecimalWithNumericRepresentation', { $numberDecimal : '1' }] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableDecimalWithNumericRepresentation', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableDecimalWithNumericRepresentation', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_decimal_with_numeric_representation_should_work(int? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableDecimalWithNumericRepresentation == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(1, "{ $project : { __fld0 : { $eq : ['$NullableDecimal128', { $numberDecimal : '1' }] }, _id : 0 } }", new[] { false, false, false }, LinqProvider.V2)] // note: LINQ2 translation is wrong
        [InlineData(1, "{ $project : { _v : { $eq : ['$NullableDecimal128', '1'] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableDecimal128', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableDecimal128', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_Decimal128_should_work(int? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableDecimal128 == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(1, "{ $project : { __fld0 : { $eq : ['$NullableDecimal128WithNumericRepresentation', { $numberDecimal : '1' }] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData(1, "{ $project : { _v : { $eq : ['$NullableDecimal128WithNumericRepresentation', { $numberDecimal : '1' }] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableDecimal128WithNumericRepresentation', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableDecimal128WithNumericRepresentation', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_Decimal128_with_numeric_representation_should_work(int? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableDecimal128WithNumericRepresentation == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData((double)1.0, "{ $project : { __fld0 : { $eq : ['$NullableDouble', 1.0] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData((double)1.0, "{ $project : { _v : { $eq : ['$NullableDouble', 1.0] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableDouble', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableDouble', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_Double_should_work(double? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableDouble == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData((short)1, "{ $project : { __fld0 : { $eq : ['$NullableInt16', 1] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData((short)1, "{ $project : { _v : { $eq : ['$NullableInt16', 1] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableInt16', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableInt16', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_Int16_should_work(short? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableInt16 == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(1, "{ $project : { __fld0 : { $eq : ['$NullableInt32', 1] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData(1, "{ $project : { _v : { $eq : ['$NullableInt32', 1] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableInt32', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableInt32', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_Int32_should_work(int? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableInt32 == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData((long)1, "{ $project : { __fld0 : { $eq : ['$NullableInt64', { $numberLong : 1 }] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData((long)1, "{ $project : { _v : { $eq : ['$NullableInt64', { $numberLong : 1 }] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableInt64', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableInt64', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_Int64_should_work(long? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableInt64 == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData((sbyte)1, "{ $project : { __fld0 : { $eq : ['$NullableSByte', 1] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData((sbyte)1, "{ $project : { _v : { $eq : ['$NullableSByte', 1] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableSByte', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableSByte', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_SByte_should_work(sbyte? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableSByte == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(1.0F, "{ $project : { __fld0 : { $eq : ['$NullableSingle', 1.0] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData(1.0F, "{ $project : { _v : { $eq : ['$NullableSingle', 1.0] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableSingle', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableSingle', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_Single_should_work(float? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableSingle == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(1.0, "{ $project : { __fld0 : { $eq : ['$NullableSingle', 1.0] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData(1.0, "{ $project : { _v : { $eq : ['$NullableSingle', 1.0] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableSingle', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableSingle', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_Single_compared_to_Double_value_should_work(double? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableSingle == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_with_nullable_Single_compared_to_Double_field_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableSingle == x.NullableDouble);

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $eq : ['$NullableSingle', '$NullableDouble'] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $eq : ['$NullableSingle', '$NullableDouble'] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().Equal(true, true, true);
        }

        [Theory]
        [InlineData((ushort)1, "{ $project : { __fld0 : { $eq : ['$NullableUInt16', 1] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData((ushort)1, "{ $project : { _v : { $eq : ['$NullableUInt16', 1] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableUInt16', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableUInt16', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_UInt16_should_work(ushort? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableUInt16 == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(1U, "{ $project : { __fld0 : { $eq : ['$NullableUInt32', { $numberLong : 1 }] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData(1U, "{ $project : { _v : { $eq : ['$NullableUInt32', 1] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableUInt32', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableUInt32', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_UInt32_should_work(uint? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableUInt32 == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(1UL, "{ $project : { __fld0 : { $eq : ['$NullableUInt64', { $numberLong : 1 }] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V2)]
        [InlineData(1UL, "{ $project : { _v : { $eq : ['$NullableUInt64', { $numberLong : 1 }] }, _id : 0 } }", new[] { true, false, false }, LinqProvider.V3)]
        [InlineData(null, "{ $project : { __fld0 : { $eq : ['$NullableUInt64', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V2)]
        [InlineData(null, "{ $project : { _v : { $eq : ['$NullableUInt64', null] }, _id : 0 } }", new[] { false, false, true }, LinqProvider.V3)]
        public void Select_with_nullable_UInt64_should_work(ulong? value, string expectedStage, bool[] expectedResults, LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.NullableUInt64 == value);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        private IMongoCollection<TestObject> CreateCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<TestObject>("test", linqProvider);

            CreateCollection(
                collection,
                new TestObject
                {
                    Id = 1,

                    Byte = 1,
                    Char = 'a',
                    CharWithStringRepresentation = 'a',
                    Decimal = 1M,
                    DecimalWithNumericRepresentation = 1M,
                    Decimal128 = 1M,
                    Decimal128WithNumericRepresentation = 1M,
                    Double = 1.0,
                    Int16 = 1,
                    Int32 = 1,
                    Int64 = 1,
                    SByte = 1,
                    Single = 1,
                    UInt16 = 1,
                    UInt32 = 1,
                    UInt64 = 1,

                    NullableByte = 1,
                    NullableChar = 'a',
                    NullableCharWithStringRepresentation = 'a',
                    NullableDecimal = 1M,
                    NullableDecimalWithNumericRepresentation = 1M,
                    NullableDecimal128 = 1M,
                    NullableDecimal128WithNumericRepresentation = 1M,
                    NullableDouble = 1.0,
                    NullableInt16 = 1,
                    NullableInt32 = 1,
                    NullableInt64 = 1,
                    NullableSByte = 1,
                    NullableSingle = 1,
                    NullableUInt16 = 1,
                    NullableUInt32 = 1,
                    NullableUInt64 = 1,
                },
                new TestObject
                {
                    Id = 2,

                    Byte = 2,
                    Char = 'b',
                    CharWithStringRepresentation = 'b',
                    Decimal = 2M,
                    DecimalWithNumericRepresentation = 2M,
                    Decimal128 = 2M,
                    Decimal128WithNumericRepresentation = 2M,
                    Double = 2.0,
                    Int16 = 2,
                    Int32 = 2,
                    Int64 = 2,
                    SByte = 2,
                    Single = 2,
                    UInt16 = 2,
                    UInt32 = 2,
                    UInt64 = 2,

                    NullableByte = 2,
                    NullableChar = 'b',
                    NullableCharWithStringRepresentation = 'b',
                    NullableDecimal = 2M,
                    NullableDecimalWithNumericRepresentation = 2M,
                    NullableDecimal128 = 2M,
                    NullableDecimal128WithNumericRepresentation = 2M,
                    NullableDouble = 2.0,
                    NullableInt16 = 2,
                    NullableInt32 = 2,
                    NullableInt64 = 2,
                    NullableSByte = 2,
                    NullableSingle = 2,
                    NullableUInt16 = 2,
                    NullableUInt32 = 2,
                    NullableUInt64 = 2,
                },
                new TestObject
                {
                    Id = 3
                });

            return collection;
        }

        private class TestObject
        {
            public int Id { get; set; }

            public byte Byte { get; set; }
            public char Char { get; set; }
            [BsonRepresentation(BsonType.String)] public char CharWithStringRepresentation { get; set; }
            public decimal Decimal { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public decimal DecimalWithNumericRepresentation { get; set; }
            public Decimal128 Decimal128 { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public Decimal128 Decimal128WithNumericRepresentation { get; set; }
            public double Double { get; set; }
            public short Int16 { get; set; }
            public int Int32 { get; set; }
            public long Int64 { get; set; }
            public sbyte SByte { get; set; }
            public float Single { get; set; }
            public ushort UInt16 { get; set; }
            public uint UInt32 { get; set; }
            public ulong UInt64 { get; set; }

            public byte? NullableByte { get; set; }
            public char? NullableChar { get; set; }
            [BsonRepresentation(BsonType.String)] public char? NullableCharWithStringRepresentation { get; set; }
            public decimal? NullableDecimal { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public decimal? NullableDecimalWithNumericRepresentation { get; set; }
            public Decimal128? NullableDecimal128 { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public Decimal128? NullableDecimal128WithNumericRepresentation { get; set; }
            public double? NullableDouble { get; set; }
            public short? NullableInt16 { get; set; }
            public int? NullableInt32 { get; set; }
            public long? NullableInt64 { get; set; }
            public sbyte? NullableSByte { get; set; }
            public float? NullableSingle { get; set; }
            public ushort? NullableUInt16 { get; set; }
            public uint? NullableUInt32 { get; set; }
            public ulong? NullableUInt64 { get; set; }
        }
    }
}
