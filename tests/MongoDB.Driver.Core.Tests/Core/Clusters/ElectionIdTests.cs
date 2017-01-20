/* Copyright 2013-2016 MongoDB Inc.
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
using Xunit;


namespace MongoDB.Driver.Core.Clusters
{
    public class ElectionIdTests
    {
        [Theory]
        [InlineData("555925bfb69aa7d5be29126b", "555925bfb69aa7d5be29126b", 0)]
        [InlineData("555925bfb69aa7d5be29126b", "555925bfb69aa7d5be29126c", -1)]
        [InlineData("000000000000000000000000", "555925bfb69aa7d5be29126b", -1)]
        [InlineData("555925bfb69aa7d5be29126c", "555925bfb69aa7d5be29126b", 1)]
        [InlineData("555925bfb69aa7d5be29126b", "000000000000000000000000", 1)]
        [InlineData("555925bfb69aa7d5be29126b", null, 1)]
        public void CompareTo_should_return_the_correct_value(string oidA, string oidB, int result)
        {
            var subject1 = new ElectionId(ObjectId.Parse(oidA));
            ElectionId subject2 = oidB == null ? (ElectionId)null : new ElectionId(ObjectId.Parse(oidB));

            subject1.CompareTo(subject2).Should().Be(result);
        }

        [Fact]
        public void Equals_should_return_false_if_any_field_is_not_equal()
        {
            var subject1 = new ElectionId(ObjectId.Empty);
            var subject2 = new ElectionId(ObjectId.GenerateNewId());
            subject1.Equals(subject2).Should().BeFalse();
            subject1.Equals((object)subject2).Should().BeFalse();
            subject1.GetHashCode().Should().NotBe(subject2.GetHashCode());
        }

        [Fact]
        public void Equals_should_return_true_if_all_fiels_are_equal()
        {
            var subject1 = new ElectionId(ObjectId.Empty);
            var subject2 = new ElectionId(ObjectId.Empty);
            subject1.Equals(subject2).Should().BeTrue();
            subject1.Equals((object)subject2).Should().BeTrue();
            subject1.GetHashCode().Should().Be(subject2.GetHashCode());
        }

        [Fact]
        public void ToString_should_return_string_representation()
        {
            var subject = new ElectionId(ObjectId.Empty);
            subject.ToString().Should().Be(ObjectId.Empty.ToString());
        }
    }
}
