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
    public class ResetMemberMapsConventionTests
    {
        [Fact]
        public void Apply_should_call_reset_on_member_map()
        {
            // Arrange
            var classMap = new BsonClassMap<TestClass>();
            classMap.AutoMap();
            var memberMap = classMap.GetMemberMap(c => c.Name);
            memberMap.SetDefaultValue("newDefault");

            var subject = new ResetMemberMapsConvention();

            // Act
            subject.Apply(memberMap);

            // Assert
            memberMap.DefaultValue.Should().Be(null);
        }

        [Fact]
        public void Apply_should_reset_member_map()
        {
            // Arrange
            var classMap = new BsonClassMap<TestClass>();
            var memberMap = classMap.MapProperty(c => c.Name);
            memberMap.SetElementName("custom_name");
            memberMap.SetIgnoreIfNull(true);

            // Initial state verification
            memberMap.ElementName.Should().Be("custom_name");
            memberMap.IgnoreIfNull.Should().BeTrue();

            var subject = new ResetMemberMapsConvention();

            // Act
            subject.Apply(memberMap);

            // Assert
            memberMap.ElementName.Should().Be("Name");
            memberMap.IgnoreIfNull.Should().BeFalse();
        }

        private class TestClass
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
