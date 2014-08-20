/* Copyright 2013-2014 MongoDB Inc.
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
using NUnit.Framework;

namespace MongoDB.Driver.Core.Misc
{
    [TestFixture]
    public class TimeSpanParserTests
    {
        [Test]
        [TestCase("2", 2 * 1000)]
        [TestCase("2ms", 2)]
        [TestCase("2s", 2 * 1000)]
        [TestCase("2m", 2 * 1000 * 60)]
        [TestCase("2h", 2 * 1000 * 60 * 60)]
        public void Parse_should_return_the_correct_TimeSpan(string value, long expectedMilliseconds)
        {
            TimeSpan result;
            var success = TimeSpanParser.TryParse(value, out result);

            success.Should().BeTrue();
            result.TotalMilliseconds.Should().Be(expectedMilliseconds);
        }

        [Test]
        [TestCase(2, "2ms")]
        [TestCase(2 * 1000, "2s")]
        [TestCase(2 * 1000 * 60, "2m")]
        [TestCase(2 * 1000 * 60 * 60, "2h")]
        public void ToString_should_return_the_correct_string(int milliseconds, string expectedString)
        {
            var result = TimeSpanParser.ToString(TimeSpan.FromMilliseconds(milliseconds));

            result.Should().Be(expectedString);
        }
    }
}
