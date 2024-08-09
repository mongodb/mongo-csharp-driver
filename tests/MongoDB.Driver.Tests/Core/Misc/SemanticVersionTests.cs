/* Copyright 2015-present MongoDB Inc.
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
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class SemanticVersionTests
    {
        [Theory]
        [InlineData(null, null, 0)]
        [InlineData(null, "1.0.0", -1)]
        [InlineData("1.0.0", null, 1)]
        [InlineData("1.0.0", "1.0.0", 0)]
        [InlineData("1.1.0", "1.1.0", 0)]
        [InlineData("1.1.1", "1.1.1", 0)]
        [InlineData("1.1.1-rc2", "1.1.1-rc2", 0)]
        [InlineData("1.0.0", "2.0.0", -1)]
        [InlineData("2.0.0", "1.0.0", 1)]
        [InlineData("1.0.0", "1.1.0", -1)]
        [InlineData("1.1.0", "1.0.0", 1)]
        [InlineData("1.0.0", "1.0.1", -1)]
        [InlineData("1.0.1", "1.0.0", 1)]
        [InlineData("1.0.0-alpha", "1.0.0-beta", -1)]
        [InlineData("1.0.0-beta", "1.0.0-alpha", 1)]
        [InlineData("4.4.0-rc12", "4.4.0-rc12", 0)]
        [InlineData("4.4.0-rc12", "4.4.0", -1)]
        [InlineData("4.4.1-rc12", "4.4.0", 1)]
        [InlineData("4.4.0-rc13", "4.4.0-rc12", 1)]
        [InlineData("4.5.0-489-gb8f58d7", "4.5.0-489-gb8f58d7", 0)]
        [InlineData("4.5.0-489-gb8f58d7", "4.5.1", -1)]
        [InlineData("4.5.0-489-gb8f58d7", "4.5.0", 1)]
        [InlineData("4.5.0-489-gb8f58d7", "4.5.0-5-g5a9a742f6f", 1)]
        [InlineData("4.4.0-rc12-5-g5a9a742f6f", "4.4.0-rc12-5-g5a9a742f6f", 0)]
        [InlineData("4.4.0-rc12-5-g5a9a742f6f", "4.4.0-rc13", -1)]
        [InlineData("4.4.0-rc12-5-g5a9a742f6f", "4.4.0-rc12", 1)]
        [InlineData("4.4.0-rc12-5-g5a9a742f6f", "4.4.0-rc12-4-g5a9a742f6f", 1)]
        [InlineData("4.4.0-rc12-5-g5a9a742f6f", "4.4.0-rc13-5-g5a9a742f6f", -1)]
        [InlineData("4.4.0-alpha1", "4.4.0-alpha", 1)]
        [InlineData("4.4.0-alpha3", "4.4.0-beta1", -1)]
        [InlineData("4.4.0-alpha0", "4.4.0-alpha", 1)]
        [InlineData("4.4.0-alpha2", "4.4.0-alpha1", 1)]
        [InlineData("4.4.0-alpha2", "4.4.0-alpha11", -1)]
        [InlineData("4.4.0-alpha", "4.4.0", -1)]
        [InlineData("4.4.0-alpha", "4.4.0-alpha", 0)]
        [InlineData("4.4.0-beta", "4.4.0-5-g5a9a742f6f", -1)]
        [InlineData("4.4.0-alpha12-5-g5a9a742f6f", "4.4.0-rc13-5-g5a9a742f6f", -1)]
        public void Comparisons_should_be_correct(string a, string b, int comparison)
        {
            var subject = a == null ? null : SemanticVersion.Parse(a);
            var comparand = b == null ? null : SemanticVersion.Parse(b);
            subject?.Equals(comparand).Should().Be(comparison == 0);
            subject?.CompareTo(comparand).Should().Be(comparison);
            (subject == comparand).Should().Be(comparison == 0);
            (subject != comparand).Should().Be(comparison != 0);
            (subject > comparand).Should().Be(comparison == 1);
            (subject >= comparand).Should().Be(comparison >= 0);
            (subject < comparand).Should().Be(comparison == -1);
            (subject <= comparand).Should().Be(comparison <= 0);
        }

        [Theory]
        [InlineData("1.0.0", 1, 0, 0, null)]
        [InlineData("1.2.0", 1, 2, 0, null)]
        [InlineData("1.0.3", 1, 0, 3, null)]
        [InlineData("1.0.3-rc", 1, 0, 3, "rc")]
        [InlineData("1.0.3-rc1", 1, 0, 3, "rc1")]
        [InlineData("1.0.3-rc.2.3", 1, 0, 3, "rc.2.3")]
        public void Parse_should_handle_valid_semantic_version_strings(string versionString, int major, int minor, int patch, string preRelease)
        {
            var subject = SemanticVersion.Parse(versionString);

            subject.Major.Should().Be(major);
            subject.Minor.Should().Be(minor);
            subject.Patch.Should().Be(patch);
            subject.PreRelease.Should().Be(preRelease);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1-rc2")]
        [InlineData("alpha")]
        public void Parse_should_throw_a_FormatException_when_the_version_string_is_invalid(string versionString)
        {
            Action act = () => SemanticVersion.Parse(versionString);

            act.ShouldThrow<FormatException>();
        }

        [Theory]
        [InlineData("1.0.0", 1, 0, 0, null)]
        [InlineData("1.2.0", 1, 2, 0, null)]
        [InlineData("1.0.3", 1, 0, 3, null)]
        [InlineData("1.0.3-rc", 1, 0, 3, "rc")]
        [InlineData("1.0.3-rc1", 1, 0, 3, "rc1")]
        [InlineData("1.0.3-rc.2.3", 1, 0, 3, "rc.2.3")]
        [InlineData("4.4.0-rc12-5-g5a9a742f6f", 4, 4, 0, "rc12-5-g5a9a742f6f")]
        public void ToString_should_render_a_correct_semantic_version_string(string versionString, int major, int minor, int patch, string preRelease)
        {
            var subject = new SemanticVersion(major, minor, patch, preRelease);

            subject.ToString().Should().Be(versionString);
        }
    }
}
