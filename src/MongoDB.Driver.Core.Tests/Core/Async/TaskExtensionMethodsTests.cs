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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Async;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Async
{
    [TestFixture]
    public class TaskExtensionMethodsTests
    {
        [Test]
        public void WithTimeout_with_task_timeout_and_cancellationToken_parameters_should_be_cancellable()
        {
            var task = Task.Run(() => { Thread.Sleep(TimeSpan.FromMilliseconds(500)); });
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellableTask = task.WithTimeout(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            cancellableTask.IsCanceled.Should().BeTrue();
        }

        [Test]
        public async Task WithTimeout_with_task_timeout_and_cancellationToken_parameters_should_not_timeout()
        {
            var task = Task.Run(() => { });
            var cancellationTokenSource = new CancellationTokenSource();
            await task.WithTimeout(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
        }

        [Test]
        public void WithTimeout_with_task_timeout_and_cancellationToken_parameters_should_timeout()
        {
            var task = Task.Run(() => { Thread.Sleep(TimeSpan.FromSeconds(1)); });
            var cancellationTokenSource = new CancellationTokenSource();
            Action action = () => task.WithTimeout(TimeSpan.FromMilliseconds(1), cancellationTokenSource.Token).Wait();
            action.ShouldThrow<TimeoutException>();
        }
    }
}
