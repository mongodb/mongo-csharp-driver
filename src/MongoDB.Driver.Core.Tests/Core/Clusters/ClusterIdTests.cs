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

using System;
using MongoDB.Driver.Core.Clusters;
using FluentAssertions;
using Xunit;


namespace MongoDB.Driver.Core.Clusters
{
    public class ClusterIdTests
    {
        [Fact]
        public void Constructor_should_initialize_instance()
        {
            var subject = new ClusterId(1);
            subject.Value.Should().Be(1);
        }

        [Fact]
        public void Equals_should_return_false_if_any_field_is_not_equal()
        {
            var subject1 = new ClusterId(1);
            var subject2 = new ClusterId(2);
            subject1.Equals(subject2).Should().BeFalse();
            subject1.Equals((object)subject2).Should().BeFalse();
            subject1.GetHashCode().Should().NotBe(subject2.GetHashCode());
        }

        [Fact]
        public void Equals_should_return_true_if_all_fiels_are_equal()
        {
            var subject1 = new ClusterId(1);
            var subject2 = new ClusterId(1);
            subject1.Equals(subject2).Should().BeTrue();
            subject1.Equals((object)subject2).Should().BeTrue();
            subject1.GetHashCode().Should().Be(subject2.GetHashCode());
        }

        [Fact]
        public void ToString_should_return_string_representation()
        {
            var subject = new ClusterId(1);
            subject.ToString().Should().Be("1");
        }
    }
}
