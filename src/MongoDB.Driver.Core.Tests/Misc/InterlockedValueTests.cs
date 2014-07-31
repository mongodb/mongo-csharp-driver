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
    public class InterlockedValueTests
    {
        [Test]
        public void Value_should_return_initial_value_after_construction()
        {
            var subject = new InterlockedValue(3);

            subject.Value.Should().Be(3);
        }

        [Test]
        public void Value_should_return_new_value_after_a_successful_change()
        {
            var subject = new InterlockedValue(3);
            subject.TryChange(5);

            subject.Value.Should().Be(5);
        }

        [Test]
        public void Value_should_return_current_value_after_an_unsuccessful_change()
        {
            var subject = new InterlockedValue(3);
            subject.TryChange(4, 5);

            subject.Value.Should().Be(3);
        }

        [Test]
        [TestCase(0, 0, false)]
        [TestCase(0, 1, true)]
        [TestCase(1, 0, true)]
        [TestCase(1, 1, false)]
        public void TryChange_with_one_parameter(int currentValue, int newValue, bool expected)
        {
            var subject = new InterlockedValue(currentValue);
            var result = subject.TryChange(newValue);

            result.Should().Be(expected);
        }

        [Test]
        [TestCase(0, 0, 1, true)]
        [TestCase(0, 1, 1, false)]
        [TestCase(0, 1, 2, false)]
        [TestCase(0, 1, 0, false)]
        public void TryChange_with_two_parameters(int currentValue, int oldValue, int newValue, bool expected)
        {
            var subject = new InterlockedValue(currentValue);
            var result = subject.TryChange(oldValue, newValue);

            result.Should().Be(expected);
        }
    }
}
