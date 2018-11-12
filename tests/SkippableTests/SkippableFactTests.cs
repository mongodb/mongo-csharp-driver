using System;
using FluentAssertions;
using Xunit;

namespace SkippableTests
{
    public class SkippableFactTests
    {
        [SkippableFact]
        public void SkippableFact_should_fail()
        {
            true.Should().BeFalse();
        }

        [SkippableFact]
        public void SkippableFact_should_pass()
        {
            true.Should().BeTrue();
        }

        [SkippableFact]
        public void SkippableFact_should_be_skipped()
        {
            throw new SkipException("test");
        }
    }
}
