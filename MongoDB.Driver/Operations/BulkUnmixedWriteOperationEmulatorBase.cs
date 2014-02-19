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

using System;
using System.Collections.Generic;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal abstract class BulkUnmixedWriteOperationEmulatorBase
    {
        // private fields
        private readonly BulkWriteOperationArgs _args;

        // constructors
        protected BulkUnmixedWriteOperationEmulatorBase(BulkWriteOperationArgs args)
        {
            _args = args;
        }

        // public methods
        public BulkWriteResult Execute(MongoConnection connection)
        {
            var serverInstance = connection.ServerInstance;
            if (!serverInstance.Supports(FeatureId.WriteOpcodes))
            {
                throw new NotSupportedException("Write opcodes are not supported.");
            }

            var batchResults = new List<BulkWriteBatchResult>();
            var remainingRequests = new List<WriteRequest>();
            var hasWriteErrors = false;

            var originalIndex = 0;
            foreach (WriteRequest request in _args.Requests)
            {
                if (hasWriteErrors && _args.IsOrdered)
                {
                    remainingRequests.Add(request);
                    continue;
                }

                var batchResult = EmulateSingleRequest(connection, request, originalIndex);
                batchResults.Add(batchResult);

                hasWriteErrors |= batchResult.HasWriteErrors;
                originalIndex++;
            }

            var combiner = new BulkWriteBatchResultCombiner(batchResults, _args.WriteConcern.Enabled);
            return combiner.CreateResultOrThrowIfHasErrors(remainingRequests);
        }

        // protected methods
        protected abstract BulkWriteBatchResult EmulateSingleRequest(MongoConnection connection, WriteRequest request, int originalIndex);
    }
}
