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
            return UseStreamWithTimeout(
                stream,
                (str, state) => str.Read(state.buffer, state.offset, state.count),
                (buffer, offset, count),
                timeout,
                cancellationToken);
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
            UseStreamWithTimeout(
                stream,
                (str, state) =>
                {
                    str.Write(state.buffer, state.offset, state.count);
                    return true;
                },
                (buffer, offset, count),
                timeout,
                cancellationToken);
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

        private static TResult UseStreamWithTimeout<TResult, TState>(Stream stream, Func<Stream, TState, TResult> method, TState state, TimeSpan timeout, CancellationToken cancellationToken)
        {
            StreamDisposeCallbackState callbackState = null;
            Timer timer = null;
            CancellationTokenRegistration cancellationSubscription = default;
            if (timeout != Timeout.InfiniteTimeSpan)
            {
                callbackState = new StreamDisposeCallbackState(stream);
                timer = new Timer(DisposeStreamCallback, callbackState, timeout, Timeout.InfiniteTimeSpan);
            }

            if (cancellationToken.CanBeCanceled)
            {
                callbackState ??= new StreamDisposeCallbackState(stream);
                cancellationSubscription = cancellationToken.Register(DisposeStreamCallback, callbackState);
            }

            try
            {
                var result = method(stream, state);
                if (callbackState?.TryChangeState(OperationState.Done) == false)
                {
                    // if cannot change the state - then the stream was/will be disposed, throw here
                    throw new IOException();
                }

                return result;
            }
            catch (IOException)
            {
                if (callbackState?.OperationState == OperationState.Cancelled)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException();
                }

                throw;
            }
            finally
            {
                timer?.Dispose();
                cancellationSubscription.Dispose();
            }

            static void DisposeStreamCallback(object state)
            {
                var disposeCallbackState = (StreamDisposeCallbackState)state;
                if (!disposeCallbackState.TryChangeState(OperationState.Cancelled))
                {
                    // if cannot change the state - then I/O was already succeeded
                    return;
                }

                try
                {
                    disposeCallbackState.Stream.Dispose();
                }
                catch (Exception)
                {
                    // callbacks should not fail, suppress any exceptions here
                }
            }
        }

        private record StreamDisposeCallbackState(Stream Stream)
        {
            private int _operationState = 0;

            public OperationState OperationState
            {
                get => (OperationState)_operationState;
            }

            public bool TryChangeState(OperationState newState) =>
                Interlocked.CompareExchange(ref _operationState, (int)newState, (int)OperationState.InProgress) == (int)OperationState.InProgress;
        }

        private enum OperationState
        {
            InProgress = 0,
            Done,
            Cancelled,
        }
    }
}
