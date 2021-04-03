/* Copyright 2020-present MongoDB Inc.
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
using System.Threading;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedTestOperationFactory
    {
        private readonly UnifiedEntityMap _entityMap;
        private readonly Dictionary<string, object> _additionalArgs;

        public UnifiedTestOperationFactory(UnifiedEntityMap entityMap, Dictionary<string, object> additionalArgs)
        {
            _entityMap = entityMap;
            _additionalArgs = additionalArgs; // can be null
        }

        public IUnifiedTestOperation CreateOperation(string operationName, string targetEntityId, BsonDocument operationArguments)
        {
            switch (targetEntityId)
            {
                case "testRunner":
                    switch (operationName)
                    {
                        case "assertCollectionExists":
                            return new UnifiedAssertCollectionExistsOperationBuilder().Build(operationArguments);
                        case "assertCollectionNotExists":
                            return new UnifiedAssertCollectionNotExistsOperationBuilder().Build(operationArguments);
                        case "assertDifferentLsidOnLastTwoCommands":
                            return new UnifiedAssertDifferentLsidOnLastTwoCommandsOperationBuilder(_entityMap).Build(operationArguments);
                        case "assertIndexExists":
                            return new UnifiedAssertIndexExistsOperationBuilder().Build(operationArguments);
                        case "assertIndexNotExists":
                            return new UnifiedAssertIndexNotExistsOperationBuilder().Build(operationArguments);
                        case "assertSameLsidOnLastTwoCommands":
                            return new UnifiedAssertSameLsidOnLastTwoCommandsOperationBuilder(_entityMap).Build(operationArguments);
                        case "assertSessionDirty":
                            return new UnifiedAssertSessionDirtyOperationBuilder(_entityMap).Build(operationArguments);
                        case "assertSessionNotDirty":
                            return new UnifiedAssertSessionNotDirtyOperationBuilder(_entityMap).Build(operationArguments);
                        case "assertSessionPinned":
                            return new UnifiedAssertSessionPinnedOperationBuilder(_entityMap).Build(operationArguments);
                        case "assertSessionTransactionState":
                            return new UnifiedAssertSessionTransactionStateOperationBuilder(_entityMap).Build(operationArguments);
                        case "assertSessionUnpinned":
                            return new UnifiedAssertSessionUnpinnedOperationBuilder(_entityMap).Build(operationArguments);
                        case "failPoint":
                            return new UnifiedFailPointOperationBuilder(_entityMap).Build(operationArguments);
                        case "loop":
                            return new UnifiedLoopOperationBuilder(_entityMap, _additionalArgs).Build(operationArguments);
                        case "targetedFailPoint":
                            return new UnifiedTargetedFailPointOperationBuilder(_entityMap).Build(operationArguments);
                        default:
                            throw new FormatException($"Invalid method name: '{operationName}'.");
                    }

                case var _ when _entityMap.HasBucket(targetEntityId):
                    switch (operationName)
                    {
                        case "delete":
                            return new UnifiedGridFsDeleteOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "download":
                            return new UnifiedGridFsDownloadOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "upload":
                            return new UnifiedGridFsUploadOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        default:
                            throw new FormatException($"Invalid method name: '{operationName}'.");
                    }

                case var _ when _entityMap.HasChangeStream(targetEntityId):
                    switch (operationName)
                    {
                        case "iterateUntilDocumentOrError":
                            return new UnifiedIterateUntilDocumentOrErrorOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        default:
                            throw new FormatException($"Invalid method name: '{operationName}'.");
                    }

                case var _ when _entityMap.HasClient(targetEntityId):
                    switch (operationName)
                    {
                        case "createChangeStream":
                            return new UnifiedCreateChangeStreamOnClientOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "listDatabases":
                            return new UnifiedListDatabasesOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        default:
                            throw new FormatException($"Invalid method name: '{operationName}'.");
                    }

                case var _ when _entityMap.HasCollection(targetEntityId):
                    switch (operationName)
                    {
                        case "aggregate":
                            return new UnifiedAggregateOnCollectionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "bulkWrite":
                            return new UnifiedBulkWriteOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "countDocuments":
                            return new UnifiedCountDocumentsOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "createChangeStream":
                            return new UnifiedCreateChangeStreamOnCollectionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "createIndex":
                            return new UnifiedCreateIndexOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "deleteMany":
                            return new UnifiedDeleteManyOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "deleteOne":
                            return new UnifiedDeleteOneOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "distinct":
                            return new UnifiedDistinctOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "estimatedDocumentCount":
                            return new UnifiedEstimatedDocumentCountOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "find":
                            return new UnifiedFindOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "findOneAndDelete":
                            return new UnifiedFindOneAndDeleteOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "findOneAndReplace":
                            return new UnifiedFindOneAndReplaceOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "findOneAndUpdate":
                            return new UnifiedFindOneAndUpdateOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "insertMany":
                            return new UnifiedInsertManyOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "insertOne":
                            return new UnifiedInsertOneOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "replaceOne":
                            return new UnifiedReplaceOneOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "updateMany":
                            return new UnifiedUpdateManyOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "updateOne":
                            return new UnifiedUpdateOneOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        default:
                            throw new FormatException($"Invalid method name: '{operationName}'.");
                    }

                case var _ when _entityMap.HasDatabase(targetEntityId):
                    switch (operationName)
                    {
                        case "aggregate":
                            return new UnifiedAggregateOnDatabaseOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "createCollection":
                            return new UnifiedCreateCollectionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "dropCollection":
                            return new UnifiedDropCollectionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "runCommand":
                            return new UnifiedRunCommandOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        default:
                            throw new FormatException($"Invalid method name: '{operationName}'.");
                    }

                case var _ when _entityMap.HasSession(targetEntityId):
                    switch (operationName)
                    {
                        case "abortTransaction":
                            return new UnifiedAbortTransactionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "commitTransaction":
                            return new UnifiedCommitTransactionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "endSession":
                            return new UnifiedEndSessionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "startTransaction":
                            return new UnifiedStartTransactionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        case "withTransaction":
                            return new UnifiedWithTransactionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments);
                        default:
                            throw new FormatException($"Invalid method name: '{operationName}'.");
                    }

                default:
                    throw new FormatException($"Target entity type not recognized for entity with id: '{targetEntityId}'.");
            }
        }
    }
}
