/* Copyright 2013-2016 MongoDB Inc.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateCollectionOperationTests
    {
        // fields
        private CollectionNamespace _collectionNamespace;
        private MessageEncoderSettings _messageEncoderSettings;

        public CreateCollectionOperationTests()
        {
            _collectionNamespace = CoreTestConfiguration.GetCollectionNamespaceForTestClass(typeof(CreateCollectionOperationTests));
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
        }

        // test methods
        [Fact]
        public void AutoIndexId_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.AutoIndexId.Should().NotHaveValue();

            subject.AutoIndexId = true;

            subject.AutoIndexId.Should().BeTrue();
        }

        [Fact]
        public void Capped_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.Capped.Should().NotHaveValue();

            subject.Capped = true;

            subject.Capped.Should().BeTrue();
        }

        [Fact]
        public void CollectionNamespace_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
        }

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.AutoIndexId.Should().NotHaveValue();
            subject.Capped.Should().NotHaveValue();
            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MaxDocuments.Should().NotHaveValue();
            subject.MaxSize.Should().NotHaveValue();
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.UsePowerOf2Sizes.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            Action action = () => { new CreateCollectionOperation(null, _messageEncoderSettings); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Equals("value");
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName }
            };

            var result = subject.CreateCommand(new SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_AutoIndexId_is_set(
            [Values(false, true)]
            bool autoIndexId)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                AutoIndexId = autoIndexId
            };
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "autoIndexId", autoIndexId }
            };

            var result = subject.CreateCommand(new SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Capped_is_set(
            [Values(false, true)]
            bool capped)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Capped = capped
            };
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "capped", capped }
            };

            var result = subject.CreateCommand(new SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result_when_IndexOptionDefaults_is_set()
        {
            var value = new BsonDocument("storageEngine", new BsonDocument("x", 1));
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                IndexOptionDefaults = value
            };
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "indexOptionDefaults", value }
            };

            var result = subject.CreateCommand(new SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_MaxDocuments_is_set(
            [Values(1, 2)]
            long maxDocuments)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                MaxDocuments = maxDocuments
            };
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "max", maxDocuments }
            };

            var result = subject.CreateCommand(new SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_MaxSize_is_set(
            [Values(1, 2)]
            long maxSize)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                MaxSize = maxSize
            };
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "size", maxSize }
            };

            var result = subject.CreateCommand(new SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_StorageEngine_is_set(
            [Values(null, "{ awesome: true }")]
            string storageEngine)
        {
            var storageEngineDoc = storageEngine == null ? null : BsonDocument.Parse(storageEngine);
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                StorageEngine = storageEngineDoc
            };
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "storageEngine", storageEngineDoc, storageEngine != null }
            };

            var result = subject.CreateCommand(new SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_UsePowerOf2Sizes_is_set(
            [Values(false, true)]
            bool usePowerOf2Sizes)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                UsePowerOf2Sizes = usePowerOf2Sizes
            };
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "flags", usePowerOf2Sizes ? 1 : 0 }
            };

            var result = subject.CreateCommand(new SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ValidationAction_is_set(
            [Values(DocumentValidationAction.Error, DocumentValidationAction.Warn)]
            DocumentValidationAction value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ValidationAction = value
            };
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "validationAction", value.ToString().ToLowerInvariant() }
            };

            var result = subject.CreateCommand(new SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ValidationLevel_is_set(
            [Values(DocumentValidationLevel.Moderate, DocumentValidationLevel.Off)]
            DocumentValidationLevel value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ValidationLevel = value
            };
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "validationLevel", value.ToString().ToLowerInvariant() }
            };

            var result = subject.CreateCommand(new SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result_when_Validator_is_set()
        {
            var value = new BsonDocument("x", 1);
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Validator = value
            };
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "validator", value }
            };

            var result = subject.CreateCommand(new SemanticVersion(3, 2, 0));

            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Any();
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = ExecuteOperation(subject, binding, async);

                result["ok"].ToBoolean().Should().BeTrue();

                var stats = GetCollectionStats(binding, async);
                stats["ns"].ToString().Should().Be(_collectionNamespace.FullName);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_AutoIndexId_is_set(
            [Values(false, true)]
            bool autoIndexId,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Any();
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                AutoIndexId = autoIndexId
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = ExecuteOperation(subject, binding, async);

                result["ok"].ToBoolean().Should().BeTrue();

                var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
                List<BsonDocument> indexes;
                if (async)
                {
                    var cursor = listIndexesOperation.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
                    indexes = cursor.ToListAsync().GetAwaiter().GetResult();
                }
                else
                {
                    var cursor = listIndexesOperation.Execute(binding, CancellationToken.None);
                    indexes = cursor.ToList();
                }

                indexes.Count.Should().Be(autoIndexId ? 1 : 0);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_Capped_is_set(
            [Values(false, true)]
            bool capped,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Any();
            DropCollection();
            var maxSize = capped ? (long?)10000 : null;
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Capped = capped,
                MaxSize = maxSize
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = ExecuteOperation(subject, binding, async);

                result["ok"].ToBoolean().Should().BeTrue();

                var stats = GetCollectionStats(binding, async);
                stats["ns"].ToString().Should().Be(_collectionNamespace.FullName);
                stats.GetValue("capped", false).ToBoolean().Should().Be(capped);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_IndexOptionDefaults_is_set(
           [Values(false, true)] bool async)
        {
            RequireServer.Where(minimumVersion: "3.2.0-rc0");
            DropCollection();
            var storageEngineOptions = new BsonDocument("mmapv1", new BsonDocument());
            var indexOptionDefaults = new BsonDocument("storageEngine", storageEngineOptions);
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                IndexOptionDefaults = indexOptionDefaults
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = ExecuteOperation(subject, binding, async);

                result["ok"].ToBoolean().Should().BeTrue();
                var collectionInfo = GetCollectionInfo(binding, _collectionNamespace.CollectionName);
                Assert.Equal(indexOptionDefaults, collectionInfo["options"]["indexOptionDefaults"]);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_MaxDocuments_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Any();
            DropCollection();
            var maxDocuments = 123L;
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Capped = true,
                MaxSize = 10000L,
                MaxDocuments = maxDocuments
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = ExecuteOperation(subject, binding, async);

                result["ok"].ToBoolean().Should().BeTrue();

                var stats = GetCollectionStats(binding, async);
                stats["ns"].ToString().Should().Be(_collectionNamespace.FullName);
                stats["capped"].ToBoolean().Should().BeTrue();
                stats["max"].ToInt64().Should().Be(maxDocuments);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_MaxSize_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Any();
            DropCollection();
            var maxSize = 10000L;
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Capped = true,
                MaxSize = maxSize
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = ExecuteOperation(subject, binding, async);

                result["ok"].ToBoolean().Should().BeTrue();

                var stats = GetCollectionStats(binding, async);
                stats["ns"].ToString().Should().Be(_collectionNamespace.FullName);
                stats["capped"].ToBoolean().Should().BeTrue();
                // TODO: not sure how to verify that the maxSize took effect
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_UsePowerOf2Sizes_is_set(
            [Values(false, true)]
            bool usePowerOf2Sizes,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Where(clusterTypes: ClusterTypes.StandaloneOrReplicaSet, storageEngines: "mmapv1");
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                UsePowerOf2Sizes = usePowerOf2Sizes
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = ExecuteOperation(subject, binding, async);

                result["ok"].ToBoolean().Should().BeTrue();

                var stats = GetCollectionStats(binding, async);
                stats["ns"].ToString().Should().Be(_collectionNamespace.FullName);
                stats["userFlags"].ToInt32().Should().Be(usePowerOf2Sizes ? 1 : 0);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_Validator_is_set(
            [Values(false, true)] bool async)
        {
            RequireServer.Where(minimumVersion: "3.2.0-rc0");
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                ValidationAction = DocumentValidationAction.Error,
                ValidationLevel = DocumentValidationLevel.Strict,
                Validator = new BsonDocument("_id", new BsonDocument("$exists", true))
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = ExecuteOperation(subject, binding, async);

                result["ok"].ToBoolean().Should().BeTrue();
                var collectionInfo = GetCollectionInfo(binding, _collectionNamespace.CollectionName);
                Assert.Equal(new BsonDocument("_id", new BsonDocument("$exists", true)), collectionInfo["options"]["validator"]);
                Assert.Equal("error", collectionInfo["options"]["validationAction"].AsString);
                Assert.Equal("strict", collectionInfo["options"]["validationLevel"].AsString);
            }
        }

        [Fact]
        public void IndexOptionDefaults_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var value = new BsonDocument("storageEngine", new BsonDocument("x", 1));
            subject.IndexOptionDefaults.Should().BeNull();

            subject.IndexOptionDefaults = value;

            subject.IndexOptionDefaults.Should().Be(value);
        }

        [Fact]
        public void MaxDocuments_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.MaxDocuments.Should().NotHaveValue();

            subject.MaxDocuments = 1;

            subject.MaxDocuments.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxDocuments_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            long maxDocuments)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            Action action = () => { subject.MaxDocuments = maxDocuments; };

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Equals("value");
        }

        [Fact]
        public void MaxSize_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.MaxSize.Should().NotHaveValue();

            subject.MaxSize = 1;

            subject.MaxSize.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxSize_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            long maxSize)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            Action action = () => { subject.MaxSize = maxSize; };

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Equals("value");
        }

        [Fact]
        public void MessageEncoderSettings_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Fact]
        public void UsePowerOf2Sizes_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.UsePowerOf2Sizes.Should().NotHaveValue();

            subject.UsePowerOf2Sizes = true;

            subject.UsePowerOf2Sizes.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void ValidationAction_should_work(
            [Values(null, DocumentValidationAction.Error, DocumentValidationAction.Warn)]
            DocumentValidationAction? value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.ValidationAction.Should().BeNull();

            subject.ValidationAction = value;

            subject.ValidationAction.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ValidationLevel_should_work(
            [Values(null, DocumentValidationLevel.Moderate, DocumentValidationLevel.Off)]
            DocumentValidationLevel? value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.ValidationLevel.Should().BeNull();

            subject.ValidationLevel = value;

            subject.ValidationLevel.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Validator_should_work(
            [Values(null, "{ x : 1 }")]
            string json)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var value = json == null ? null : BsonDocument.Parse(json);
            subject.Validator.Should().BeNull();

            subject.Validator = value;

            subject.Validator.Should().Be(value);
        }

        // helper methods
        private void DropCollection()
        {
            var operation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                operation.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        private BsonDocument GetCollectionInfo(IReadBinding binding, string collectionName)
        {
            var commandOperation = new ReadCommandOperation<BsonDocument>(
                _collectionNamespace.DatabaseNamespace,
                new BsonDocument("listCollections", 1),
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings());
            var commandResult = commandOperation.Execute(binding, CancellationToken.None);
            return commandResult["cursor"]["firstBatch"].AsBsonArray.Where(c => c["name"] == _collectionNamespace.CollectionName).Single().AsBsonDocument;
        }

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

        private BsonDocument GetCollectionStats(IReadBinding binding, bool async)
        {
            var command = new BsonDocument
            {
                { "collStats", _collectionNamespace.CollectionName }
            };
            var operation = new ReadCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            if (async)
            {
                return operation.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                return operation.Execute(binding, CancellationToken.None);
            }
        }
    }
}
