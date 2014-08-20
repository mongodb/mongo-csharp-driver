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
using MongoDB.Driver.Core.Misc;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Misc
{
    [TestFixture]
    public class SlidingTimeoutTests
    {
        private FrozenClock _clock;

        [SetUp]
        public void Setup()
        {
            _clock = FrozenClock.FreezeUtcNow();
        }

        [Test]
        public void Expiration_should_be_DateTime_Max_when_timeout_is_zero()
        {
            var subject = new SlidingTimeout(TimeSpan.Zero);

            subject.Expiration.Should().Be(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
        }

        [Test]
        public void Expiration_should_be_DateTime_Max_when_timeout_is_infinite()
        {
            var subject = new SlidingTimeout(Timeout.InfiniteTimeSpan);

            subject.Expiration.Should().Be(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
        }

        [Test]
        public void Expiration_should_be_UtcNow_plus_timeout()
        {
            var subject = new SlidingTimeout(TimeSpan.FromSeconds(10), _clock);

            subject.Expiration.Should().Be(_clock.UtcNow.AddSeconds(10));
        }

        [Test]
        public void ThrowIfExpired_should_not_throw_when_the_timeout_has_not_expired()
        {
            var subject = new SlidingTimeout(TimeSpan.FromSeconds(10), _clock);

            Action act = () => subject.ThrowIfExpired();

            act.ShouldNotThrow();
        }

        [Test]
        public void ThrowIfExpired_should_throw_when_the_timeout_has_expired()
        {
            var subject = new SlidingTimeout(TimeSpan.FromSeconds(10), _clock);
            _clock.UtcNow = _clock.UtcNow.AddSeconds(11);

            Action act = () => subject.ThrowIfExpired();

            act.ShouldThrow<TimeoutException>();
        }
        
        [Test]
        public void ToTimeout_should_return_the_amount_of_time_left()
        {
            var subject = new SlidingTimeout(TimeSpan.FromMilliseconds(20), _clock);

            subject.ToTimeout().Should().Be(TimeSpan.FromMilliseconds(20));

            _clock.UtcNow = _clock.UtcNow.AddMilliseconds(10);

            subject.ToTimeout().Should().Be(TimeSpan.FromMilliseconds(10));

            _clock.UtcNow = _clock.UtcNow.AddMilliseconds(10);

            subject.ToTimeout().Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void ToTimeout_should_throw_a_TimeoutException_when_the_timeout_has_expired()
        {
            var subject = new SlidingTimeout(TimeSpan.FromMilliseconds(20), _clock);

            _clock.UtcNow = _clock.UtcNow.AddMilliseconds(30);

            Action act = () => subject.ToTimeout();

            act.ShouldThrow<TimeoutException>();
        }

        [Test]
        public void ToTimeout_should_return_infinite_TimeSpan_when_expiration_is_MaxValue()
        {
            var subject = new SlidingTimeout(Timeout.InfiniteTimeSpan);

            subject.ToTimeout().Should().Be(Timeout.InfiniteTimeSpan);
        }
    }
}