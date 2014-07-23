/* Copyright 2013-2014 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Core.Misc
{
    internal static class StreamExtensionMethods
    {
        // static methods
        public static void FillBuffer(this Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                var bytesRead = stream.Read(buffer, offset, count);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static async Task FillBufferAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            while (count > 0)
            {
                var bytesRead = await stream.ReadAsync(buffer, offset, count, cancellationToken);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static async Task FillBufferAsync(this Stream stream, IByteBuffer buffer, int offset, int count, CancellationToken cancellationToken)
        {
            while (count > 0)
            {
                var backingBytes = buffer.AccessBackingBytes(offset);
                var bytesToRead = Math.Min(count, backingBytes.Count);
                var bytesRead = await stream.ReadAsync(backingBytes.Array, backingBytes.Offset, bytesToRead, cancellationToken);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        public static async Task WriteBufferAsync(this Stream stream, IByteBuffer buffer, int offset, int count, CancellationToken cancellationToken)
        {
            while (count > 0)
            {
                var backingBytes = buffer.AccessBackingBytes(offset);
                var bytesToWrite = Math.Min(count, backingBytes.Count - backingBytes.Offset);
                await stream.WriteAsync(backingBytes.Array, backingBytes.Offset, bytesToWrite);
                offset += bytesToWrite;
                count -= bytesToWrite;
            }
        }
    }
}
