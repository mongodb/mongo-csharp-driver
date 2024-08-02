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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Core.Misc
{
    internal static class StreamExtensionMethods
    {
        // static methods
        public static void EfficientCopyTo(this Stream input, Stream output)
        {
            if (input is IStreamEfficientCopyTo efficientCopyToStream)
            {
                efficientCopyToStream.EfficientCopyTo(output);
            }
            else
            {
                input.CopyTo(output); // fallback to standard CopyTo if EfficientCopyTo is not available
            }
        }

        public static async Task<int> ReadAsync(this Stream stream, byte[] buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var state = 1; // 1 == reading, 2 == done reading, 3 == timedout, 4 == cancelled

            var bytesRead = 0;
            using (new Timer(_ => ChangeState(3), null, timeout, Timeout.InfiniteTimeSpan))
            using (cancellationToken.Register(() => ChangeState(4)))
            {
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                    ChangeState(2); // note: might not actually go to state 2 if already in state 3 or 4
                }
                catch when (state == 1)
                {
                    try { stream.Dispose(); } catch { }
                    throw;
                }
                catch when (state >= 3)
                {
                    // a timeout or operation cancelled exception will be thrown instead
                }

                if (state == 3) { throw new TimeoutException(); }
                if (state == 4) { throw new OperationCanceledException(); }
            }

            return bytesRead;

            void ChangeState(int to)
            {
                var from = Interlocked.CompareExchange(ref state, to, 1);
                if (from == 1 && to >= 3)
                {
                    try { stream.Dispose(); } catch { } // disposing the stream aborts the read attempt
                }
            }
        }

        public static void ReadBytes(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var bytesRead = stream.Read(buffer, offset, count); // TODO: honor cancellationToken?
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static void ReadBytes(this Stream stream, IByteBuffer buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var backingBytes = buffer.AccessBackingBytes(offset);
                var bytesToRead = Math.Min(count, backingBytes.Count);
                var bytesRead = stream.Read(backingBytes.Array, backingBytes.Offset, bytesToRead); // TODO: honor cancellationToken?
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static async Task ReadBytesAsync(this Stream stream, byte[] buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var bytesRead = await stream.ReadAsync(buffer, offset, count, timeout, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static async Task ReadBytesAsync(this Stream stream, IByteBuffer buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var backingBytes = buffer.AccessBackingBytes(offset);
                var bytesToRead = Math.Min(count, backingBytes.Count);
                var bytesRead = await stream.ReadAsync(backingBytes.Array, backingBytes.Offset, bytesToRead, timeout, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }


        public static async Task WriteAsync(this Stream stream, byte[] buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var state = 1; // 1 == writing, 2 == done writing, 3 == timedout, 4 == cancelled

            using (new Timer(_ => ChangeState(3), null, timeout, Timeout.InfiniteTimeSpan))
            using (cancellationToken.Register(() => ChangeState(4)))
            {
                try
                {
                    await stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                    ChangeState(2); // note: might not actually go to state 2 if already in state 3 or 4
                }
                catch when (state == 1)
                {
                    try { stream.Dispose(); } catch { }
                    throw;
                }
                catch when (state >= 3)
                {
                    // a timeout or operation cancelled exception will be thrown instead
                }

                if (state == 3) { throw new TimeoutException(); }
                if (state == 4) { throw new OperationCanceledException(); }
            }

            void ChangeState(int to)
            {
                var from = Interlocked.CompareExchange(ref state, to, 1);
                if (from == 1 && to >= 3)
                {
                    try { stream.Dispose(); } catch { } // disposing the stream aborts the write attempt
                }
            }
        }

        public static void WriteBytes(this Stream stream, IByteBuffer buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var backingBytes = buffer.AccessBackingBytes(offset);
                var bytesToWrite = Math.Min(count, backingBytes.Count);
                stream.Write(backingBytes.Array, backingBytes.Offset, bytesToWrite); // TODO: honor cancellationToken?
                offset += bytesToWrite;
                count -= bytesToWrite;
            }
        }

        public static async Task WriteBytesAsync(this Stream stream, IByteBuffer buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var backingBytes = buffer.AccessBackingBytes(offset);
                var bytesToWrite = Math.Min(count, backingBytes.Count);
                await stream.WriteAsync(backingBytes.Array, backingBytes.Offset, bytesToWrite, timeout, cancellationToken).ConfigureAwait(false);
                offset += bytesToWrite;
                count -= bytesToWrite;
            }
        }
    }
}
