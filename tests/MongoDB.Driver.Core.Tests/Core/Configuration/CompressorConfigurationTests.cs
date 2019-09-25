/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Configuration
{
    public class CompressorConfigurationTests
    {
        [Theory]
        [InlineData(CompressorType.Noop)]
        [InlineData(CompressorType.Snappy)]
        [InlineData(CompressorType.Zlib)]
        public void constructor_should_initialize_instance(CompressorType type)
        {
            var subject = new CompressorConfiguration(type);

            subject.Type.Should().Be(type);
            subject.Properties.Should().NotBeNull();
            subject.Properties.Count.Should().Be(0);
        }

        [Theory]
        [InlineData("Properties")]
        [InlineData("Type")]
        public void Equals_should_return_false_if_any_field_is_not_equal(string fieldName)
        {
            var type = CompressorType.Snappy;
            var key = "x";
            var subject1 = new CompressorConfiguration(type);
            subject1.Properties.Add(key, 1);
            switch (fieldName)
            {
                case "Properties": key = "y"; break;
                case "Type": type = CompressorType.Zlib; break;
            }
            var subject2 = new CompressorConfiguration(type);
            subject2.Properties.Add(key, 1);

            var result = subject1.Equals(subject2);
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            result.Should().BeFalse();
            hashCode2.Should().NotBe(hashCode1);
        }

        [Fact]
        public void Equals_should_return_false_if_obj_is_null()
        {
            var subject = new CompressorConfiguration(CompressorType.Noop);

            var result = subject.Equals(null);

            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_false_if_obj_is_wrong_type()
        {
            var subject = new CompressorConfiguration(CompressorType.Noop);
            var obj = new object();

            var result = subject.Equals(new object());
            var hashCode1 = subject.GetHashCode();
            var hashCode2 = obj.GetHashCode();

            result.Should().BeFalse();
            hashCode2.Should().NotBe(hashCode1);
        }

        [Theory]
        [InlineData(CompressorType.Noop)]
        [InlineData(CompressorType.Snappy)]
        [InlineData(CompressorType.Zlib)]
        public void Equals_should_return_true_if_all_fields_are_equal(CompressorType type)
        {
            var subject1 = new CompressorConfiguration(type);
            var subject2 = new CompressorConfiguration(type);
            subject1.Properties.Add("x", 1);
            subject2.Properties.Add("x", 1);

            var result = subject1.Equals(subject2);
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            result.Should().BeTrue();
            hashCode2.Should().Be(hashCode1);
        }
    }
}
