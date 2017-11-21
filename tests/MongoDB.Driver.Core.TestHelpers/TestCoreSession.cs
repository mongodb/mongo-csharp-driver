/* Copyright 2017 MongoDB Inc.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core
{
    public class TestCoreSession : ICoreSession
    {
        #region static
        public static BsonDocument GenerateSessionId()
        {
            var guid = Guid.NewGuid();
            var id = new BsonBinaryData(guid, GuidRepresentation.Standard);
            return new BsonDocument("id", id);
        }

        public static ICoreSessionHandle NewHandle()
        {
            var session = new TestCoreSession();
            return new CoreSessionHandle(session);
        }
        #endregion

        private readonly IClusterClock _clusterClock = new ClusterClock();
        private readonly BsonDocument _id = GenerateSessionId();
        private readonly IOperationClock _operationClock = new OperationClock();

        public BsonDocument ClusterTime => _clusterClock.ClusterTime;

        public BsonDocument Id => _id;

        public bool IsCausallyConsistent => false;

        public bool IsImplicit => false;

        public BsonTimestamp OperationTime => _operationClock.OperationTime;

        public void AdvanceClusterTime(BsonDocument newClusterTime)
        {
            _clusterClock.AdvanceClusterTime(newClusterTime);
        }

        public void AdvanceOperationTime(BsonTimestamp newOperationTime)
        {
            _operationClock.AdvanceOperationTime(newOperationTime);
        }

        public void Dispose()
        {
        }

        public void WasUsed()
        {
        }
    }
}
