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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class OrderedEnumerableListWrapperTests
    {
        [Fact]
        public void Constructor_should_initialize_list_and_exception_message()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            var exceptionMessage = "ThenBy is not supported";

            // Act
            var subject = new OrderedEnumerableListWrapper<int>(list, exceptionMessage);

            // Assert
            subject.Should().NotBeNull();
            subject.Should().BeAssignableTo<IOrderedEnumerable<int>>();
        }

        [Fact]
        public void GetEnumerator_should_return_list_enumerator()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            var subject = new OrderedEnumerableListWrapper<int>(list, "ThenBy is not supported");

            // Act
            var enumerator = subject.GetEnumerator();

            // Assert
            enumerator.Should().NotBeNull();
            var enumerated = new List<int>();
            while (enumerator.MoveNext())
            {
                enumerated.Add(enumerator.Current);
            }
            enumerated.Should().Equal(list);
        }

        [Fact]
        public void GetEnumerator_non_generic_should_return_enumerable_enumerator()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            var subject = new OrderedEnumerableListWrapper<int>(list, "ThenBy is not supported");

            // Act
            var enumerator = ((IEnumerable)subject).GetEnumerator();

            // Assert
            enumerator.Should().NotBeNull();
            var enumerated = new List<int>();
            while (enumerator.MoveNext())
            {
                enumerated.Add((int)enumerator.Current);
            }
            enumerated.Should().Equal(list);
        }

        [Fact]
        public void CreateOrderedEnumerable_should_throw_with_configured_message()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            var exceptionMessage = "Custom exception message";
            var subject = new OrderedEnumerableListWrapper<int>(list, exceptionMessage);

            // Act
            Action act = () => subject.CreateOrderedEnumerable(x => x, Comparer<int>.Default, false);
            var exception = Record.Exception(act);

            // Assert
            exception.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Be(exceptionMessage);
        }
        
        [Fact]
        public void Should_be_enumerable_using_foreach()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            var subject = new OrderedEnumerableListWrapper<int>(list, "ThenBy is not supported");
            var enumerated = new List<int>();

            // Act
            foreach (var item in subject)
            {
                enumerated.Add(item);
            }

            // Assert
            enumerated.Should().Equal(list);
        }
    }
}
