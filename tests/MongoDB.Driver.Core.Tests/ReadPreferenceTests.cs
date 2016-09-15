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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver
{
    public class ReadPreferenceTests
    {
        [Fact]
        public void constructor_should_throw_when_tagSets_is_not_empty_and_mode_is_primary()
        {
            var tagSets = new[] { new TagSet(new[] { new Tag("name", "value") }) };

            var exception = Record.Exception(() => new ReadPreference(ReadPreferenceMode.Primary, tagSets: tagSets));

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("tagSets");
        }

        [Fact]
        public void constructor_should_throw_when_maxStaleness_is_invalid()
        {
            var exception = Record.Exception(() => new ReadPreference(ReadPreferenceMode.Secondary, maxStaleness: TimeSpan.FromSeconds(-1)));

            var argumentOutOfRangeException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            argumentOutOfRangeException.ParamName.Should().Be("maxStaleness");
        }

        [Fact]
        public void constructor_should_throw_when_maxStaleness_is_not_null_and_mode_is_primary()
        {
            var exception = Record.Exception(() => new ReadPreference(ReadPreferenceMode.Primary, maxStaleness: TimeSpan.FromSeconds(1)));

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("maxStaleness");
        }

        [Fact]
        public void constructor_with_mode_should_initialize_instance()
        {
            var mode = ReadPreferenceMode.Secondary; // use a value that is not the default

            var result = new ReadPreference(mode);

            result.ReadPreferenceMode.Should().Be(mode);
            result.TagSets.Should().BeEmpty();
            result.MaxStaleness.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_with_tagSets_should_initialize_instance()
        {
            var mode = ReadPreferenceMode.Secondary; // can't use tagSets with mode Primary
            var tagSets = new[] { new TagSet(new[] { new Tag("name", "value") }) };

            var result = new ReadPreference(mode, tagSets: tagSets);

            result.ReadPreferenceMode.Should().Be(mode);
            result.TagSets.Should().NotBeSameAs(tagSets);
            result.TagSets.Should().Equal(tagSets);
            result.MaxStaleness.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_with_tagSets_should_initialize_instance_when_tagSets_is_null()
        {
            var result = new ReadPreference(ReadPreferenceMode.Secondary, tagSets: null);

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Secondary);
            result.TagSets.Should().BeEmpty();
            result.MaxStaleness.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_with_maxStaleness_should_initialize_instance()
        {
            var maxStaleness = TimeSpan.FromSeconds(123);
            var result = new ReadPreference(ReadPreferenceMode.Secondary, maxStaleness: maxStaleness);

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Secondary);
            result.TagSets.Should().BeEmpty();
            result.MaxStaleness.Should().Be(maxStaleness);
        }

        [Fact]
        public void constructor_with_maxStaleness_should_initialize_instance_when_value_is_null()
        {
            var result = new ReadPreference(ReadPreferenceMode.Secondary, maxStaleness: null);

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Secondary);
            result.TagSets.Should().BeEmpty();
            result.MaxStaleness.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_maxStaleness_fields(
            [Values(null, 1, 2)]
            int? lhsSeconds,
            [Values(null, 1, 2)]
            int? rhsSeconds)
        {
            var lhsMaxStaleness = lhsSeconds.HasValue ? TimeSpan.FromSeconds(lhsSeconds.Value) : (TimeSpan?)null;
            var rhsMaxStaleness = rhsSeconds.HasValue ? TimeSpan.FromSeconds(rhsSeconds.Value) : (TimeSpan?)null;
            var lhs = new ReadPreference(ReadPreferenceMode.Secondary, maxStaleness: lhsMaxStaleness);
            var rhs = new ReadPreference(ReadPreferenceMode.Secondary, maxStaleness: rhsMaxStaleness);

            Equals_Act_and_Assert(lhs, rhs, lhsSeconds.Equals(rhsSeconds));
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_mode_fields(
            [Values(ReadPreferenceMode.Primary, ReadPreferenceMode.Secondary)]
            ReadPreferenceMode lhsMode,
            [Values(ReadPreferenceMode.Primary, ReadPreferenceMode.Secondary)]
            ReadPreferenceMode rhsMode)
        {
            var lhs = new ReadPreference(lhsMode);
            var rhs = new ReadPreference(rhsMode);

            Equals_Act_and_Assert(lhs, rhs, lhsMode.Equals(rhsMode));
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_compare_tagSets_fields(
            [Values(null, "a", "b")]
            string lhsTagValue,
            [Values(null, "a", "b")]
            string rhsTagValue)
        {
            var lhsTagSets = lhsTagValue == null ? null : new[] { new TagSet(new[] { new Tag("x", lhsTagValue) }) };
            var rhsTagSets = rhsTagValue == null ? null : new[] { new TagSet(new[] { new Tag("x", rhsTagValue) }) };
            var lhs = new ReadPreference(ReadPreferenceMode.Secondary, tagSets: lhsTagSets);
            var rhs = new ReadPreference(ReadPreferenceMode.Secondary, tagSets: rhsTagSets);

            Equals_Act_and_Assert(lhs, rhs, object.Equals(lhsTagValue, rhsTagValue));
        }

        [Fact]
        public void Nearest_should_return_expected_result()
        {
            var result = ReadPreference.Nearest;

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Nearest);
            result.TagSets.Count.Should().Be(0);
            result.MaxStaleness.Should().NotHaveValue();
        }

        [Fact]
        public void Primary_should_return_expected_result()
        {
            var result = ReadPreference.Primary;

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Primary);
            result.TagSets.Count.Should().Be(0);
            result.MaxStaleness.Should().NotHaveValue();
        }

        [Fact]
        public void PrimaryPreferred_should_return_expected_result()
        {
            var result = ReadPreference.PrimaryPreferred;

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.PrimaryPreferred);
            result.TagSets.Count.Should().Be(0);
            result.MaxStaleness.Should().NotHaveValue();
        }

        [Fact]
        public void Secondary_should_return_expected_result()
        {
            var result = ReadPreference.Secondary;

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Secondary);
            result.TagSets.Count.Should().Be(0);
            result.MaxStaleness.Should().NotHaveValue();
        }

        [Fact]
        public void SecondaryPreferred_should_return_expected_result()
        {
            var result = ReadPreference.SecondaryPreferred;

            result.ReadPreferenceMode.Should().Be(ReadPreferenceMode.SecondaryPreferred);
            result.TagSets.Count.Should().Be(0);
            result.MaxStaleness.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void ToString_should_return_expected_result(
            [Values(ReadPreferenceMode.Primary, ReadPreferenceMode.Secondary)]
            ReadPreferenceMode mode)
        {
            var subject = new ReadPreference(mode);

            var result = subject.ToString();

            result.Should().Be($"{{ Mode : {mode} }}");
        }

        [Theory]
        [InlineData(new[] { 1 }, "[{ x : a }]")]
        [InlineData(new[] { 2 }, "[{ x : a, y : b }]")]
        [InlineData(new[] { 1, 1 }, "[{ x : a }, { x : a }]")]
        [InlineData(new[] { 2, 1 }, "[{ x : a, y : b }, { x : a }]")]
        [InlineData(new[] { 1, 2 }, "[{ x : a }, { x : a, y : b }]")]
        public void ToString_should_return_expected_result_when_tagSets_is_set(int[] tagSetSizes, string expectedTagSetsString)
        {
            var tagSets = new List<TagSet>();
            foreach (var size in tagSetSizes)
            {
                var tags = new List<Tag>();
                for (var i = 0; i < size; i++)
                {
                    var name = new string((char)('x' + i), 1);
                    var value = new string((char)('a' + i), 1);
                    var tag = new Tag(name, value);
                    tags.Add(tag);
                }
                var tagSet = new TagSet(tags);
                tagSets.Add(tagSet);
            }
            var subject = new ReadPreference(ReadPreferenceMode.Secondary, tagSets: tagSets);

            var result = subject.ToString();

            result.Should().Be($"{{ Mode : Secondary, TagSets : {expectedTagSetsString} }}");
        }

        [Theory]
        [ParameterAttributeData]
        public void ToString_should_return_expected_result_when_maxStaleness_is_set(
            [Values(1, 2)]
            int seconds)
        {
            var maxStaleness = TimeSpan.FromSeconds(seconds);
            var subject = new ReadPreference(ReadPreferenceMode.Secondary, maxStaleness: maxStaleness );

            var result = subject.ToString();

            result.Should().Be($"{{ Mode : Secondary, MaxStaleness : {seconds}s }}");
        }

        [Theory]
        [ParameterAttributeData]
        public void With_maxStaleness_should_return_expected_result(
            [Values(1, 2)]
            int originalSeconds,
            [Values(1, 2)]
            int seconds)
        {
            var originalMaxStaleness = TimeSpan.FromSeconds(originalSeconds);
            var maxStaleness = TimeSpan.FromSeconds(seconds);
            var subject = new ReadPreference(ReadPreferenceMode.Secondary, maxStaleness: originalMaxStaleness);

            var result = subject.With(maxStaleness);

            result.MaxStaleness.Should().Be(maxStaleness);
            result.With(originalMaxStaleness).Should().Be(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_mode_should_return_expected_result(
            [Values(ReadPreferenceMode.Primary, ReadPreferenceMode.Secondary)]
            ReadPreferenceMode originalMode,
            [Values(ReadPreferenceMode.Primary, ReadPreferenceMode.Secondary)]
            ReadPreferenceMode mode)
        {
            var subject = new ReadPreference(originalMode);

            var result = subject.With(mode);

            result.ReadPreferenceMode.Should().Be(mode);
            result.With(originalMode).Should().Be(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_tagSets_should_return_expected_result(
            [Values("a", "b")]
            string originalTagValue,
            [Values("a", "b")]
            string tagValue)
        {
            var originalTagSets = new[] { new TagSet(new[] { new Tag("x", originalTagValue) }) };
            var tagSets = new[] { new TagSet(new[] { new Tag("x", tagValue) }) };
            var subject = new ReadPreference(ReadPreferenceMode.Secondary, tagSets: originalTagSets);

            var result = subject.With(tagSets);

            result.TagSets.Should().Equal(tagSets);
            result.With(originalTagSets).Should().Be(subject);
        }

        // private methods
        private void Equals_Act_and_Assert(ReadPreference lhs, ReadPreference rhs, bool expectedResult)
        {
            var result1 = lhs.Equals(rhs);
            var result2 = lhs.Equals((object)rhs);
            var hashCode1 = lhs.GetHashCode();
            var hashCode2 = rhs.GetHashCode();

            result1.Should().Be(expectedResult);
            result2.Should().Be(expectedResult);
            (hashCode1 == hashCode2).Should().Be(expectedResult);
        }
    }
}
