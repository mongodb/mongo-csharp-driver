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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
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
        [Theory]
        [ParameterAttributeData]
        public void AutoIndexId_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.AutoIndexId = value;
            var result = subject.AutoIndexId;

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

            subject.AutoIndexId.Should().NotHaveValue();
            subject.Capped.Should().NotHaveValue();
            subject.Collation.Should().BeNull();
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

            var result = subject.CreateCommand(null);

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
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                AutoIndexId = autoIndexId
            };

            var result = subject.CreateCommand(null);

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

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "capped", () => capped.Value, capped != null }
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

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_Collation_is_set_and_not_supported()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.Collation.LastNotSupportedVersion));

            exception.Should().BeOfType<NotSupportedException>();
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

            var result = subject.CreateCommand(null);

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

            var result = subject.CreateCommand(null);

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

            var result = subject.CreateCommand(null);

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

            var result = subject.CreateCommand(null);

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

            var result = subject.CreateCommand(null);

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

            var result = subject.CreateCommand(null);

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

            var result = subject.CreateCommand(null);

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

            var result = subject.CreateCommand(null);

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

            var result = subject.CreateCommand(null);

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
            int? w,
            [Values(false, true)]
            bool isWriteConcernSupported)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var serverVersion = Feature.CommandsThatWriteAcceptWriteConcern.SupportedOrNotSupportedVersion(isWriteConcernSupported);

            var result = subject.CreateCommand(serverVersion);

            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null && isWriteConcernSupported }
            };
            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            BsonDocument info;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["name"].AsString.Should().Be(_collectionNamespace.CollectionName);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_AutoIndexId_is_set(
            [Values(false, true)]
            bool autoIndexId,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                AutoIndexId = autoIndexId
            };

            BsonDocument info;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["autoIndexId"].ToBoolean().Should().Be(autoIndexId);
        }

        [SkippableTheory]
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
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
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

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_Collation_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Collation);
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            BsonDocument info;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["collation"]["locale"].AsString.Should().Be("en_US");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_IndexOptionDefaults_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.IndexOptionsDefaults);
            DropCollection();
            var indexOptionDefaults = new BsonDocument
            {
                {  "storageEngine", new BsonDocument("mmapv1", new BsonDocument()) }
            };
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                IndexOptionDefaults = indexOptionDefaults
            };

            BsonDocument info;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["indexOptionDefaults"].Should().Be(indexOptionDefaults);
        }

        [SkippableTheory]
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
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["max"].ToInt64().Should().Be(maxDocuments);
        }

        [SkippableTheory]
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
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["size"].ToInt64().Should().BeGreaterOrEqualTo(maxSize); // server rounds maxSize up
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_NoPadding_is_set(
            [Values(false, true)]
            bool noPadding,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.0").ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet).StorageEngine("mmapv1");
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                NoPadding = noPadding
            };

            BsonDocument info;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["flags"].Should().Be(noPadding ? 2 : 0);
        }

        [SkippableTheory]
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
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["storageEngine"].Should().Be(storageEngine);
        }

        [SkippableTheory]
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
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["flags"].Should().Be(usePowerOf2Sizes ? 1 : 0);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_create_collection_when_Validator_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.DocumentValidation);
            DropCollection();
            var validator = new BsonDocument("_id", new BsonDocument("$exists", true));
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Validator = validator,
                ValidationLevel = DocumentValidationLevel.Strict,
                ValidationAction = DocumentValidationAction.Error
            };

            BsonDocument info;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                ExecuteOperation(subject, binding, async);
                info = GetCollectionInfo(binding);
            }

            info["options"]["validator"].Should().Be(validator);
            info["options"]["validationLevel"].AsString.Should().Be("strict");
            info["options"]["validationAction"].AsString.Should().Be("error");
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_Collation_is_set_and_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().DoesNotSupport(Feature.Collation);
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            Exception exception;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                exception = Record.Exception(() => ExecuteOperation(subject, binding, async));
            }

            exception.Should().BeOfType<NotSupportedException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.CommandsThatWriteAcceptWriteConcern).ClusterType(ClusterType.ReplicaSet);
            DropCollection();
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() =>
            {
                using (var binding = CoreTestConfiguration.GetReadWriteBinding())
                {
                    ExecuteOperation(subject, binding, false);
                }
            });

            exception.Should().BeOfType<MongoWriteConcernException>();
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
        private void DropCollection()
        {
            var operation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                operation.Execute(binding, CancellationToken.None);
            }
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
