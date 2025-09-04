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
            try
            {
                using var manualResetEvent = new ManualResetEventSlim();
                var readOperation = stream.BeginRead(
                    buffer,
                    offset,
                    count,
                    state => ((ManualResetEventSlim)state.AsyncState).Set(),
                    manualResetEvent);

                if (readOperation.IsCompleted || manualResetEvent.Wait(timeout, cancellationToken))
                {
                    return stream.EndRead(readOperation);
                }
            }
            catch (OperationCanceledException)
            {
                // Have to suppress OperationCanceledException here, it will be thrown after the stream will be disposed.
            }
            catch (ObjectDisposedException)
            {
                throw new IOException();
            }

            try
            {
                stream.Dispose();
            }
            catch
            {
                // Ignore any exceptions
            }

            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException();
        }

        public static async Task<int> ReadAsync(this Stream stream, byte[] buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Task<int> readTask = null;
            try
            {
                readTask = stream.ReadAsync(buffer, offset, count);
                return await readTask.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // It's possible to get ObjectDisposedException when the connection pool was closed with interruptInUseConnections set to true.
                throw new IOException();
            }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
            {
                // await Task.WaitAsync() throws OperationCanceledException in case of cancellation and TimeoutException in case of timeout
                try
                {
                    stream.Dispose();
                    if (readTask != null)
                    {
                        // Should await on the task to avoid UnobservedTaskException
                        await readTask.ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Ignore any exceptions
                }

                throw;
            }
        }

        public static void ReadBytes(this Stream stream, OperationContext operationContext, byte[] buffer, int offset, int count, TimeSpan socketTimeout)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var bytesRead = stream.Read(buffer, offset, count, operationContext.RemainingTimeoutOrDefault(socketTimeout), operationContext.CancellationToken);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static void ReadBytes(this Stream stream, OperationContext operationContext, IByteBuffer buffer, int offset, int count, TimeSpan socketTimeout)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var backingBytes = buffer.AccessBackingBytes(offset);
                var bytesToRead = Math.Min(count, backingBytes.Count);
                var bytesRead = stream.Read(backingBytes.Array, backingBytes.Offset, bytesToRead, operationContext.RemainingTimeoutOrDefault(socketTimeout), operationContext.CancellationToken);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static void ReadBytes(this Stream stream, byte[] destination, int offset, int count, CancellationToken cancellationToken)
        {
            while (count > 0)
            {
                var bytesRead = stream.Read(destination, offset, count); // TODO: honor cancellationToken?
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static async Task ReadBytesAsync(this Stream stream, OperationContext operationContext, byte[] buffer, int offset, int count, TimeSpan socketTimeout)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var bytesRead = await stream.ReadAsync(buffer, offset, count, operationContext.RemainingTimeoutOrDefault(socketTimeout), operationContext.CancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static async Task ReadBytesAsync(this Stream stream, OperationContext operationContext, IByteBuffer buffer, int offset, int count, TimeSpan socketTimeout)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var backingBytes = buffer.AccessBackingBytes(offset);
                var bytesToRead = Math.Min(count, backingBytes.Count);
                var bytesRead = await stream.ReadAsync(backingBytes.Array, backingBytes.Offset, bytesToRead, operationContext.RemainingTimeoutOrDefault(socketTimeout), operationContext.CancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static async Task ReadBytesAsync(this Stream stream, byte[] destination, int offset, int count, CancellationToken cancellationToken)
        {
            while (count > 0)
            {
                var bytesRead = await stream.ReadAsync(destination, offset, count, cancellationToken).ConfigureAwait(false);
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
            try
            {
                using var manualResetEvent = new ManualResetEventSlim();
                var writeOperation = stream.BeginWrite(
                    buffer,
                    offset,
                    count,
                    state => ((ManualResetEventSlim)state.AsyncState).Set(),
                    manualResetEvent);

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
            catch (ObjectDisposedException)
            {
                // It's possible to get ObjectDisposedException when the connection pool was closed with interruptInUseConnections set to true.
                throw new IOException();
            }

            try
            {
                stream.Dispose();
            }
            catch
            {
                // Ignore any exceptions
            }

            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException();
        }

        public static async Task WriteAsync(this Stream stream, byte[] buffer, int offset, int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Task writeTask = null;
            try
            {
                writeTask = stream.WriteAsync(buffer, offset, count);
                await writeTask.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // It's possible to get ObjectDisposedException when the connection pool was closed with interruptInUseConnections set to true.
                throw new IOException();
            }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
            {
                // await Task.WaitAsync() throws OperationCanceledException in case of cancellation and TimeoutException in case of timeout
                try
                {
                    stream.Dispose();
                    // Should await on the task to avoid UnobservedTaskException
                    if (writeTask != null)
                    {
                        await writeTask.ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Ignore any exceptions
                }

                throw;
            }
        }

        public static void WriteBytes(this Stream stream, OperationContext operationContext, IByteBuffer buffer, int offset, int count, TimeSpan socketTimeout)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var backingBytes = buffer.AccessBackingBytes(offset);
                var bytesToWrite = Math.Min(count, backingBytes.Count);
                stream.Write(backingBytes.Array, backingBytes.Offset, bytesToWrite, operationContext.RemainingTimeoutOrDefault(socketTimeout), operationContext.CancellationToken);
                offset += bytesToWrite;
                count -= bytesToWrite;
            }
        }

        public static async Task WriteBytesAsync(this Stream stream, OperationContext operationContext, IByteBuffer buffer, int offset, int count, TimeSpan socketTimeout)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            while (count > 0)
            {
                var backingBytes = buffer.AccessBackingBytes(offset);
                var bytesToWrite = Math.Min(count, backingBytes.Count);
                await stream.WriteAsync(backingBytes.Array, backingBytes.Offset, bytesToWrite, operationContext.RemainingTimeoutOrDefault(socketTimeout), operationContext.CancellationToken).ConfigureAwait(false);
                offset += bytesToWrite;
                count -= bytesToWrite;
            }
        }
    }
}
