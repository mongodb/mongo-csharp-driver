/* Copyright 2017-present MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class OperationClock : IOperationClock
    {
        #region static

        public static BsonTimestamp GreaterOperationTime(BsonTimestamp x, BsonTimestamp y)
        {
            if (x == null)
            {
                return y;
            }
            else if (y == null)
            {
                return x;
            }
            else
            {
                return x > y ? x : y;
            }
        }

        #endregion

        private BsonTimestamp _operationTime;

        public BsonTimestamp OperationTime => _operationTime;

        public void AdvanceOperationTime(BsonTimestamp newOperationTime)
        {
            Ensure.IsNotNull(newOperationTime, nameof(newOperationTime));
            _operationTime = GreaterOperationTime(_operationTime, newOperationTime);
        }
    }

    internal sealed class NoOperationClock : IOperationClock
    {
        public BsonTimestamp OperationTime => null;

        public void AdvanceOperationTime(BsonTimestamp newOperationTime)
        {
        }
    }
}
