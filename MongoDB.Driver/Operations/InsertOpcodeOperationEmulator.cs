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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal class InsertOpcodeOperationEmulator
    {
        // private fields
        private readonly BulkInsertOperationArgs _args;

        // constructors
        public InsertOpcodeOperationEmulator(BulkInsertOperationArgs args)
        {
            _args = args;
        }

        // public methods
        public IEnumerable<WriteConcernResult> Execute(MongoConnection connection)
        {
            var serverInstance = connection.ServerInstance;
            if (!serverInstance.Supports(FeatureId.WriteCommands))
            {
                throw new NotSupportedException("Write commands are not supported.");
            }

            var operation = new BulkInsertOperation(_args);

            BulkWriteResult bulkWriteResult;
            BulkWriteException bulkWriteException = null;
            try
            {
                bulkWriteResult = operation.Execute(connection);
            }
            catch (BulkWriteException ex)
            {
                bulkWriteResult = ex.Result;
                bulkWriteException = ex;
            }

            var converter = new BulkWriteResultConverter();
            if (bulkWriteException != null)
            {
                throw converter.ToWriteConcernException(bulkWriteException);
            }
            else
            {
                if (_args.WriteConcern.Enabled)
                {
                    return new[] { converter.ToWriteConcernResult(bulkWriteResult) };
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
