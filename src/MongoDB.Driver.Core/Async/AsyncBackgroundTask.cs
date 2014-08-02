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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Async
{
    internal static class AsyncBackgroundTask
    {
        public static async Task Start(Func<CancellationToken, Task> action, TimeSpan delay, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(action, "action");
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero(delay, "delay");

            try
            {
                if(delay.Equals(Timeout.InfiniteTimeSpan))
                {
                    await action(cancellationToken).ConfigureAwait(false);
                    return;
                }

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await action(cancellationToken).ConfigureAwait(false);
                    if (!delay.Equals(TimeSpan.Zero))
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch(TaskCanceledException)
            { }
        }
    }
}