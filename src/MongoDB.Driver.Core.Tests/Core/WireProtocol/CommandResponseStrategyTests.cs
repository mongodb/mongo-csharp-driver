/* Copyright 2013-2015 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.WireProtocol
{
    [TestFixture]
    public class CommandResponseStrategyTests
    {
        [Test]
        public void Read_should_invoke_the_delegate()
        {
            var subject = CommandResponseStrategy<BsonDocument>.Read;

            var result = subject.Decide(() => new BsonDocument("x", 1));
            result.Should().Be("{x: 1}");
        }

        [Test]
        public void Read_async_should_invoke_the_delegate()
        {
            var subject = CommandResponseStrategy<BsonDocument>.Read;

            var tcs = new TaskCompletionSource<BsonDocument>();
            var task = subject.DecideAsync(() => tcs.Task);
            task.IsCompleted.Should().BeFalse();
            tcs.SetResult(new BsonDocument("x", 1));
            task.IsCompleted.Should().BeTrue();

            task.Result.Should().Be("{x: 1}");
        }

        [Test]
        public void ThrowAway_should_invoke_the_delegate()
        {
            var subject = CommandResponseStrategy<BsonDocument>.ThrowAway(new BsonDocument("a", 0));
            bool invoked = false;

            var result = subject.Decide(() => { invoked = true; return new BsonDocument("x", 1); });
            result.Should().Be("{a: 0}");

            // race condition going on here...
            Thread.Sleep(10);
            invoked.Should().BeTrue();
        }

        [Test]
        public void ThrowAway_async_should_invoke_the_delegate()
        {
            var subject = CommandResponseStrategy<BsonDocument>.ThrowAway(new BsonDocument("a", 0));
            bool invoked = false;

            var tcs = new TaskCompletionSource<BsonDocument>();
            var task = subject.DecideAsync(() => { invoked = true; return tcs.Task; });
            task.IsCompleted.Should().BeTrue();
            task.Result.Should().Be("{a: 0}");
            invoked.Should().BeTrue();
        }

    }
}
