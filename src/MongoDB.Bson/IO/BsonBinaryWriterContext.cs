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

namespace MongoDB.Bson.IO
{
    internal class BsonBinaryWriterContext
    {
        // private fields
        private readonly BsonBinaryWriterContext _parentContext;
        private BsonBinaryWriterContext _cachedPushContext;
        private ContextType _contextType;
        private long _startPosition;
        private int _index; // used when contextType is Array

        // constructors
        internal BsonBinaryWriterContext(
            BsonBinaryWriterContext parentContext,
            ContextType contextType,
            long startPosition)
        {
            _parentContext = parentContext;
            _contextType = contextType;
            _startPosition = startPosition;
        }

        // internal properties
        internal BsonBinaryWriterContext ParentContext
        {
            get { return _parentContext; }
        }

        internal ContextType ContextType
        {
            get { return _contextType; }
        }

        internal long StartPosition
        {
            get { return _startPosition; }
        }

        internal int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        internal BsonBinaryWriterContext PopContext()
        {
            return _parentContext;
        }

        internal BsonBinaryWriterContext PushContext(ContextType contextType, long startPosition)
        {
            if (_cachedPushContext == null)
                _cachedPushContext = new BsonBinaryWriterContext(this, contextType, startPosition);
            else
            {
                _cachedPushContext._contextType = contextType;
                _cachedPushContext._startPosition = startPosition;
                _cachedPushContext._index = 0;
            }
            return _cachedPushContext;
        }
    }
}
