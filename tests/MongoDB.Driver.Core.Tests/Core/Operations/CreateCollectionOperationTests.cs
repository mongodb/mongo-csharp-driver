/* Copyright 2013-present MongoDB Inc.
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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Tests.Core.Operations;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateCollectionOperationTests : OperationTestBase
    {
        // test methods
        [Theory]
        [ParameterAttributeData]
        public void AutoIndexId_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

#pragma warning disable 618
            subject.AutoIndexId = value;
            var result = subject.AutoIndexId;
#pragma warning restore

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Capped_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.Capped = value;
            var result = subject.Capped;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

#pragma warning disable 618
            subject.AutoIndexId.Should().NotHaveValue();
#pragma warning restore
            subject.Capped.Should().NotHaveValue();
            subject.ChangeStreamPreAndPostImages.Should().BeNull();
            subject.ClusteredIndex.Should().BeNull();
            subject.Collation.Should().BeNull();
            subject.EncryptedFields.Should().BeNull();
            subject.IndexOptionDefaults.Should().BeNull();
            subject.MaxDocuments.Should().NotHaveValue();
            subject.MaxSize.Should().NotHaveValue();
            subject.NoPadding.Should().NotHaveValue();
            subject.StorageEngine.Should().BeNull();
            subject.UsePowerOf2Sizes.Should().NotHaveValue();
            subject.ValidationAction.Should().BeNull();
            subject.ValidationLevel.Should().BeNull();
            subject.Validator.Should().BeNull();
            subject.WriteConcern.Should().BeNull();
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => { new CreateCollectionOperation(null, _messageEncoderSettings); });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_AutoIndexId_is_set(
            [Values(null, false, true)]
            bool? autoIndexId)
        {
#pragma warning disable 618
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                AutoIndexId = autoIndexId
            };
#pragma warning restore
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "autoIndexId", () => autoIndexId.Value, autoIndexId != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Capped_is_set(
            [Values(null, false, true)]
            bool? capped)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Capped = capped
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "capped", () => capped.Value, capped != null }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result_when_ChangeStreamsPreAndPost_is_set()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ChangeStreamPreAndPostImages = new BsonDocument()
                {
                    { "key", "value" }
                }
            };

            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "changeStreamPreAndPostImages", new BsonDocument() { { "key", "value" } } }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ClusteredIndex_is_set(
            [Values(null, "{ key : { _id : 1 }, unique : true }", "{ key : { _id : 1 }, unique : true, name: 'clustered index name' }")]
            string clusteredIndex)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ClusteredIndex = clusteredIndex != null ? BsonDocument.Parse(clusteredIndex) : null
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "clusteredIndex", () => BsonDocument.Parse(clusteredIndex), clusteredIndex != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Collation_is_set(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var collation = locale == null ? null : new Collation(locale);
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = collation
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_IndexOptionDefaults_is_set(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string indexOptionDefaultsString)
        {
            var indexOptionDefaults = indexOptionDefaultsString == null ? null : BsonDocument.Parse(indexOptionDefaultsString);
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                IndexOptionDefaults = indexOptionDefaults
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "indexOptionDefaults", () => indexOptionDefaults, indexOptionDefaults != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_MaxDocuments_is_set(
            [Values(null, 1L, 2L)]
            long? maxDocuments)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                MaxDocuments = maxDocuments
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "max", () => maxDocuments.Value, maxDocuments != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_MaxSize_is_set(
            [Values(null, 1L, 2L)]
            long? maxSize)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                MaxSize = maxSize
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "size", () => maxSize.Value, maxSize != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_NoPadding_is_set(
            [Values(null, false, true)]
            bool? noPadding)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                NoPadding = noPadding
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "flags", () => noPadding.Value ? 2 : 0, noPadding != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_StorageEngine_is_set(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string storageEngineString)
        {
            var storageEngine = storageEngineString == null ? null : BsonDocument.Parse(storageEngineString);
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                StorageEngine = storageEngine
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "storageEngine", storageEngine, storageEngineString != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_UsePowerOf2Sizes_is_set(
            [Values(null, false, true)]
            bool? usePowerOf2Sizes)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                UsePowerOf2Sizes = usePowerOf2Sizes
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "flags", () => usePowerOf2Sizes.Value ? 1 : 0, usePowerOf2Sizes != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ValidationAction_is_set(
            [Values(null, DocumentValidationAction.Error, DocumentValidationAction.Warn)]
            DocumentValidationAction? validationAction)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ValidationAction = validationAction
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "validationAction", () => validationAction.ToString().ToLowerInvariant(), validationAction != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ValidationLevel_is_set(
            [Values(null, DocumentValidationLevel.Moderate, DocumentValidationLevel.Off)]
            DocumentValidationLevel? validationLevel)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ValidationLevel = validationLevel
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "validationLevel", () => validationLevel.ToString().ToLowerInvariant(), validationLevel != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Validator_is_set(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string validatorString)
        {
            var validator = validatorString == null ? null : BsonDocument.Parse(validatorString);
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Validator = validator
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "validator", validator, validator != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateEncryptedCreateCollectionOperationIfConfigured_should_return_expected_result_when_EncryptedFields_is_null()
        {
            var subject = CreateCollectionOperation.CreateEncryptedCreateCollectionOperationIfConfigured(_collectionNamespace, encryptedFields: null, _messageEncoderSettings, null);
            var session = OperationTestHelper.CreateSession();

            var s = subject.Should().BeOfType<CreateCollectionOperation>().Subject;

            var command = s.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
            };
            command.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(new[] { "'escCollection' : 'escCollectionName'", "escCollectionName" }, new[] { "'ecocCollection' : 'ecocCollectionName'", "ecocCollectionName" })]
        [InlineData(new[] { null, "esc" }, new[] { null, "ecoc" })]
        public void CreateEncryptedCreateCollectionOperationIfConfigured_should_return_expected_result_when_EncryptedFields_is_set(string[] escCollectionStrElement, string[] ecocCollectionStrElement)
        {
            var encryptedFields = BsonDocument.Parse($@"
            {{
                {GetFirstElementWithCommaOrEmpty(escCollectionStrElement)}
                {GetFirstElementWithCommaOrEmpty(ecocCollectionStrElement)}
                ""fields"" :
                [{{
                    ""path"" : ""firstName"",
                    ""keyId"" : {{ ""$binary"" : {{ ""subType"" : ""04"", ""base64"" : ""AAAAAAAAAAAAAAAAAAAAAA=="" }} }},
                    ""bsonType"" : ""string"",
                    ""queries"" : {{ ""queryType"" : ""equality"" }}
                }},
                {{
                    ""path"" : ""ssn"",
                    ""keyId"" : {{ ""$binary"" : {{ ""subType"" : ""04"", ""base64"": ""BBBBBBBBBBBBBBBBBBBBBB=="" }} }},
                    ""bsonType"" : ""string""
                }}]
            }}");

            var subject = CreateCollectionOperation.CreateEncryptedCreateCollectionOperationIfConfigured(_collectionNamespace, encryptedFields, _messageEncoderSettings, null);
            var session = OperationTestHelper.CreateSession();

            var operations = ((CompositeWriteOperation<BsonDocument>)subject)._operations<BsonDocument>();

            // esc
            AssertCreateCollectionCommand(
                operations[0],
                new CollectionNamespace(_collectionNamespace.DatabaseNamespace.DatabaseName, GetExpectedCollectionName(escCollectionStrElement)),
                encryptedFields: null,
                isMainOperation: false);
            // eco
            AssertCreateCollectionCommand(
                operations[1],
                new CollectionNamespace(_collectionNamespace.DatabaseNamespace.DatabaseName, GetExpectedCollectionName(ecocCollectionStrElement)),
                encryptedFields: null,
                isMainOperation: false);
            // main
            AssertCreateCollectionCommand(
                operations[2],
                _collectionNamespace,
                encryptedFields,
                isMainOperation: true,
                withClusteredIndex: false);
            // __safeContent__
            AssertIndex(operations[3], _collectionNamespace, index: new BsonDocument("__safeContent__", 1));

            void AssertCreateCollectionCommand((IWriteOperation<BsonDocument> Operation, bool IsMainOperation) operationInfo, CollectionNamespace collectionNamespace, BsonDocument encryptedFields, bool isMainOperation, bool withClusteredIndex = true)
            {
                var expectedResult = new BsonDocument
                {
                    { "create", collectionNamespace.CollectionName },
                    { "encryptedFields", encryptedFields, encryptedFields != null },
                    { "clusteredIndex", new BsonDocument { { "key" , new BsonDocument("_id", 1 ) }, { "unique", true } }, withClusteredIndex }
                };
                AssertCommand(operationInfo, isMainOperation, expectedResult);
            }

            void AssertIndex((IWriteOperation<BsonDocument> Operation, bool IsMainOperation) operationInfo, CollectionNamespace collectionNamespace, BsonDocument index)
            {
                var expectedResult = new BsonDocument
                {
                    { "createIndexes", collectionNamespace.CollectionName },
                    {
                        "indexes",
                        BsonArray.Create(new [] { new BsonDocument { { "key", index }, { "name", IndexNameHelper.GetIndexName(index) } } })
                    }
                };
                AssertCommand(operationInfo, isMainOperation: false, expectedResult);
            }

            void AssertCommand((IWriteOperation<BsonDocument> Operation, bool IsMainOperation) operationInfo, bool isMainOperation, BsonDocument expectedResult)
            {
                operationInfo.IsMainOperation.Should().Be(isMainOperation);
                var operation = operationInfo.Operation;

                var result = operation switch
                {
                    CreateCollectionOperation createCollectionOperation => createCollectionOperation.CreateCommand(session),
                    CreateIndexesOperation createIndexesOperation => createIndexesOperation.CreateCommand(session, OperationTestHelper.CreateConnectionDescription()),
                    _ => throw new Exception($"Unexpected operation {operation}."),
                };
                result.Should().Be(expectedResult);
            }

            string GetFirstElementWithCommaOrEmpty(string[] array) => array.First() != null ? $"{array.First()}," : string.Empty;

            string GetExpectedCollectionName(string[] array)
            {
                var first = array.First();
                var last = array.Last();
                if (first != null)
                {
                    return last;
                }
                else
                {
                    return $"enxcol_.{_collectionNamespace.CollectionName}.{last}";
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_create_collection(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            BsonDocument info;
            using (var binding = CreateReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["name"].AsString.Should().Be(_collectionNamespace.CollectionName);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_AutoIndexId_is_set(
            [Values(false, true)]
            bool autoIndexId,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().VersionLessThan("3.7.0");
            DropCollection();
#pragma warning disable 618
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                AutoIndexId = autoIndexId
            };
#pragma warning restore

            BsonDocument info;
            using (var binding = CreateReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["autoIndexId"].ToBoolean().Should().Be(autoIndexId);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_Capped_is_set(
            [Values(false, true)]
            bool capped,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var maxSize = capped ? 10000L : (long?)null;
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Capped = capped,
                MaxSize = maxSize
            };

            BsonDocument info;
            using (var binding = CreateReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            // in older versions of the server options might not be present if capped is false
            if (capped || info.Contains("options"))
            {
                info["options"].AsBsonDocument.GetValue("capped", false).ToBoolean().Should().Be(capped);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_Collation_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            BsonDocument info;
            using (var binding = CreateReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["collation"]["locale"].AsString.Should().Be("en_US");
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_IndexOptionDefaults_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            DropCollection();
            var storageEngine = CoreTestConfiguration.StorageEngine;
            var indexOptionDefaults = new BsonDocument
            {
                {  "storageEngine", new BsonDocument(storageEngine, new BsonDocument()) }
            };
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                IndexOptionDefaults = indexOptionDefaults
            };

            BsonDocument info;
            using (var binding = CreateReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["indexOptionDefaults"].Should().Be(indexOptionDefaults);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_MaxDocuments_is_set(
            [Values(1L, 2L)]
            long maxDocuments,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Capped = true,
                MaxSize = 10000L,
                MaxDocuments = maxDocuments
            };

            BsonDocument info;
            using (var binding = CreateReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["max"].ToInt64().Should().Be(maxDocuments);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_MaxSize_is_set(
            [Values(10000L, 20000L)]
            long maxSize,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Capped = true,
                MaxSize = maxSize
            };

            BsonDocument info;
            using (var binding = CreateReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["size"].ToInt64().Should().BeGreaterOrEqualTo(maxSize); // server rounds maxSize up
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_NoPadding_is_set(
            [Values(false, true)]
            bool noPadding,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).StorageEngine("mmapv1");
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                NoPadding = noPadding
            };

            BsonDocument info;
            using (var binding = CreateReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["flags"].Should().Be(noPadding ? 2 : 0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_StorageEngine_is_set(
            [Values("abc", "def")]
            string metadata,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().StorageEngine("wiredTiger");
            DropCollection();
            var storageEngine = new BsonDocument
            {
                { "wiredTiger", new BsonDocument("configString", "app_metadata=" + metadata) }
            };
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                StorageEngine = storageEngine
            };

            BsonDocument info;
            using (var binding = CreateReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["storageEngine"].Should().Be(storageEngine);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_UsePowerOf2Sizes_is_set(
            [Values(false, true)]
            bool usePowerOf2Sizes,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).StorageEngine("mmapv1");
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                UsePowerOf2Sizes = usePowerOf2Sizes
            };

            BsonDocument info;
            using (var binding = CreateReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["flags"].Should().Be(usePowerOf2Sizes ? 1 : 0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_Validator_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var validator = new BsonDocument("_id", new BsonDocument("$exists", true));
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Validator = validator,
                ValidationLevel = DocumentValidationLevel.Strict,
                ValidationAction = DocumentValidationAction.Error
            };

            BsonDocument info;
            using (var binding = CreateReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["validator"].Should().Be(validator);
            info["options"]["validationLevel"].AsString.Should().Be("strict");
            info["options"]["validationAction"].AsString.Should().Be("error");
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterType(ClusterType.ReplicaSet);
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() =>
            {
                using (var binding = CreateReadWriteBinding())
                {
                    ExecuteOperation(subject, binding, false);
                }
            });

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "create", async);
        }

        [Theory]
        [ParameterAttributeData]
        public void IndexOptionDefaults_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.IndexOptionDefaults = value;
            var result = subject.IndexOptionDefaults;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxDocuments_get_and_set_should_work(
            [Values(null, 1L, 2L)]
            long? value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.MaxDocuments = value;
            var result = subject.MaxDocuments;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxDocuments_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            long maxDocuments)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            var exception = Record.Exception(() => { subject.MaxDocuments = maxDocuments; });

            var argumentOutOfRangeException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            argumentOutOfRangeException.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxSize_get_and_set_should_work(
            [Values(null, 1L, 2L)]
            long? value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.MaxSize = value;
            var result = subject.MaxSize;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxSize_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            long maxSize)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            var exception = Record.Exception(() => { subject.MaxSize = maxSize; });

            var argumentOutOfRangeException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            argumentOutOfRangeException.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void NoPadding_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.NoPadding = value;
            var result = subject.NoPadding;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void StorageEngine_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.StorageEngine = value;
            var result = subject.StorageEngine;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void UsePowerOf2Sizes_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.UsePowerOf2Sizes = value;
            var result = subject.UsePowerOf2Sizes;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ValidationAction_get_and_set_should_work(
            [Values(null, DocumentValidationAction.Error, DocumentValidationAction.Warn)]
            DocumentValidationAction? value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.ValidationAction = value;
            var result = subject.ValidationAction;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ValidationLevel_get_and_set_should_work(
            [Values(null, DocumentValidationLevel.Moderate, DocumentValidationLevel.Off)]
            DocumentValidationLevel? value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.ValidationLevel = value;
            var result = subject.ValidationLevel;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Validator_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Validator = value;
            var result = subject.Validator;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? w)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var value = w.HasValue ? new WriteConcern(w.Value) : null;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        // helper methods
        private BsonDocument ExecuteOperation(CreateCollectionOperation subject, IWriteBinding binding, bool async)
        {
            if (async)
            {
                return subject.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                return subject.Execute(binding, CancellationToken.None);
            }
        }

        private BsonDocument GetCollectionInfo(IReadBinding binding)
        {
            var listCollectionsOperation = new ListCollectionsOperation(_collectionNamespace.DatabaseNamespace, _messageEncoderSettings)
            {
                Filter = new BsonDocument("name", _collectionNamespace.CollectionName)
            };
            return listCollectionsOperation.Execute(binding, CancellationToken.None).Single();
        }
    }
}
