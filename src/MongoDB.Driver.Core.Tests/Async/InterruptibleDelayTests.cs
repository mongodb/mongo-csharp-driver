using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using MongoDB.Driver.Core.Async;
using System.Threading;

namespace MongoDB.Driver.Core.Tests.Async
{
    [TestFixture]
    public class InterruptibleDelayTests
    {
        [Test]
        public void Constructor_should_throw_an_ArgumentException_when_the_delay_is_less_than_negative_1()
        {
            Action act = () => new InterruptibleDelay(TimeSpan.FromMilliseconds(-2));

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Task_should_be_complete_after_the_delay_has_expired()
        {
            var subject = new InterruptibleDelay(TimeSpan.FromMilliseconds(10));
            subject.Task.Wait(TimeSpan.FromMilliseconds(100)).Should().BeTrue();
        }

        [Test]
        public void Task_should_be_complete_after_getting_interupted()
        {
            var subject = new InterruptibleDelay(TimeSpan.FromHours(10));
            subject.Interrupt();
            subject.Task.IsCompleted.Should().BeTrue();
        }
    }
}