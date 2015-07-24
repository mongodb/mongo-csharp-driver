/* Copyright 2013-2015 MongoDB Inc.
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
using NUnit.Framework;

namespace MongoDB.Driver
{
    [TestFixture]
    public class ReadPreferenceTests
    {
        [Test]
        public void constructor_should_throw_when_mode_is_primary_and_tagSets_is_not_empty()
        {
            var tagSets = new[] { new TagSet(new[] { new Tag("name", "value") }) };

            Action action = () => new ReadPreference(ReadPreferenceMode.Primary, tagSets);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("tagSets");
        }

        [Test]
        public void constructor_should_initialize_instance_when_tagSets_is_null()
        {
            var result = new ReadPreference(ReadPreferenceMode.Secondary, null);

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Secondary);
            result.TagSets.Should().BeEmpty();
        }

        [Test]
        public void constructor_with_mode_should_initialize_instance()
        {
            var mode = ReadPreferenceMode.Secondary; // use a value that is not the default

            var result = new ReadPreference(mode: mode);

            result.ReadPreferenceMode.Should().Be(mode);
            result.TagSets.Should().BeEmpty();
        }

        [Test]
        public void constructor_with_tagSets_should_initialize_instance()
        {
            var mode = ReadPreferenceMode.Secondary; // can't use tagSets with mode Primary
            var tagSets = new[] { new TagSet(new[] { new Tag("name", "value") }) };

            var result = new ReadPreference(mode: mode, tagSets: tagSets);

            result.ReadPreferenceMode.Should().Be(mode);
            result.TagSets.Should().NotBeSameAs(tagSets);
            result.TagSets.Should().Equal(tagSets);
        }

        [Test]
        public void Equals_should_return_false_when_any_field_is_not_equal(
            [Values("mode", "tagSets")]
            string notEqualFieldName)
        {
            var mode = ReadPreferenceMode.Secondary;
            var tagSets = new[] { new TagSet(new[] { new Tag("name", "value1") }) };
            var subject1 = new ReadPreference(mode, tagSets);
            switch (notEqualFieldName)
            {
                case "mode": mode = ReadPreferenceMode.SecondaryPreferred; break;
                case "tagSets": tagSets = new[] { new TagSet(new[] { new Tag("name", "value2") }) }; break;
                default: throw new ArgumentException("notEqualFieldName");
            }
            var subject2 = new ReadPreference(mode, tagSets);

            var result1 = subject1.Equals(subject2);
            var result2 = subject1.Equals((object)subject2);
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            result1.Should().BeFalse();
            result2.Should().BeFalse();
            hashCode1.Should().NotBe(hashCode2);

        }
        [Test]
        public void Equals_should_return_true_when_all_fields_are_equal()
        {
            var mode = ReadPreferenceMode.Secondary;
            var tagSets = new[] { new TagSet(new[] { new Tag("name", "value") }) };
            var subject1 = new ReadPreference(mode, tagSets);
            var subject2 = new ReadPreference(mode, tagSets);

            var result1 = subject1.Equals(subject2);
            var result2 = subject1.Equals((object)subject2);
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            result1.Should().BeTrue();
            result2.Should().BeTrue();
            hashCode1.Should().Be(hashCode2);

        }

        [Test]
        public void Nearest_should_return_expected_result()
        {
            var result = ReadPreference.Nearest;

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Nearest);
            result.TagSets.Count.Should().Be(0);
        }

        [Test]
        public void Primary_should_return_expected_result()
        {
            var result = ReadPreference.Primary;

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Primary);
            result.TagSets.Count.Should().Be(0);
        }

        [Test]
        public void PrimaryPreferred_should_return_expected_result()
        {
            var result = ReadPreference.PrimaryPreferred;

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.PrimaryPreferred);
            result.TagSets.Count.Should().Be(0);
        }

        [Test]
        public void Secondary_should_return_expected_result()
        {
            var result = ReadPreference.Secondary;

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Secondary);
            result.TagSets.Count.Should().Be(0);
        }

        [Test]
        public void SecondaryPreferred_should_return_expected_result()
        {
            var result = ReadPreference.SecondaryPreferred;

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.SecondaryPreferred);
            result.TagSets.Count.Should().Be(0);
        }

        [Test]
        public void With_mode_should_return_expected_result()
        {
            var mode1 = ReadPreferenceMode.Secondary;
            var mode2 = ReadPreferenceMode.SecondaryPreferred;
            var subject = new ReadPreference(mode1);

            var result = subject.With(mode: mode2);

            result.ReadPreferenceMode.Should().Be(mode2);
            result.TagSets.Should().Equal(subject.TagSets);
        }

        [Test]
        public void With_tagSets_should_return_expected_result()
        {
            var tagSets1 = new[] { new TagSet(new[] { new Tag("name", "value1") }) };
            var tagSets2 = new[] { new TagSet(new[] { new Tag("name", "value2") }) };
            var subject = new ReadPreference(ReadPreferenceMode.Secondary, tagSets1);

            var result = subject.With(tagSets: tagSets2);

            result.ReadPreferenceMode.Should().Be(subject.ReadPreferenceMode);
            result.TagSets.Should().Equal(tagSets2);
        }
    }
}
