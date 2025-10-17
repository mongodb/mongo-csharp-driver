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

using System.Collections.Generic;
namespace MongoDB.Bson.IO;

/// <summary>
/// Represents a bookmark that can be used to return a reader to the current position and state.
/// </summary>
public class BsonBinaryReaderBookmark : BsonReaderBookmark
{
    private readonly BsonBinaryReaderContext _context;
    private readonly BsonBinaryReaderContext[] _contextArray;
    private readonly long _position;

    internal BsonBinaryReaderBookmark(
        BsonReaderState state,
        BsonType currentBsonType,
        string currentName,
        BsonBinaryReaderContext currentContext,
        Stack<BsonBinaryReaderContext> contextsStack,
        long position)
        : base(state, currentBsonType, currentName)
    {
        _context = currentContext;
        _contextArray = contextsStack.ToArray();
        _position = position;
    }

    internal long Position => _position;

    internal BsonBinaryReaderContext RestoreContext(Stack<BsonBinaryReaderContext> contextStack)
    {
        contextStack.Clear();

        for (var i = _contextArray.Length - 1; i >= 0; i--)
        {
            contextStack.Push(_contextArray[i]);
        }

        return _context;
    }
}
