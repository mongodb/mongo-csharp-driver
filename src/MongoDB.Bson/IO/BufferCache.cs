/* Copyright 2020-present MongoDB Inc.
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
using System.Threading;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a class that provides reusable buffer per thread.
    /// </summary>
    internal static class BufferCache
    {
        private const int MinSize = 16;
        private const int MaxSize = 8192;
        private const int MaxThreads = 1024;

        // private static fields
        [ThreadStatic]
        private static byte[] __buffer;

        private static int __buffersCount = 0;

        public static byte[] GetBuffer(int size)
        {
            if (size > MaxSize ||
                __buffer == null && Interlocked.Increment(ref __buffersCount) >= MaxThreads)
            {
                return new byte[size];
            }

            if (!(__buffer?.Length >= size))
            {
                var newSize = size <= MinSize ? MinSize : CeilPower2(size);
                __buffer = new byte[newSize];
            }

            return __buffer;
        }

        // private methods
        private static int CeilPower2(int number)
        {
            number--;
            number |= number >> 1;
            number |= number >> 2;
            number |= number >> 4;
            number |= number >> 8;
            number |= number >> 16;
            number++;

            return number;
        }
    }
}
