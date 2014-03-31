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

using MongoDB.Driver.Internal;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Operations
{
    internal class BulkUpdateOperationEmulator : BulkUnmixedWriteOperationEmulatorBase
    {
        // private fields
        private readonly BulkUpdateOperationArgs _args;

        // constructors
        public BulkUpdateOperationEmulator(BulkUpdateOperationArgs args)
            : base(args)
        {
            _args = args;
        }

        // protected methods
        protected override BulkWriteBatchResult EmulateSingleRequest(MongoConnection connection, WriteRequest request, int originalIndex)
        {
            var serverInstance = connection.ServerInstance;
            var updateRequest = (UpdateRequest)request;

            var updateRequests = new[] { updateRequest };
            var operationArgs = new BulkUpdateOperationArgs(
                _args.CheckElementNames,
                _args.CollectionName,
                _args.DatabaseName,
                1, // maxBatchCount
                serverInstance.MaxMessageLength, // maxBatchLength
                true, // isOrdered
                _args.ReaderSettings,
                updateRequests,
                _args.WriteConcern,
                _args.WriterSettings);
            var operation = new UpdateOpcodeOperation(operationArgs);

            WriteConcernResult writeConcernResult;
            WriteConcernException writeConcernException = null;
            try
            {
                writeConcernResult = operation.Execute(connection);
            }
            catch (WriteConcernException ex)
            {
                writeConcernResult = ex.WriteConcernResult;
                writeConcernException = ex;
            }

            var indexMap = new IndexMap.RangeBased(0, originalIndex, 1);
            return BulkWriteBatchResult.Create(
                updateRequest,
                writeConcernResult,
                writeConcernException,
                indexMap);
        }
    }
}
