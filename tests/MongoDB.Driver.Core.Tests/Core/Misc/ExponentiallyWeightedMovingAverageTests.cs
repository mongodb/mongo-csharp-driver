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
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class ExponentiallyWeightedMovingAverageTests
    {
        [Theory]
        [InlineData(-0.001)]
        [InlineData(-0.01)]
        [InlineData(-0.1)]
        [InlineData(-1)]
        public void Constructor_should_throw_an_ArgumentOutOfRangeException_if_alpha_is_below_0(double alpha)
        {
            Action act = () => new ExponentiallyWeightedMovingAverage(alpha);

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(1.001)]
        [InlineData(1.01)]
        [InlineData(1.1)]
        [InlineData(2)]
        public void Constructor_should_throw_an_ArgumentOutOfRangeException_if_alpha_is_above_1(double alpha)
        {
            Action act = () => new ExponentiallyWeightedMovingAverage(alpha);

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(0.2, new[] { 10 }, 10)]
        [InlineData(0.2, new[] { 10, 20 }, 12)]
        [InlineData(0.2, new[] { 10, 20, 12 }, 12)]
        [InlineData(0.2, new[] { 10, 20, 12, 17 }, 13)]
        public void Average_should_be_properly_computed(double alpha, int[] samples, double result)
        {
            var subject = new ExponentiallyWeightedMovingAverage(alpha);

            foreach (var sample in samples)
            {
                subject.AddSample(TimeSpan.FromMilliseconds(sample));
            }

            subject.Average.TotalMilliseconds.Should().Be(result);
        }
    }
}