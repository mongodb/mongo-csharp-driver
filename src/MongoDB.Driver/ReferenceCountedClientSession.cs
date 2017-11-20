/* Copyright 2017 MongoDB Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// A reference counted client session wrapper.
    /// </summary>
    /// <seealso cref="MongoDB.Driver.WrappingClientSession" />
    internal sealed class ReferenceCountedClientSession : WrappingClientSession
    {
        // private fields
        private readonly object _lock = new object();
        private int _referenceCount;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceCountedClientSession"/> class.
        /// </summary>
        /// <param name="wrapped">The wrapped session.</param>
        public ReferenceCountedClientSession(IClientSession wrapped)
            : base(wrapped, ownsWrapped: false)
        {
            _referenceCount = 1;
        }

        // public methods
        /// <summary>
        /// Decrements the reference count.
        /// </summary>
        public void DecrementReferenceCount()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                if (--_referenceCount == 0)
                {
                    Dispose();
                }
            }
        }

        /// <summary>
        /// Increments the reference count.
        /// </summary>
        public void IncrementReferenceCount()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                _referenceCount++;
            }
        }

        // protected methods
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!IsDisposed())
                {
                    Wrapped.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
