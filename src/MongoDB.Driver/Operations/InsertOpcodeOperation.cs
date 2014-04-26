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
using System.IO;
using System.Linq;
using MongoDB.Bson.IO;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Operations
{
    internal class InsertOpcodeOperation : WriteOpcodeOperationBase
    {
        // private fields
        private readonly BulkInsertOperationArgs _args;
        private readonly bool _continueOnError;

        // constructors
        public InsertOpcodeOperation(BulkInsertOperationArgs args)
            : base(args.DatabaseName, args.CollectionName, args.ReaderSettings, args.WriterSettings, args.WriteConcern)
        {
            _args = args;
            _continueOnError = !_args.IsOrdered;
        }

        // public methods
        public IEnumerable<WriteConcernResult> Execute(MongoConnection connection)
        {
            var serverInstance = connection.ServerInstance;
            if (serverInstance.Supports(FeatureId.WriteCommands) && _args.WriteConcern.Enabled)
            {
                var emulator = new InsertOpcodeOperationEmulator(_args);
                return emulator.Execute(connection);
            }

            var results = WriteConcern.Enabled ? new List<WriteConcernResult>() : null;
            var finalException = (Exception)null;

            var requests = _args.Requests.Cast<InsertRequest>();
            if (_args.AssignId != null)
            {
                requests = requests.Select(r => { _args.AssignId(r); return r; });
            }

            using (var enumerator = requests.GetEnumerator())
            {
                var maxBatchCount = _args.MaxBatchCount; // OP_INSERT is not limited by the MaxBatchCount reported by the server
                var maxBatchLength = Math.Min(_args.MaxBatchLength, connection.ServerInstance.MaxMessageLength);
                var maxDocumentSize = connection.ServerInstance.MaxDocumentSize;

                Batch<InsertRequest> nextBatch = new FirstBatch<InsertRequest>(enumerator);
                while (nextBatch != null)
                {
                    // release buffer as soon as possible
                    BatchProgress<InsertRequest> batchProgress;
                    SendMessageWithWriteConcernResult sendBatchResult;
                    using (var stream = new MemoryStream())
                    {
                        var flags = _continueOnError ? InsertFlags.ContinueOnError : InsertFlags.None;
                        var message = new MongoInsertMessage(
                            _args.WriterSettings,
                            _args.DatabaseName + "." + _args.CollectionName,
                            _args.CheckElementNames,
                            flags,
                            maxBatchCount,
                            maxBatchLength,
                            maxDocumentSize,
                            nextBatch);
                        message.WriteTo(stream); // consumes as much of nextBatch as fits in one message
                        batchProgress = message.BatchProgress;

                        sendBatchResult = SendBatch(connection, stream, message.RequestId, batchProgress.IsLast);
                    }

                    // note: getLastError is sent even when WriteConcern is not enabled if ContinueOnError is false
                    if (sendBatchResult.GetLastErrorRequestId.HasValue)
                    {
                        WriteConcernResult writeConcernResult;
                        try
                        {
                            writeConcernResult = ReadWriteConcernResult(connection, sendBatchResult);
                        }
                        catch (WriteConcernException ex)
                        {
                            writeConcernResult = ex.WriteConcernResult;
                            if (_continueOnError)
                            {
                                finalException = ex;
                            }
                            else if (WriteConcern.Enabled)
                            {
                                results.Add(writeConcernResult);
                                ex.Data["results"] = results;
                                throw;
                            }
                            else
                            {
                                return null;
                            }
                        }

                        if (results != null)
                        {
                            results.Add(writeConcernResult);
                        }
                    }

                    nextBatch = batchProgress.NextBatch;
                }
            }

            if (WriteConcern.Enabled && finalException != null)
            {
                finalException.Data["results"] = results;
                throw finalException;
            }

            return results;
        }

        // private methods
        private SendMessageWithWriteConcernResult SendBatch(MongoConnection connection, Stream stream, int requestId, bool isLast)
        {
            var writeConcern = WriteConcern;
            if (!writeConcern.Enabled && !_continueOnError && !isLast)
            {
                writeConcern = WriteConcern.Acknowledged;
            }
            return SendMessageWithWriteConcern(connection, stream, requestId, ReaderSettings, WriterSettings, writeConcern);
        }
    }
}
