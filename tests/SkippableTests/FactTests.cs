using System;
using FluentAssertions;
using Xunit;

namespace SkippableTests
{
    public class FactTests
    {
        [Fact]
        public void Fact_should_fail()
        {
            true.Should().BeFalse();
        }

        [Fact]
        public void Fact_should_pass()
        {
            true.Should().BeTrue();
        }
    }
}
