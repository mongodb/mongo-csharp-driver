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

namespace MongoDB.Bson.IO;

internal static class RawBsonSliceHelper
{
    private const int MinimumLength = 5;

    // Returns the length declared by the slice's prefix; bytes past it are not part of the document.
    public static int GetValidatedLength(IByteBuffer slice, string paramName)
    {
        if (slice == null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (slice.Length < MinimumLength)
        {
            throw new ArgumentException($"Raw BSON slice is {slice.Length} bytes but the minimum is {MinimumLength}.", paramName);
        }

        var length = slice.GetByte(0) | (slice.GetByte(1) << 8) | (slice.GetByte(2) << 16) | (slice.GetByte(3) << 24);
        if (length < MinimumLength || length > slice.Length)
        {
            throw new ArgumentException($"Raw BSON slice declares a length of {length} but is {slice.Length} bytes.", paramName);
        }

        if (slice.GetByte(length - 1) != 0)
        {
            throw new ArgumentException("Raw BSON slice is missing its terminating null byte.", paramName);
        }

        return length;
    }
}
