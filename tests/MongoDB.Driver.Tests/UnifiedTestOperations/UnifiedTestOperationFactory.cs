/* Copyright 2010-present MongoDB Inc.
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

        public IUnifiedTestOperation CreateOperation(string operationName, string targetEntityId, BsonDocument operationArguments) =>
            targetEntityId switch
            {
                "testRunner" => operationName switch
                {
                    "assertCollectionExists" => new UnifiedAssertCollectionExistsOperationBuilder().Build(operationArguments),
                    "assertCollectionNotExists" => new UnifiedAssertCollectionNotExistsOperationBuilder().Build(operationArguments),
                    "assertDifferentLsidOnLastTwoCommands" => new UnifiedAssertDifferentLsidOnLastTwoCommandsOperationBuilder(_entityMap).Build(operationArguments),
                    "assertEventCount" => new UnifiedAssertEventCountOperationBuilder(_entityMap).Build(operationArguments),
                    "assertIndexExists" => new UnifiedAssertIndexExistsOperationBuilder().Build(operationArguments),
                    "assertIndexNotExists" => new UnifiedAssertIndexNotExistsOperationBuilder().Build(operationArguments),
                    "assertNumberConnectionsCheckedOut" => new UnifiedAssertNumberConnectionsCheckedOutOperationBuilder(_entityMap).Build(operationArguments),
                    "assertSameLsidOnLastTwoCommands" => new UnifiedAssertSameLsidOnLastTwoCommandsOperationBuilder(_entityMap).Build(operationArguments),
                    "assertSessionDirty" => new UnifiedAssertSessionDirtyOperationBuilder(_entityMap).Build(operationArguments),
                    "assertSessionNotDirty" => new UnifiedAssertSessionNotDirtyOperationBuilder(_entityMap).Build(operationArguments),
                    "assertSessionPinned" => new UnifiedAssertSessionPinnedOperationBuilder(_entityMap).Build(operationArguments),
                    "assertSessionTransactionState" => new UnifiedAssertSessionTransactionStateOperationBuilder(_entityMap).Build(operationArguments),
                    "assertSessionUnpinned" => new UnifiedAssertSessionUnpinnedOperationBuilder(_entityMap).Build(operationArguments),
                    "assertTopologyType" => new UnifiedAssertTopologyTypeOperationBuilder(_entityMap).Build(operationArguments),
                    "createEntities" => new UnifiedCreateEntitiesOperationBuilder(_entityMap).Build(operationArguments),
                    "failPoint" => new UnifiedFailPointOperationBuilder(_entityMap).Build(operationArguments),
                    "loop" => new UnifiedLoopOperationBuilder(_entityMap, _additionalArgs).Build(operationArguments),
                    "recordTopologyDescription" => new UnifiedRecordTopologyDescriptionOperationBuilder(_entityMap).Build(operationArguments),
                    "runOnThread" => new UnifiedRunOnThreadOperationBuilder(_entityMap).Build(operationArguments),
                    "targetedFailPoint" => new UnifiedTargetedFailPointOperationBuilder(_entityMap).Build(operationArguments),
                    "wait" => new UnifiedWaitOperationBuilder(_entityMap).Build(operationArguments),
                    "waitForEvent" => new UnifiedWaitForEventOperationBuilder(_entityMap).Build(operationArguments),
                    "waitForThread" => new UnifiedWaitForThreadOperationBuilder(_entityMap).Build(operationArguments),
                    "waitForPrimaryChange" => new UnifiedWaitForPrimaryChangeOperationBuilder(_entityMap).Build(operationArguments),
                    _ => throw new FormatException($"Invalid method name: '{operationName}'."),
                },
                _ when _entityMap.Buckets.ContainsKey(targetEntityId) => operationName switch
                {
                    "delete" => new UnifiedGridFsDeleteOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "download" => new UnifiedGridFsDownloadOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "downloadByName" => new UnifiedGridFsDownloadByNameOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "upload" => new UnifiedGridFsUploadOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    _ => throw new FormatException($"Invalid method name: '{operationName}'."),
                },
                _ when _entityMap.ChangeStreams.ContainsKey(targetEntityId) || _entityMap.Cursors.ContainsKey(targetEntityId) => operationName switch
                {
                    "iterateUntilDocumentOrError" => new UnifiedIterateUntilDocumentOrErrorOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "close" => new UnifiedCloseCursorOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    _ => throw new FormatException($"Invalid method name: '{operationName}'."),
                },
                _ when _entityMap.Clients.ContainsKey(targetEntityId) => operationName switch
                {
                    "clientBulkWrite" => new UnifiedClientBulkWriteOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "close" => new UnifiedCloseClientOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "createChangeStream" => new UnifiedCreateChangeStreamOnClientOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "listDatabases" => new UnifiedListDatabasesOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "listDatabaseNames" => new UnifiedListDatabaseNamesOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    _ => throw new FormatException($"Invalid method name: '{operationName}'."),
                },
                _ when _entityMap.Collections.ContainsKey(targetEntityId) => operationName switch
                {
                    "aggregate" => new UnifiedAggregateOperationBuilder(_entityMap).BuildCollectionOperation(targetEntityId, operationArguments),
                    "bulkWrite" => new UnifiedBulkWriteOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "count" => new UnifiedCountOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "countDocuments" => new UnifiedCountDocumentsOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "createChangeStream" => new UnifiedCreateChangeStreamOnCollectionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "createFindCursor" => new UnifiedCreateFindCursorOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "createIndex" => new UnifiedCreateIndexOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "createSearchIndex" => new UnifiedCreateSearchIndexOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "createSearchIndexes" => new UnifiedCreateSearchIndexesOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "deleteMany" => new UnifiedDeleteManyOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "deleteOne" => new UnifiedDeleteOneOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "distinct" => new UnifiedDistinctOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "dropIndex" => new UnifiedDropIndexOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "dropIndexes" => new UnifiedDropIndexesOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "dropSearchIndex" => new UnifiedDropSearchIndexOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "estimatedDocumentCount" => new UnifiedEstimatedDocumentCountOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "find" => new UnifiedFindOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "findOneAndDelete" => new UnifiedFindOneAndDeleteOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "findOneAndReplace" => new UnifiedFindOneAndReplaceOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "findOneAndUpdate" => new UnifiedFindOneAndUpdateOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "insertMany" => new UnifiedInsertManyOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "insertOne" => new UnifiedInsertOneOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "listIndexes" => new UnifiedListIndexesOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "listSearchIndexes" => new UnifiedListSearchIndexesOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "mapReduce" => new UnifiedMapReduceOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "rename" => new UnifiedRenameCollectionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "replaceOne" => new UnifiedReplaceOneOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "updateMany" => new UnifiedUpdateManyOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "updateOne" => new UnifiedUpdateOneOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "updateSearchIndex" => new UnifiedUpdateSearchIndexesOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    _ => throw new FormatException($"Invalid method name: '{operationName}'."),
                },
                _ when _entityMap.Databases.ContainsKey(targetEntityId) => operationName switch
                {
                    "aggregate" => new UnifiedAggregateOperationBuilder(_entityMap).BuildDatabaseOperation(targetEntityId, operationArguments),
                    "createCollection" => new UnifiedCreateCollectionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "createChangeStream" => new UnifiedCreateChangeStreamOnDatabaseOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "dropCollection" => new UnifiedDropCollectionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "listCollections" => new UnifiedListCollectionsOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "listCollectionNames" => new UnifiedListCollectionNamesOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "runCommand" => new UnifiedRunCommandOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    _ => throw new FormatException($"Invalid method name: '{operationName}'."),
                },
                _ when _entityMap.Sessions.ContainsKey(targetEntityId) => operationName switch
                {
                    "abortTransaction" => new UnifiedAbortTransactionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "commitTransaction" => new UnifiedCommitTransactionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "endSession" => new UnifiedEndSessionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "startTransaction" => new UnifiedStartTransactionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "withTransaction" => new UnifiedWithTransactionOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    _ => throw new FormatException($"Invalid method name: '{operationName}'."),
                },
                _ when _entityMap.ClientEncryptions.ContainsKey(targetEntityId) => operationName switch
                {
                    "createDataKey" => new UnifiedCreateDataKeyOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "rewrapManyDataKey" => new UnifiedRewrapManyDataKeyOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "encrypt" => new UnifiedEncryptOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "decrypt" => new UnifiedDecryptOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "addKeyAltName" => new UnifiedAddKeyAltNameOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "deleteKey" => new UnifiedDeleteKeyOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "getKey" => new UnifiedGetKeyOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "getKeys" => new UnifiedGetKeysOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "getKeyByAltName" => new UnifiedGetKeyByAltNameOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    "removeKeyAltName" => new UnifiedRemoveKeyAltNameOperationBuilder(_entityMap).Build(targetEntityId, operationArguments),
                    _ => throw new FormatException($"Invalid method name: '{operationName}'."),
                },
                _ => throw new FormatException($"Target entity type not recognized for entity with id: '{targetEntityId}'."),
            };
    }
}
