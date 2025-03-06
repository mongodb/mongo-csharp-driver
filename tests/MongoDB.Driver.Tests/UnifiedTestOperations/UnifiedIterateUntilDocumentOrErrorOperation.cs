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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedIterateUntilDocumentOrErrorOperation<TDocument> : IUnifiedEntityTestOperation
    {
        private readonly UnifiedIterateUntilDocumentOrErrorOperationResultConverter _converter;
        private readonly IEnumerator<TDocument> _enumerator;

        public UnifiedIterateUntilDocumentOrErrorOperation(IEnumerator<TDocument> enumerator)
        {
            _converter = new UnifiedIterateUntilDocumentOrErrorOperationResultConverter();
            _enumerator = enumerator;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var hasNext = _enumerator.MoveNext();
                if (hasNext == false)
                {
                    throw new InvalidOperationException("Unexpected false return value from MoveNext.");
                }
                return OperationResult.FromResult(_converter.Convert(_enumerator.Current));
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var hasNext = _enumerator.MoveNext(); // TODO: Change to async counterpart when async enumeration is implemented
                if (hasNext == false)
                {
                    throw new InvalidOperationException("Unexpected false return value from MoveNext.");
                }
                return Task.FromResult(OperationResult.FromResult(_converter.Convert(_enumerator.Current)));
            }
            catch (Exception exception)
            {
                return Task.FromResult(OperationResult.FromException(exception));
            }
        }
    }

    public class UnifiedIterateUntilDocumentOrErrorOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedIterateUntilDocumentOrErrorOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public IUnifiedEntityTestOperation Build(string targetEnumeratorId, BsonDocument arguments)
        {
            if (arguments != null)
            {
                throw new FormatException("IterateUntilDocumentOrErrorOperation is not expected to contain arguments.");
            }

            if (_entityMap.ChangeStreams.TryGetValue(targetEnumeratorId, out var changeStreamEnumerator))
            {
                return new UnifiedIterateUntilDocumentOrErrorOperation<ChangeStreamDocument<BsonDocument>>(changeStreamEnumerator);
            }
            else if (_entityMap.Cursors.TryGetValue(targetEnumeratorId, out var enumerator))
            {
                return new UnifiedIterateUntilDocumentOrErrorOperation<BsonDocument>(enumerator);
            }
            else
            {
                throw new FormatException("No supported enumerator found.");
            }
        }
    }

    public class UnifiedIterateUntilDocumentOrErrorOperationResultConverter
    {
        private static readonly string[] __changeStreamFields = ["_id", "clusterTime", "documentKey", "fullDocument", "fullDocumentBeforeChange", "ns", "operationDescription", "operationType", "to", "splitEvent", "collectionUUID", "updateDescription", "wallTime"];
        private static readonly string[] __nsFields = ["db", "coll"];

        public BsonDocument Convert<T>(T value) =>
            value switch
            {
                ChangeStreamDocument<BsonDocument> changeStreamResult => MapToBsonDocument(changeStreamResult),
                BsonDocument bsonDocument => bsonDocument,
                _ => throw new FormatException($"Unsupported enumerator document {value.GetType().Name}.")
            };

        private BsonDocument MapToBsonDocument(ChangeStreamDocument<BsonDocument> changeStreamResult)
        {
            var result = new BsonDocument()
            {
                { "_id", changeStreamResult.ResumeToken },
                { "clusterTime", changeStreamResult.ClusterTime, changeStreamResult.ClusterTime != null },
                { "documentKey", changeStreamResult.DocumentKey, changeStreamResult.DocumentKey != null },
                { "operationDescription", changeStreamResult.OperationDescription, changeStreamResult.OperationDescription != null },
                { "to", () => SerializationHelper.SerializeValue(ChangeStreamDocumentCollectionNamespaceSerializer.Instance, changeStreamResult.RenameTo), changeStreamResult.RenameTo != null },
                { "splitEvent", () => SerializationHelper.SerializeValue(ChangeStreamSplitEventSerializer.Instance, changeStreamResult.SplitEvent), changeStreamResult.SplitEvent != null },
                { "collectionUUID", () => SerializationHelper.SerializeValue(GuidSerializer.StandardInstance, changeStreamResult.CollectionUuid), changeStreamResult.CollectionUuid != null },
                { "updateDescription", () => SerializationHelper.SerializeValue(ChangeStreamUpdateDescriptionSerializer.Instance, changeStreamResult.UpdateDescription), changeStreamResult.UpdateDescription != null },
                { "wallTime", () => SerializationHelper.SerializeValue(DateTimeSerializer.UtcInstance, changeStreamResult.WallTime), changeStreamResult.WallTime != null }
            };
            AppendDocumentFieldValue(result, changeStreamResult, "fullDocument");
            AppendDocumentFieldValue(result, changeStreamResult, "fullDocumentBeforeChange");
            AppendNsFieldValue(result, changeStreamResult);
            AppendOperationTypeFieldValue(result, changeStreamResult);

            // map the rest of change stream document
            result.AddRange(changeStreamResult.BackingDocument.Elements.Where(e => !__changeStreamFields.Contains(e.Name)));

            return result;

            void AppendDocumentFieldValue(BsonDocument document, ChangeStreamDocument<BsonDocument> changeStreamResult, string fieldName)
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

            void AppendNsFieldValue(BsonDocument document, ChangeStreamDocument<BsonDocument> changeStreamDocument)
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

            void AppendOperationTypeFieldValue(BsonDocument document, ChangeStreamDocument<BsonDocument> changeStreamDocument)
            {
                BsonValue value = null;
                if (Enum.IsDefined(typeof(ChangeStreamOperationType), changeStreamDocument.OperationType))
                {
                    value = SerializationHelper.SerializeValue(ChangeStreamOperationTypeSerializer.Instance, changeStreamDocument.OperationType);
                }
                else if(changeStreamResult.BackingDocument.Contains("operationType"))
                {
                    value = changeStreamResult.BackingDocument["operationType"];
                }

                if (value == null)
                {
                    return;
                }

                document.Add("operationType", value);
            }
        }
    }
}
