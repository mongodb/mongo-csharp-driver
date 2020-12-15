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
using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class OperationResult
    {
        private IEnumerator<ChangeStreamDocument<BsonDocument>> _changeStream;
        private Exception _exception;
        private BsonValue _result;

        private OperationResult()
        {
        }

        public static OperationResult FromChangeStream(IEnumerator<ChangeStreamDocument<BsonDocument>> changeStream)
        {
            return new OperationResult
            {
                _changeStream = changeStream
            };
        }

        public static OperationResult FromException(Exception exception)
        {
            return new OperationResult
            {
                _exception = exception
            };
        }

        public static OperationResult FromResult(BsonValue result)
        {
            return new OperationResult
            {
                _result = result
            };
        }

        public IEnumerator<ChangeStreamDocument<BsonDocument>> ChangeStream => _changeStream;
        public Exception Exception => _exception;
        public BsonValue Result => _result;
    }
}
