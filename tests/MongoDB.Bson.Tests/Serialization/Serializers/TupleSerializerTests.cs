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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class TupleSerializerTests
    {
        [Fact]
        public void Create_should_create_tuple_serializer_with_correct_type()
        {
            // Arrange
            var serializers = new IBsonSerializer[] { new Int32Serializer(), new StringSerializer() };

            // Act
            var result = TupleSerializer.Create(serializers);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType(typeof(TupleSerializer<int, string>));
            var tupleSerializer = (IBsonTupleSerializer)result;
            tupleSerializer.GetItemSerializer(1).Should().BeOfType<Int32Serializer>();
            tupleSerializer.GetItemSerializer(2).Should().BeOfType<StringSerializer>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(9)]
        public void Create_should_throw_for_invalid_number_of_items(int itemCount)
        {
            // Arrange
            IBsonSerializer[] serializers = [.. Enumerable.Range(0, itemCount).Select(_ => new Int32Serializer())];

            // Act
            Action act = () => TupleSerializer.Create(serializers);

            // Assert
            act.ShouldThrow<Exception>().WithMessage("Invalid number of Tuple items : *");
        }

        [Theory]
        [InlineData("Item1", 1, true)]
        [InlineData("Item2", 2, true)]
        [InlineData("Item10", 10, true)]
        [InlineData("Rest", 8, true)]
        [InlineData("NotAnItem", 0, false)]
        [InlineData("Item", 0, false)]
        public void TryParseItemName_should_handle_item_names_correctly(string itemName, int expectedItemNumber, bool expectedResult)
        {
            // Act
            var result = TupleSerializer.TryParseItemName(itemName, out var itemNumber);

            // Assert
            result.Should().Be(expectedResult);
            if (expectedResult)
            {
                itemNumber.Should().Be(expectedItemNumber);
            }
        }

        [Fact]
        public void TupleSerializer_T1_should_throw_when_item1_serializer_is_null()
        {
            // Act
            Action act = () => new TupleSerializer<int>((IBsonSerializer<int>)null);

            // Assert
            act.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("item1Serializer");
        }

        [Fact]
        public void TupleSerializer_T1_should_initialize_item_serializers()
        {
            // Arrange
            var item1Serializer = new Int32Serializer();

            // Act
            var serializer = new TupleSerializer<int>(item1Serializer);

            // Assert
            serializer.Item1Serializer.Should().BeSameAs(item1Serializer);
        }

        [Fact]
        public void TupleSerializer_T1_GetItemSerializer_should_return_correct_serializer()
        {
            // Arrange
            var item1Serializer = new Int32Serializer();
            var serializer = new TupleSerializer<int>(item1Serializer);

            // Act & Assert
            serializer.GetItemSerializer(1).Should().BeSameAs(item1Serializer);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void TupleSerializer_T1_GetItemSerializer_should_throw_for_invalid_item_number(int itemNumber)
        {
            // Arrange
            var serializer = new TupleSerializer<int>(new Int32Serializer());

            // Act
            Action act = () => serializer.GetItemSerializer(itemNumber);

            // Assert
            act.ShouldThrow<IndexOutOfRangeException>()
                .And.Message.Should().Be("itemNumber");
        }
    }
}
