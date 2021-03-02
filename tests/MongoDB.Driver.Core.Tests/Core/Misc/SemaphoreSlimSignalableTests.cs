/* Copyright 2021-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class SemaphoreSlimSignalableTests
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void SemaphoreSlimSignalable_constructor_should_check_arguments([Values(-1, 0, 1025)]int count)
        {
            var exception = Record.Exception(() => new SemaphoreSlimSignalable(count));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("count");
            e.Message.Should().StartWith("Value is not between");
        }

        [Theory]
        [ParameterAttributeData]
        public void SemaphoreSlimSignalable_wait_should_enter(
            [Values(true, false)]bool async,
            [Values(1, 2, 4)]int count)
        {
            var semaphore = new SemaphoreSlimSignalable(count);

            if (async)
            {
                for (int i = 0; i < count; i++)
                {
                    var waitResult = semaphore.Wait(TimeSpan.Zero, default);
                }
            }
            else
            {

            }
            
        }
    }
}
