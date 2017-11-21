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

using MongoDB.Bson;

namespace MongoDB.Driver
{
    // used by the DefaultLegacyOperationExecutor when the application uses the deprecated MongoServer, MongoDatabase and MongoCollection constructors
    internal class NoClientSession : IClientSession
    {
        private readonly IServerSession _noServerSession = new NoServerSession();
        private readonly ClientSessionOptions _options = new ClientSessionOptions();

        public IMongoClient Client => null;

        public BsonDocument ClusterTime => null;

        public bool IsImplicit => true;

        public BsonTimestamp OperationTime => null;

        public ClientSessionOptions Options => _options;

        public IServerSession ServerSession => _noServerSession;

        public void AdvanceClusterTime(BsonDocument newClusterTime)
        {
        }

        public void AdvanceOperationTime(BsonTimestamp newOperationTime)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]
        public void Dispose()
        {
        }
    }
}
