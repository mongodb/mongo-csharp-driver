/* Copyright 2010-present MongoDB Inc.
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

        public static void ReadBytes(this Stream stream, byte[] buffer, int offset, int count, int timeoutMs = Timeout.Infinite, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            ExecuteOperationWithTimeout(
                stream,
                (buffer, offset, count),
                (currentStream, state) =>
                {
                    var bytesRead = 0;
                    var remainingBytes = state.count;
                    while (remainingBytes > 0)
                    {
                        var readResult = currentStream.Read(state.buffer, state.offset + bytesRead, remainingBytes);
                        if (readResult == 0)
                        {
                            throw new EndOfStreamException();
                        }

                        bytesRead += readResult;
                        remainingBytes -= readResult;
                    }
                },
                timeoutMs,
                cancellationToken);
        }

        public static void ReadBytes(this Stream stream, IByteBuffer buffer, int offset, int count, int timeoutMs = Timeout.Infinite, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            ExecuteOperationWithTimeout(
                stream,
                (buffer, offset, count),
                (currentStream, state) =>
                {
                    var bytesRead = 0;
                    var remainingBytes = state.count;
                    while (remainingBytes > 0)
                    {
                        var backingBytes = state.buffer.AccessBackingBytes(state.offset + bytesRead);
                        var bytesToRead = Math.Min(remainingBytes, backingBytes.Count);
                        var readResult = currentStream.Read(backingBytes.Array, backingBytes.Offset, bytesToRead);
                        if (readResult == 0)
                        {
                            throw new EndOfStreamException();
                        }

                        bytesRead += readResult;
                        remainingBytes -= readResult;
                    }
                },
                timeoutMs,
                cancellationToken);
        }

        public static Task ReadBytesAsync(this Stream stream, byte[] buffer, int offset, int count, int timeoutMs = Timeout.Infinite, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            return ExecuteOperationWithTimeoutAsync(
                stream,
                (buffer, offset, count),
                async (currentStream, state) =>
                {
                    var bytesRead = 0;
                    var remainingBytes = state.count;
                    while (bytesRead < state.count)
                    {
                        var readResult = await currentStream.ReadAsync(state.buffer, state.offset + bytesRead, remainingBytes).ConfigureAwait(false);
                        if (readResult == 0)
                        {
                            throw new EndOfStreamException();
                        }

                        bytesRead += readResult;
                        remainingBytes -= readResult;
                    }
                },
                timeoutMs,
                cancellationToken
            );
        }

        public static Task ReadBytesAsync(this Stream stream, IByteBuffer buffer, int offset, int count, int timeoutMs = Timeout.Infinite, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            return ExecuteOperationWithTimeoutAsync(
                stream,
                (buffer, offset, count),
                async (currentStream, state) =>
                {
                    var bytesRead = 0;
                    var remainingBytes = state.count;
                    while (remainingBytes > 0)
                    {
                        var backingBytes = state.buffer.AccessBackingBytes(state.offset + bytesRead);
                        var bytesToRead = Math.Min(remainingBytes, backingBytes.Count);
                        var readResult = await currentStream.ReadAsync(backingBytes.Array, backingBytes.Offset, bytesToRead).ConfigureAwait(false);
                        if (readResult == 0)
                        {
                            throw new EndOfStreamException();
                        }

                        bytesRead += readResult;
                        remainingBytes -= readResult;
                    }
                },
                timeoutMs,
                cancellationToken);
        }

        public static void WriteBytes(this Stream stream, byte[] buffer, int offset, int count, int timeoutMs = Timeout.Infinite, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            ExecuteOperationWithTimeout(
                stream,
                (buffer, offset, count),
                (currentStream, state) => currentStream.Write(state.buffer, state.offset, state.count),
                timeoutMs,
                cancellationToken);
        }

        public static void WriteBytes(this Stream stream, IByteBuffer buffer, int offset, int count, int timeoutMs = Timeout.Infinite, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            ExecuteOperationWithTimeout(
                stream,
                (buffer, offset, count),
                (currentStream, state) =>
                {
                    var bytesWritten = 0;
                    var remainingBytes = state.count;
                    while (remainingBytes > 0)
                    {
                        var backingBytes = state.buffer.AccessBackingBytes(state.offset + bytesWritten);
                        var bytesToWrite = Math.Min(remainingBytes, backingBytes.Count);
                        currentStream.Write(backingBytes.Array, backingBytes.Offset, bytesToWrite);
                        bytesWritten += bytesToWrite;
                        remainingBytes -= bytesToWrite;
                    }
                },
                timeoutMs,
                cancellationToken);
        }

        public static Task WriteBytesAsync(this Stream stream, byte[] buffer, int offset, int count, int timeoutMs = Timeout.Infinite, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            return ExecuteOperationWithTimeoutAsync(
                stream,
                (buffer, offset, count),
                (currentStream, state) => currentStream.WriteAsync(state.buffer, state.offset, state.count),
                timeoutMs,
                cancellationToken);
        }

        public static Task WriteBytesAsync(this Stream stream, IByteBuffer buffer, int offset, int count, int timeoutMs = Timeout.Infinite, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(stream, nameof(stream));
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));

            return ExecuteOperationWithTimeoutAsync(
                stream,
                (buffer, offset, count),
                async (currentStream, state) =>
                {
                    var bytesWritten = 0;
                    var remainingBytes = state.count;
                    while (remainingBytes > 0)
                    {
                        var backingBytes = state.buffer.AccessBackingBytes(state.offset + bytesWritten);
                        var bytesToWrite = Math.Min(remainingBytes, backingBytes.Count);
                        await currentStream.WriteAsync(backingBytes.Array, backingBytes.Offset, bytesToWrite).ConfigureAwait(false);
                        bytesWritten += bytesToWrite;
                        remainingBytes -= bytesToWrite;
                    }
                },
                timeoutMs,
                cancellationToken);
        }

        private static async Task ExecuteOperationWithTimeoutAsync<TState>(Stream stream, TState state, Func<Stream, TState, Task> operation, int timeoutMs, CancellationToken cancellationToken)
        {
            if (timeoutMs == 0)
            {
                throw new TimeoutException();
            }

            var timeout = TimeSpan.FromMilliseconds(timeoutMs);
            var operationTask = operation(stream, state);

            try
            {
                await operationTask.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is TaskCanceledException or TimeoutException)
            {
                operationTask.IgnoreExceptions();
                try
                {
                    stream.Dispose();
                }
                catch (Exception)
                {
                    // suppress any exception
                }

                throw;
            }
        }

        private static void ExecuteOperationWithTimeout<TState>(Stream stream, TState state, Action<Stream, TState> operation, int timeoutMs, CancellationToken cancellationToken)
        {
            if (timeoutMs == 0)
            {
                throw new TimeoutException();
            }

            StreamDisposeCallbackState callbackState = null;
            Timer timer = null;
            CancellationTokenRegistration cancellationSubscription = default;
            if (timeoutMs > 0)
            {
                callbackState = new StreamDisposeCallbackState(stream);
                timer = new Timer(DisposeStreamCallback, callbackState, timeoutMs, Timeout.Infinite);
            }

            if (cancellationToken.CanBeCanceled)
            {
                callbackState ??= new StreamDisposeCallbackState(stream);
                cancellationSubscription = cancellationToken.Register(DisposeStreamCallback, callbackState);
            }

            try
            {
                operation(stream, state);
                if (callbackState?.TryChangeStateFromInProgress(OperationState.Done) == false)
                {
                    // If the state can't be changed - then the stream was/will be disposed, throw here
                    throw new IOException();
                }
            }
            catch (Exception ex)
            {
                if (callbackState?.OperationState == OperationState.Interrupted)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException();
                }

                if (ex is ObjectDisposedException)
                {
                    throw new IOException();
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
                if (!disposeCallbackState.TryChangeStateFromInProgress(OperationState.Interrupted))
                {
                    // If the state can't be changed - then I/O had already succeeded
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
            private int _operationState = (int)OperationState.InProgress;

            public OperationState OperationState => (OperationState)_operationState;

            public bool TryChangeStateFromInProgress(OperationState newState) =>
                Interlocked.CompareExchange(ref _operationState, (int)newState, (int)OperationState.InProgress) == (int)OperationState.InProgress;
        }

        private enum OperationState
        {
            InProgress = 0,
            Done,
            Interrupted,
        }
    }
}
