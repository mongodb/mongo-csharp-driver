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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol
{
    internal readonly struct CursorBatch<TDocument>
    {
        // fields
        private readonly long _cursorId;
        private readonly IReadOnlyList<TDocument> _documents;
        private readonly BsonDocument _postBatchResumeToken;

        // constructors
        public CursorBatch(
            long cursorId,
            BsonDocument postBatchResumeToken,
            IReadOnlyList<TDocument> documents)
        {
            _cursorId = cursorId;
            _postBatchResumeToken = postBatchResumeToken;
            _documents = Ensure.IsNotNull(documents, nameof(documents));
        }

        public CursorBatch(long cursorId, IReadOnlyList<TDocument> documents)
            : this(cursorId, null, documents)
        {
        }

        // properties
        public long CursorId
        {
            get { return _cursorId; }
        }

        public IReadOnlyList<TDocument> Documents
        {
            get { return _documents; }
        }

        public BsonDocument PostBatchResumeToken
        {
            get { return _postBatchResumeToken; }
        }
    }
}
