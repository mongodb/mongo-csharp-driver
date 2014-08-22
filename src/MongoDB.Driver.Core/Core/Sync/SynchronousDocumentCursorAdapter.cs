/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core.Sync
{
    public class SynchronousDocumentCursorAdapter<TDocument>
    {
        // fields
        private readonly Cursor<TDocument> _cursor;

        // constructor
        public SynchronousDocumentCursorAdapter(Cursor<TDocument> cursor)
        {
            _cursor = Ensure.IsNotNull(cursor, "cursor");
        }

        // methods
        public IEnumerator<TDocument> GetEnumerator()
        {
            while (_cursor.MoveNextAsync().GetAwaiter().GetResult())
            {
                var batch = _cursor.Current;
                foreach (var document in batch)
                {
                    yield return document;
                }
            }
        }
    }
}
