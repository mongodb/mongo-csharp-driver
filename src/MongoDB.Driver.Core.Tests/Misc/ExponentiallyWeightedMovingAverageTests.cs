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
    public class ExponentiallyWeightedMovingAverageTests
    {
        [Test]
        [TestCase(-0.001)]
        [TestCase(-0.01)]
        [TestCase(-0.1)]
        [TestCase(-1)]
        public void Constructor_should_throw_an_ArgumentOutOfRangeException_if_alpha_is_below_0(double alpha)
        {
            Action act = () => new ExponentiallyWeightedMovingAverage(alpha);

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        [TestCase(1.001)]
        [TestCase(1.01)]
        [TestCase(1.1)]
        [TestCase(2)]
        public void Constructor_should_throw_an_ArgumentOutOfRangeException_if_alpha_is_above_1(double alpha)
        {
            Action act = () => new ExponentiallyWeightedMovingAverage(alpha);

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        [TestCase(0.2, new[] { 10 }, 10)]
        [TestCase(0.2, new[] { 10, 20 }, 12)]
        [TestCase(0.2, new[] { 10, 20, 12 }, 12)]
        [TestCase(0.2, new[] { 10, 20, 12, 17 }, 13)]
        public void Average_should_be_properly_computed(double alpha, int[] samples, double result)
        {
            var subject = new ExponentiallyWeightedMovingAverage(alpha);

            foreach(var sample in samples)
            {
                subject.AddSample(TimeSpan.FromMilliseconds(sample));
            }

            subject.Average.TotalMilliseconds.Should().Be(result);
        }
    }
}