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

        public static int Read(this Stream stream, byte[] buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            IAsyncResult readOperation;
            try
            {
                using var manualResetEvent = new ManualResetEventSlim();
                readOperation = stream.BeginRead(buffer, offset, count, state =>
                {
                    ((ManualResetEventSlim)state.AsyncState).Set();
                }, manualResetEvent);

                try
                {
                    if (readOperation.IsCompleted || manualResetEvent.Wait(timeout, cancellationToken))
                    {
                        return stream.EndRead(readOperation);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Have to suppress OperationCanceledException here, it will be thrown after the stream will be disposed.
                }
            }
            catch (ObjectDisposedException ex)
            {
                throw new EndOfStreamException("The connection was interrupted.", ex);
            }

            try
            {
                stream.Dispose();
                stream.EndRead(readOperation);
            }
            catch
            {
                // ignore any exceptions
            }

            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException();
        }

        public static async Task<int> ReadAsync(this Stream stream, byte[] buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var timeoutTask = Task.Delay(timeout, cancellationToken);
            var readTask = stream.ReadAsync(buffer, offset, count);

            await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);

            if (!readTask.IsCompleted)
            {
                try
                {
                    stream.Dispose();
                    // should await in the read task to avoid UnobservedTaskException
                    await readTask.ConfigureAwait(false);
                }
                catch
                {
                    // ignore any exceptions
                }

                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException();
            }

            return await readTask.ConfigureAwait(false);
        }

        public static void ReadBytes(this Stream stream, byte[] buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var bytesRead = stream.Read(buffer, offset, count, timeout, cancellationToken);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static void ReadBytes(this Stream stream, IByteBuffer buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var backingBytes = buffer.AccessBackingBytes(offset);
                var bytesToRead = Math.Min(count, backingBytes.Count);
                var bytesRead = stream.Read(backingBytes.Array, backingBytes.Offset, bytesToRead, timeout, cancellationToken);
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

        public static void Write(this Stream stream, byte[] buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            IAsyncResult writeOperation;
            try
            {
                using var manualResetEvent = new ManualResetEventSlim();
                writeOperation = stream.BeginWrite(buffer, offset, count, state =>
                {
                    ((ManualResetEventSlim)state.AsyncState).Set();
                }, manualResetEvent);

                try
                {
                    if (writeOperation.IsCompleted || manualResetEvent.Wait(timeout, cancellationToken))
                    {
                        stream.EndWrite(writeOperation);
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Have to suppress OperationCanceledException here, it will be thrown after the stream will be disposed.
                }
            }
            catch (ObjectDisposedException ex)
            {
                throw new EndOfStreamException("The connection was interrupted.", ex);
            }

            try
            {
                stream.Dispose();
                stream.EndWrite(writeOperation);
            }
            catch
            {
                // ignore any exceptions
            }

            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException();
        }

        public static async Task WriteAsync(this Stream stream, byte[] buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var timeoutTask = Task.Delay(timeout, cancellationToken);
            var writeTask = stream.WriteAsync(buffer, offset, count);

            await Task.WhenAny(writeTask, timeoutTask).ConfigureAwait(false);

            if (!writeTask.IsCompleted)
            {
                try
                {
                    stream.Dispose();
                    await writeTask.ConfigureAwait(false);
                }
                catch
                {
                    // ignore any exceptions
                }

                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException();
            }

            await writeTask.ConfigureAwait(false);
        }

        public static void WriteBytes(this Stream stream, IByteBuffer buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
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
                stream.Write(backingBytes.Array, backingBytes.Offset, bytesToWrite, timeout, cancellationToken); // TODO: honor cancellationToken?
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
