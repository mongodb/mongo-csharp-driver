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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Operations
{
    internal static class ReadConcernHelper
    {
        public static void AppendReadConcern(BsonDocument document, ReadConcern readConcern, ConnectionDescription connectionDescription, ICoreSession session)
        {
            var sessionsAreSupported = connectionDescription.IsMasterResult.LogicalSessionTimeout != null;
            var shouldAppendAfterClusterTime = session.IsCausallyConsistent && session.OperationTime != null && sessionsAreSupported;
            var shouldAppendReadConcern = !readConcern.IsServerDefault || shouldAppendAfterClusterTime;

            if (shouldAppendReadConcern)
            {
                var readConcernDocument = readConcern.ToBsonDocument();
                if (shouldAppendAfterClusterTime)
                {
                    readConcernDocument.Add("afterClusterTime", session.OperationTime);
                }
                document.Add("readConcern", readConcernDocument);
            }
        }
    }
}
