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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Misc;
using Xunit;


namespace MongoDB.Driver.Core.Async
{
    public class MetronomeTests
    {
        private FrozenClock _clock;
        private readonly TimeSpan _halfPeriod = TimeSpan.FromMilliseconds(500);
        private readonly TimeSpan _period = TimeSpan.FromMilliseconds(1000);
        private readonly TimeSpan _quarterPeriod = TimeSpan.FromMilliseconds(250);
        private Metronome _subject;
        private readonly TimeSpan _threeQuarterPeriod = TimeSpan.FromMilliseconds(750);

        public MetronomeTests()
        {
            _clock = FrozenClock.FreezeUtcNow();
            _subject = new Metronome(_period, _clock);
        }

        [Fact]
        public void Constructor_should_initialize_instance()
        {
            _subject.NextTick.Should().Be(_clock.UtcNow);
            _subject.Period.Should().Be(_period);
        }

        [Fact]
        public void Constructor_should_throw_if_clock_is_null()
        {
            Action act = () => new Metronome(_period, null);
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_period_is_negative()
        {
            var period = TimeSpan.FromMilliseconds(-2);
            Action act = () => new Metronome(period);
            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void GetNextTickDelay_should_be_infinite_if_period_is_infinite()
        {
            var period = Timeout.InfiniteTimeSpan;
            var subject = new Metronome(period);
            subject.GetNextTickDelay().Should().Be(Timeout.InfiniteTimeSpan);
        }

        [Fact]
        public void GetNextTickDelay_should_be_threeQuarterPeriod_when_oneQuarterPeriod_past_the_last_tick()
        {
            var now = _clock.UtcNow;
            _clock.UtcNow = _clock.UtcNow.Add(_quarterPeriod);
            _subject.GetNextTickDelay().Should().Be(_threeQuarterPeriod);
            _subject.NextTick.Should().Be(now + _period);
        }

        [Fact]
        public void GetNextTickDelay_should_be_zero_when_first_instantiated()
        {
            _subject.GetNextTickDelay().Should().Be(TimeSpan.Zero);
            _subject.NextTick.Should().Be(_clock.UtcNow);
        }

        [Fact]
        public void GetNextTickDelay_should_be_zero_when_time_equals_nextTick()
        {
            var now = _clock.UtcNow;
            _clock.UtcNow = _clock.UtcNow.Add(_period);
            _subject.GetNextTickDelay().Should().Be(TimeSpan.Zero);
            _subject.NextTick.Should().Be(now + _period);
        }

        [Fact]
        public void GetNextTickDelay_should_not_advance_nextTick_when_called_more_than_once_during_the_same_period()
        {
            var now = _clock.UtcNow;
            _clock.UtcNow = _clock.UtcNow.Add(_quarterPeriod);
            _subject.GetNextTickDelay().Should().Be(_threeQuarterPeriod);
            _subject.NextTick.Should().Be(now + _period);

            _clock.UtcNow = _clock.UtcNow.Add(_quarterPeriod);
            _subject.GetNextTickDelay().Should().Be(_halfPeriod);
            _subject.NextTick.Should().Be(now + _period);
        }

        [Fact]
        public void GetNextTickDelay_should_skip_one_missed_tick()
        {
            var now = _clock.UtcNow;
            _clock.UtcNow = _clock.UtcNow.Add(_period + _quarterPeriod);
            _subject.GetNextTickDelay().Should().Be(_threeQuarterPeriod);
            _subject.NextTick.Should().Be(now + _period + _period);
        }

        [Fact]
        public void GetNextTickDelay_should_skip_two_missed_ticks()
        {
            var now = _clock.UtcNow;
            _clock.UtcNow = _clock.UtcNow.Add(_period + _period + _quarterPeriod);
            _subject.GetNextTickDelay().Should().Be(_threeQuarterPeriod);
            _subject.NextTick.Should().Be(now + _period + _period + _period);
        }
    }
}
