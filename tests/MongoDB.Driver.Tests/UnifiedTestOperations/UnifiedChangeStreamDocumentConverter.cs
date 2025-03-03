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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedChangeStreamDocumentConverter
    {
        private static readonly string[] __changeStreamFields = ["_id", "clusterTime", "documentKey", "fullDocument", "fullDocumentBeforeChange", "ns", "operationDescription", "operationType", "to", "splitEvent", "collectionUUID", "updateDescription", "wallTime", "nsType"];
        private static readonly string[] __nsFields = ["db", "coll"];

        public static BsonDocument Convert(ChangeStreamDocument<BsonDocument> changeStreamDocument)
        {
            var result = new BsonDocument()
            {
                { "_id", changeStreamDocument.ResumeToken },
                { "clusterTime", changeStreamDocument.ClusterTime, changeStreamDocument.ClusterTime != null },
                { "documentKey", changeStreamDocument.DocumentKey, changeStreamDocument.DocumentKey != null },
                { "nsType", () => changeStreamDocument.NamespaceType.ToString().ToLower(), changeStreamDocument.NamespaceType != ChangeStreamNamespaceType.Unknown },
                { "operationDescription", changeStreamDocument.OperationDescription, changeStreamDocument.OperationDescription != null },
                { "to", () => SerializationHelper.SerializeValue(ChangeStreamDocumentCollectionNamespaceSerializer.Instance, changeStreamDocument.RenameTo), changeStreamDocument.RenameTo != null },
                { "splitEvent", () => SerializationHelper.SerializeValue(ChangeStreamSplitEventSerializer.Instance, changeStreamDocument.SplitEvent), changeStreamDocument.SplitEvent != null },
                { "collectionUUID", () => SerializationHelper.SerializeValue(GuidSerializer.StandardInstance, changeStreamDocument.CollectionUuid), changeStreamDocument.CollectionUuid != null },
                { "updateDescription", () => SerializationHelper.SerializeValue(ChangeStreamUpdateDescriptionSerializer.Instance, changeStreamDocument.UpdateDescription), changeStreamDocument.UpdateDescription != null },
                { "wallTime", () => SerializationHelper.SerializeValue(DateTimeSerializer.UtcInstance, changeStreamDocument.WallTime), changeStreamDocument.WallTime != null }
            };
            AppendDocumentFieldValue(result, changeStreamDocument, "fullDocument");
            AppendDocumentFieldValue(result, changeStreamDocument, "fullDocumentBeforeChange");
            AppendNsFieldValue(result, changeStreamDocument);
            AppendOperationTypeFieldValue(result, changeStreamDocument);

            // map the rest of change stream document
            result.AddRange(changeStreamDocument.BackingDocument.Elements.Where(e => !__changeStreamFields.Contains(e.Name)));

            return result;
        }

        private static void AppendDocumentFieldValue(BsonDocument document, ChangeStreamDocument<BsonDocument> changeStreamResult, string fieldName)
        {
            BsonValue value = fieldName == "fullDocument" ? changeStreamResult.FullDocument : changeStreamResult.FullDocumentBeforeChange;
            if (value == null && !changeStreamResult.BackingDocument.Contains(fieldName))
            {
                return;
            }

            if (value == null)
            {
                value = BsonNull.Value;
            }

            document.Add(fieldName, value);
        }

        private static void AppendNsFieldValue(BsonDocument document, ChangeStreamDocument<BsonDocument> changeStreamDocument)
        {
            BsonDocument nsFieldValue;
            if (changeStreamDocument.CollectionNamespace != null)
            {
                nsFieldValue = (BsonDocument)SerializationHelper.SerializeValue(ChangeStreamDocumentCollectionNamespaceSerializer.Instance, changeStreamDocument.CollectionNamespace);
            }
            else if (changeStreamDocument.DatabaseNamespace != null)
            {
                nsFieldValue = (BsonDocument)SerializationHelper.SerializeValue(ChangeStreamDocumentDatabaseNamespaceSerializer.Instance, changeStreamDocument.DatabaseNamespace);
            }
            else if (changeStreamDocument.BackingDocument.Contains("ns"))
            {
                nsFieldValue = new BsonDocument();
            }
            else
            {
                return;
            }

            // map the rest of ns document
            nsFieldValue.AddRange(changeStreamDocument.BackingDocument["ns"].AsBsonDocument.Where(e => !__nsFields.Contains(e.Name)));
            document.Add("ns", nsFieldValue);
        }

        private static void AppendOperationTypeFieldValue(BsonDocument document, ChangeStreamDocument<BsonDocument> changeStreamDocument)
        {
            BsonValue value = null;
            if (Enum.IsDefined(typeof(ChangeStreamOperationType), changeStreamDocument.OperationType))
            {
                value = SerializationHelper.SerializeValue(ChangeStreamOperationTypeSerializer.Instance, changeStreamDocument.OperationType);
            }
            else if (changeStreamDocument.BackingDocument.Contains("operationType"))
            {
                value = changeStreamDocument.BackingDocument["operationType"];
            }

            if (value == null)
            {
                return;
            }

            document.Add("operationType", value);
        }
    }
}

