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

using System.Linq;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Operations
{
    internal class BulkInsertOperationEmulator : BulkUnmixedWriteOperationEmulatorBase
    {
        // private fields
        private readonly BulkInsertOperationArgs _args;

        // constructors
        public BulkInsertOperationEmulator(BulkInsertOperationArgs args)
            : base(args)
        {
            _args = args;
        }

        // protected methods
        protected override BulkWriteBatchResult EmulateSingleRequest(MongoConnection connection, WriteRequest request, int originalIndex)
        {
            var serverInstance = connection.ServerInstance;
            var insertRequest = (InsertRequest)request;

            var insertRequests = new[] { insertRequest };
            var operationArgs = new BulkInsertOperationArgs(
                _args.AssignId,
                _args.CheckElementNames,
                _args.CollectionName,
                _args.DatabaseName,
                1, // maxBatchCount
                serverInstance.MaxMessageLength, // maxBatchLength
                true, // isOrdered
                _args.ReaderSettings,
                insertRequests,
                _args.WriteConcern,
                _args.WriterSettings);
            var operation = new InsertOpcodeOperation(operationArgs);

            WriteConcernResult writeConcernResult = null;
            WriteConcernException writeConcernException = null;
            try
            {
                var operationResult = operation.Execute(connection);
                if (operationResult != null)
                {
                    writeConcernResult = operationResult.First();
                }
            }
            catch (WriteConcernException ex)
            {
                writeConcernResult = ex.WriteConcernResult;
                writeConcernException = ex;
            }

            var indexMap = new IndexMap.RangeBased(0, originalIndex, 1);
            return BulkWriteBatchResult.Create(
                insertRequest,
                writeConcernResult,
                writeConcernException,
                indexMap);
        }
    }
}
