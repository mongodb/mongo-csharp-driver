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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class ResetClassMapConventionTests
    {
        [Fact]
        public void Apply_should_call_reset_on_class_map()
        {
            // Arrange
            var classMap = new BsonClassMap<TestClass>();
            classMap.SetIgnoreExtraElementsIsInherited(true);

            var subject = new ResetClassMapConvention();

            // Act
            subject.Apply(classMap);

            // Assert
            classMap.IgnoreExtraElementsIsInherited.Should().Be(false);
        }

        [Fact]
        public void Apply_should_reset_class_map()
        {
            // Arrange
            var classMap = new BsonClassMap<TestClass>();
            classMap.MapIdProperty(c => c.Id);
            classMap.MapProperty(c => c.Name).SetElementName("custom_name");

            // Initial state verification
            classMap.IdMemberMap.Should().NotBeNull();
            classMap.GetMemberMap(c => c.Name).ElementName.Should().Be("custom_name");

            var subject = new ResetClassMapConvention();

            // Act
            subject.Apply(classMap);

            // Assert
            classMap.IdMemberMap.Should().BeNull();
            classMap.GetMemberMap(c => c.Name).Should().BeNull();
        }

        private class TestClass
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
