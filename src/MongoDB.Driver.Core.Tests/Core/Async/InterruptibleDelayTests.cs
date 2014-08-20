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
using System.Threading;
using FluentAssertions;
using MongoDB.Driver.Core.Async;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Async
{
    [TestFixture]
    public class InterruptibleDelayTests
    {
        [Test]
        public void Constructor_should_throw_an_ArgumentException_when_the_delay_is_less_than_negative_1()
        {
            Action act = () => new InterruptibleDelay(TimeSpan.FromMilliseconds(-2), CancellationToken.None);

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Task_should_be_complete_after_the_delay_has_expired()
        {
            var subject = new InterruptibleDelay(TimeSpan.FromMilliseconds(10), CancellationToken.None);
            subject.Task.Wait(TimeSpan.FromMilliseconds(100)).Should().BeTrue();
        }

        [Test]
        public void Task_should_be_complete_after_getting_interupted()
        {
            var subject = new InterruptibleDelay(TimeSpan.FromHours(10), CancellationToken.None);
            subject.Interrupt();
            subject.Task.IsCompleted.Should().BeTrue();
        }
    }
}