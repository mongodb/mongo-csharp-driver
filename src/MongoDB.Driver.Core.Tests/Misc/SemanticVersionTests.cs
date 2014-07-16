using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Misc
{
    [TestFixture]
    public class SemanticVersionTests
    {
        [Test]
        [TestCase("1.0.0", 1, 0, 0, null)]
        [TestCase("1.2.0", 1, 2, 0, null)]
        [TestCase("1.0.3", 1, 0, 3, null)]
        [TestCase("1.0.3-rc", 1, 0, 3, "rc")]
        [TestCase("1.0.3-rc1", 1, 0, 3, "rc1")]
        [TestCase("1.0.3-rc.2.3", 1, 0, 3, "rc.2.3")]
        public void Parse_should_handle_valid_semantic_version_strings(string versionString, int major, int minor, int patch, string preRelease)
        {
            var subject = SemanticVersion.Parse(versionString);

            subject.Major.Should().Be(major);
            subject.Minor.Should().Be(minor);
            subject.Patch.Should().Be(patch);
            subject.PreRelease.Should().Be(preRelease);
        }

        [Test]
        [TestCase("1")]
        [TestCase("1.0")]
        [TestCase("1.0.a")]
        [TestCase("1-rc2")]
        [TestCase("1.0-rc2")]
        [TestCase("alpha")]
        public void Parse_should_throw_a_FormatException_when_the_version_string_is_invalid(string versionString)
        {
            Action act = () => SemanticVersion.Parse(versionString);

            act.ShouldThrow<FormatException>();
        }

        [Test]
        [TestCase("1.0.0", 1, 0, 0, null)]
        [TestCase("1.2.0", 1, 2, 0, null)]
        [TestCase("1.0.3", 1, 0, 3, null)]
        [TestCase("1.0.3-rc", 1, 0, 3, "rc")]
        [TestCase("1.0.3-rc1", 1, 0, 3, "rc1")]
        [TestCase("1.0.3-rc.2.3", 1, 0, 3, "rc.2.3")]
        public void ToString_should_render_a_correct_semantic_version_string(string versionString, int major, int minor, int patch, string preRelease)
        {
            var subject = new SemanticVersion(major, minor, patch, preRelease);

            subject.ToString().Should().Be(versionString);
        }
    }
}