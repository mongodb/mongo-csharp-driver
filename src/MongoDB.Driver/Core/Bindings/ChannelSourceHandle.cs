/* Copyright 2013-present MongoDB Inc.
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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    internal sealed class ChannelSourceHandle : IChannelSourceHandle
    {
        // fields
        private bool _disposed;
        private readonly ReferenceCounted<IChannelSource> _reference;

        // constructors
        public ChannelSourceHandle(IChannelSource channelSource)
            : this(new ReferenceCounted<IChannelSource>(channelSource))
        {
        }

        private ChannelSourceHandle(ReferenceCounted<IChannelSource> reference)
        {
            _reference = reference;
        }

        // properties
        public IServer Server
        {
            get { return _reference.Instance.Server; }
        }

        public ServerDescription ServerDescription
        {
            get { return _reference.Instance.ServerDescription; }
        }

        public ICoreSessionHandle Session
        {
            get { return _reference.Instance.Session; }
        }

        // methods
        public IChannelHandle GetChannel(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetChannel(operationContext);
        }

        public Task<IChannelHandle> GetChannelAsync(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetChannelAsync(operationContext);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _reference.DecrementReferenceCount();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public IChannelSourceHandle Fork()
        {
            ThrowIfDisposed();
            _reference.IncrementReferenceCount();
            return new ChannelSourceHandle(_reference);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
