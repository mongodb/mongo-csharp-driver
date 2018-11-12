using System;
using FluentAssertions;
using Xunit;

namespace SkippableTests
{
    public class TheoryTests
    {
        [Theory]
        [InlineData(0)]
        public void Theory_should_fail(int x)
        {
            x.Should().Be(1);
        }

        [Theory]
        [InlineData(0)]
        public void Theory_should_pass(int x)
        {
            x.Should().Be(0);
        }
    }
}
