/* Copyright 2015-2016 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class SemanticVersionTests
    {
        [Theory]
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
        public void Comparisons_should_be_correct(string a, string b, int comparison)
        {
            var subject = SemanticVersion.Parse(a);
            var comparand = b == null ? null : SemanticVersion.Parse(b);
            subject.Equals(comparand).Should().Be(comparison == 0);
            subject.CompareTo(comparand).Should().Be(comparison);
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
        public void ToString_should_render_a_correct_semantic_version_string(string versionString, int major, int minor, int patch, string preRelease)
        {
            var subject = new SemanticVersion(major, minor, patch, preRelease);

            subject.ToString().Should().Be(versionString);
        }
    }
}