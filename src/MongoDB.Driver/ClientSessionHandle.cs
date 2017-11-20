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

using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// A client session handle
    /// </summary>
    /// <seealso cref="MongoDB.Driver.IClientSessionHandle" />
    internal sealed class ClientSessionHandle : WrappingClientSession, IClientSessionHandle
    {
        // private fields
        private readonly ReferenceCountedClientSession _wrapped;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSessionHandle"/> class.
        /// </summary>
        /// <param name="wrapped">The wrapped session.</param>
        public ClientSessionHandle(IClientSession wrapped)
            : this(new ReferenceCountedClientSession(Ensure.IsNotNull(wrapped, nameof(wrapped))))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSessionHandle"/> class.
        /// </summary>
        /// <param name="wrapped">The wrapped session.</param>
        public ClientSessionHandle(ReferenceCountedClientSession wrapped)
            : base(Ensure.IsNotNull(wrapped, nameof(wrapped)), ownsWrapped: false)
        {
            _wrapped = wrapped;
        }

        // public methods
        /// <inheritdoc />
        public IClientSessionHandle Fork()
        {
            ThrowIfDisposed();
            _wrapped.IncrementReferenceCount();
            return new ClientSessionHandle(_wrapped);
        }

        // protected methods
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!IsDisposed())
                {
                    _wrapped.DecrementReferenceCount();
                }               
            }
            base.Dispose(disposing);
        }
    }
}
