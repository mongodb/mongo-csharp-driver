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
    internal class BulkWriteOperationUpsert
    {
        private readonly BsonValue _id;
        private readonly int _index;

        internal BulkWriteOperationUpsert(
            int index,
            BsonValue id)
        {
            _index = index;
            _id = id;
        }

        public BsonValue Id
        {
            get { return _id; }
        }

        public int Index
        {
            get { return _index; }
        }

        internal BulkWriteOperationUpsert WithMappedIndex(IndexMap indexMap)
        {
            var mappedIndex = indexMap.Map(_index);
            return (_index == mappedIndex) ? this : new BulkWriteOperationUpsert(mappedIndex, _id);
        }
    }
}
