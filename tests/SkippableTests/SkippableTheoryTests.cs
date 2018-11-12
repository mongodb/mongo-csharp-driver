using System;
using FluentAssertions;
using Xunit;

namespace SkippableTests
{
    public class SkippableTheoryTests
    {
        [SkippableTheory]
        [InlineData(0)]
        public void SkippableTheory_should_fail(int x)
        {
            x.Should().Be(1);
        }

        [SkippableTheory]
        [InlineData(0)]
        public void SkippableTheory_should_pass(int x)
        {
            x.Should().Be(0);
        }

        [SkippableTheory]
        [InlineData(0)]
        public void SkippableTheory_should_be_skipped(int x)
        {
            if (x == 0)
            {
                throw new SkipException("test");
            }
        }
    }
}
