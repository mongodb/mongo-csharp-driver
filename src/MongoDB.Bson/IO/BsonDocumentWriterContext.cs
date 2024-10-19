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
    internal class BsonDocumentWriterContext
    {
        // private fields
        private readonly BsonDocumentWriterContext _parentContext;
        private BsonDocumentWriterContext _cachedPushContext;
        private ContextType _contextType;
        private BsonDocument _document;
        private BsonArray _array;
        private string _code;
        private string _name;

        // constructors
        internal BsonDocumentWriterContext(
            BsonDocumentWriterContext parentContext,
            ContextType contextType,
            BsonDocument document)
        {
            _parentContext = parentContext;
            _contextType = contextType;
            _document = document;
        }

        private BsonDocumentWriterContext(
            BsonDocumentWriterContext parentContext,
            ContextType contextType,
            BsonDocument document,
            BsonArray array,
            string code)
        {
            _parentContext = parentContext;
            _contextType = contextType;
            _document = document;
            _array = array;
            _code = code;
        }

        // internal properties
        internal BsonDocumentWriterContext ParentContext
        {
            get { return _parentContext; }
        }

        internal string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        internal ContextType ContextType
        {
            get { return _contextType; }
        }

        internal BsonDocument Document
        {
            get { return _document; }
        }

        internal BsonArray Array
        {
            get { return _array; }
        }

        internal string Code
        {
            get { return _code; }
        }

        internal BsonDocumentWriterContext PopContext()
        {
            return _parentContext;
        }

        internal BsonDocumentWriterContext PushContext(ContextType contextType, BsonDocument document)
        {
            return PushContext(contextType, document, null, null);
        }

        internal BsonDocumentWriterContext PushContext(ContextType contextType, BsonArray array)
        {
            return PushContext(contextType, null, array, null);
        }

        internal BsonDocumentWriterContext PushContext(ContextType contextType, string code)
        {
            return PushContext(contextType, null, null, code);
        }

        private BsonDocumentWriterContext PushContext(ContextType contextType, BsonDocument document, BsonArray array, string code)
        {
            if (_cachedPushContext == null)
                _cachedPushContext = new BsonDocumentWriterContext(this, contextType, document, array, code);
            else
            {
                _cachedPushContext._contextType = contextType;
                _cachedPushContext._document = document;
                _cachedPushContext._array = array;
                _cachedPushContext._code = code;
                _cachedPushContext._name = null;
            }
            return _cachedPushContext;
        }
    }
}
