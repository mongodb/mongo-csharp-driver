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
using System.Text;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a class that has some helper methods for <see cref="Encoding"/>.
    /// </summary>
    internal static class EncodingHelper
    {
        private static readonly ArraySegment<byte> __emptySegment = new ArraySegment<byte>(new byte[0]);

        public static ArraySegment<byte> GetBytesCachedBuffer(this Encoding encoding, string value)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding), "Value cannot be null.");
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null.");
            }

            var length = value.Length;
            if (length == 0)
            {
                return __emptySegment;
            }

            var maxSize = encoding.GetMaxByteCount(length);
            var buffer = BufferCache.GetBuffer(maxSize);

            var size = encoding.GetBytes(value, 0, length, buffer, 0);

            var result = new ArraySegment<byte>(buffer, 0, size);

            return result;
        }
    }
}
