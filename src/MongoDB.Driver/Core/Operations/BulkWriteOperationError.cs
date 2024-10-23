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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class BulkWriteOperationError
    {
        // fields
        private readonly int _code;
        private readonly BsonDocument _details;
        private readonly int _index;
        private readonly string _message;

        // constructors
        public BulkWriteOperationError(int index, int code, string message, BsonDocument details)
        {
            _code = code;
            _details = details;
            _index = index;
            _message = message;
        }

        // properties
        public ServerErrorCategory Category
        {
            get
            {
                switch (_code)
                {
                    case 50:
                        return ServerErrorCategory.ExecutionTimeout;
                    case 11000:
                    case 11001:
                    case 12582:
                        return ServerErrorCategory.DuplicateKey;
                    default:
                        return ServerErrorCategory.Uncategorized;
                }
            }
        }

        public int Code
        {
            get { return _code; }
        }

        public BsonDocument Details
        {
            get { return _details; }
        }

        public int Index
        {
            get { return _index; }
        }

        public string Message
        {
            get { return _message; }
        }

        // methods
        public BulkWriteOperationError WithMappedIndex(IndexMap indexMap)
        {
            var mappedIndex = indexMap.Map(_index);
            return (_index == mappedIndex) ? this : new BulkWriteOperationError(mappedIndex, Code, Message, Details);
        }
    }
}
