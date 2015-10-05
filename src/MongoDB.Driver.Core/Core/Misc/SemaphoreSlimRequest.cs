/* Copyright 2015 MongoDB Inc.
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

namespace MongoDB.Driver.Core.Misc
{
    /// <summary>
    /// Represents a tentative request to acquire a SemaphoreSlim.
    /// </summary>
    public sealed class SemaphoreSlimRequest : IDisposable
    {
        // private fields
        private readonly CancellationTokenSource _disposeCancellationTokenSource;
        private readonly SemaphoreSlim _semaphore;
        private readonly Task _task;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphoreSlimRequest"/> class.
        /// </summary>
        /// <param name="semaphore">The semaphore.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public SemaphoreSlimRequest(SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {

            _semaphore = Ensure.IsNotNull(semaphore, nameof(semaphore));

            _disposeCancellationTokenSource = new CancellationTokenSource();
            var disposeCancellationToken = _disposeCancellationTokenSource.Token;
            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, disposeCancellationToken);
            var linkedCancellationToken = linkedCancellationTokenSource.Token;

            _task = semaphore.WaitAsync(linkedCancellationToken);
        }

        // public properties
        /// <summary>
        /// Gets the semaphore wait task.
        /// </summary>
        /// <value>
        /// The semaphore wait task.
        /// </value>
        public Task Task => _task;

        // public methods        
        /// <inheritdoc/>
        public void Dispose()
        {
            if (_task.Status == TaskStatus.RanToCompletion)
            {
                _semaphore.Release(); // we already had the lock
            }
            else
            {
                _disposeCancellationTokenSource.Cancel();
                try
                {
                    _task.GetAwaiter().GetResult(); // will throw OperationCanceledException if we didn't get the lock
                    _semaphore.Release(); // we got the lock while we were trying to cancel the request
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
            }

            _disposeCancellationTokenSource.Dispose();
        }
    }
}
