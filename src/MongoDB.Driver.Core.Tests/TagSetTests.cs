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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver
{
    public class TagSetTests
    {
        [Fact]
        public void Constructor_should_create_new_tag_list()
        {
            var tags = new[] { new Tag("name", "value") };
            var tagSet = new TagSet(tags);
            tagSet.Tags.Should().NotBeSameAs(tags);
        }

        [Fact]
        public void Constructor_should_throw_if_tags_is_null()
        {
            Action action = () => new TagSet(null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_with_no_arguments_should_create_empty_tag_set()
        {
            var tagSet = new TagSet();
            tagSet.Tags.Count.Should().Be(0);
        }

        [Fact]
        public void Constructor_with_no_tags_should_create_empty_tag_set()
        {
            var tags = new Tag[0];
            var tagSet = new TagSet(tags);
            tagSet.Tags.Count.Should().Be(0);
        }

        [Fact]
        public void Constructor_with_one_tag_should_create_tag_set_with_one_tag()
        {
            var tags = new[] { new Tag("name", "value") };
            var tagSet = new TagSet(tags);
            tagSet.Tags.Should().Equal(tags);
        }

        [Fact]
        public void Constructor_with_two_tag_should_create_tag_set_with_two_tags()
        {
            var tags = new[] { new Tag("name1", "value1"), new Tag("name2", "value2") };
            var tagSet = new TagSet(tags);
            tagSet.Tags.Should().Equal(tags);
        }

        [Fact]
        public void ContainsAll_should_return_false_if_any_required_tag_is_missing()
        {
            var tagSet = new TagSet(new[] { new Tag("name1", "value1"), new Tag("name2", "value2") });
            var required = new TagSet(new[] { new Tag("name1", "value1"), new Tag("name3", "value3") });
            tagSet.ContainsAll(required).Should().BeFalse();
        }

        [Fact]
        public void ContainsAll_should_return_true_if_all_required_tags_are_present()
        {
            var tagSet = new TagSet(new[] { new Tag("name1", "value1"), new Tag("name2", "value2") });
            var required = new TagSet(new[] { new Tag("name1", "value1") });
            tagSet.ContainsAll(required).Should().BeTrue();
        }

        [Fact]
        public void Equals_should_return_false_if_any_field_is_not_the_same()
        {
            var tagSet1 = new TagSet(new[] { new Tag("name1", "value1") });
            var tagSet2 = new TagSet(new[] { new Tag("name2", "value2") });
            tagSet1.Equals(tagSet2).Should().BeFalse();
            tagSet1.Equals((object)tagSet2).Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_false_if_rhs_is_null()
        {
            var tagSet1 = new TagSet(new[] { new Tag("name", "value") });
            tagSet1.Equals((TagSet)null).Should().BeFalse();
            tagSet1.Equals((object)null).Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_false_if_rhs_is_not_a_TagSet()
        {
            var tagSet1 = new TagSet(new[] { new Tag("name", "value") });
            tagSet1.Equals((object)"abc").Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_true_if_all_fields_are_the_same()
        {
            var tagSet1 = new TagSet(new[] { new Tag("name", "value") });
            var tagSet2 = new TagSet(new[] { new Tag("name", "value") });
            tagSet1.Equals(tagSet2).Should().BeTrue();
            tagSet1.Equals((object)tagSet2).Should().BeTrue();
        }
    }
}
