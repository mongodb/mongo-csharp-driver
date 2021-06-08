/* Copyright 2021-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedCloseCursorOperation<T> : IUnifiedSpecialTestOperation
    {
        private readonly IEnumerator<T> _enumeratorToClose;

        public UnifiedCloseCursorOperation(IEnumerator<T> enumeratorToClose)
        {
            _enumeratorToClose = Ensure.IsNotNull(enumeratorToClose, nameof(enumeratorToClose));
        }

        public void Execute()
        {
            _enumeratorToClose.Dispose();
        }
    }

    public class UnifiedCloseCursorOperationBuilder
    {
        private UnifiedEntityMap _entityMap;

        public UnifiedCloseCursorOperationBuilder(UnifiedEntityMap entityMap) => _entityMap = entityMap;

        public IUnifiedSpecialTestOperation Build(string targetEntityId, BsonDocument operationArguments)
        {
            if (_entityMap.ChangeStreams.TryGetValue(targetEntityId, out var changeStreamEnumerator))
            {
                return new UnifiedCloseCursorOperation<ChangeStreamDocument<BsonDocument>>(changeStreamEnumerator);
            }
            else if (_entityMap.Cursors.TryGetValue(targetEntityId, out var enumerator))
            {
                return new UnifiedCloseCursorOperation<BsonDocument>(enumerator);
            }
            else
            {
                throw new FormatException("No supported enumerator found.");
            }
        }
    }
}
