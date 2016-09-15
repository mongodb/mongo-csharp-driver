/* Copyright 2015-2016 MongoDB Inc.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Helpers
{
    public class BlockingMemoryStream : MemoryStream
    {
        private readonly object _lock = new object();
        private readonly TimeSpan _spinWaitTimeout;

        public BlockingMemoryStream()
        {
            _spinWaitTimeout = TimeSpan.FromSeconds(1);
        }

        public BlockingMemoryStream(TimeSpan spinWaitTimeout)
        {
            _spinWaitTimeout = spinWaitTimeout;
        }

        public override long Length
        {
            get
            {
                lock (_lock)
                {
                    return base.Length;
                }
            }
        }

        public object Lock
        {
            get { return _lock; }
        }

        public override long Position
        {
            get
            {
                lock (_lock)
                {
                    return base.Position;
                }
            }

            set
            {
                lock (_lock)
                {
                    base.Position = value;
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            SpinWait.SpinUntil(() => Length - Position >= count, _spinWaitTimeout);
            lock (_lock)
            {
                return base.Read(buffer, offset, count);
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Run(() => { SpinWait.SpinUntil(() => Length - Position >= count, _spinWaitTimeout); });

            lock (_lock)
            {
                return base.Read(buffer, offset, count); // Read, not ReadAsync
            }
        }

        public override int ReadByte()
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                base.Write(buffer, offset, count);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                base.Write(buffer, offset, count); // Write, not WriteAsync
            }
            return Task.FromResult(true);
        }

        public override void WriteByte(byte value)
        {
            lock (_lock)
            {
                base.WriteByte(value);
            }
        }
    }
}