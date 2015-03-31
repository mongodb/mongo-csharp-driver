/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class CreateCollectionOperationTests
    {
        // fields
        private CollectionNamespace _collectionNamespace;
        private MessageEncoderSettings _messageEncoderSettings;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _collectionNamespace = CoreTestConfiguration.GetCollectionNamespaceForTestFixture();
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
        }

        // test methods
        [Test]
        public void AutoIndexId_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.AutoIndexId.Should().NotHaveValue();

            subject.AutoIndexId = true;

            subject.AutoIndexId.Should().BeTrue();
        }

        [Test]
        public void Capped_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.Capped.Should().NotHaveValue();

            subject.Capped = true;

            subject.Capped.Should().BeTrue();
        }

        [Test]
        public void CollectionNamespace_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
        }

        [Test]
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

        [Test]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            Action action = () => { new CreateCollectionOperation(null, _messageEncoderSettings); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Equals("value");
        }

        [Test]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
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

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
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

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
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

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
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

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
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

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
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

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_create_collection()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding, CancellationToken.None);

                result["ok"].ToBoolean().Should().BeTrue();

                var stats = await GetCollectionStatsAsync(binding);
                stats["ns"].ToString().Should().Be(_collectionNamespace.FullName);
            }
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_create_collection_when_AutoIndexId_is_set(
            [Values(false, true)]
            bool autoIndexId)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                AutoIndexId = autoIndexId
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding, CancellationToken.None);

                result["ok"].ToBoolean().Should().BeTrue();

                var listIndexesOperation = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
                var cursor = await listIndexesOperation.ExecuteAsync(binding, CancellationToken.None);
                var indexes = await cursor.ToListAsync();

                indexes.Count.Should().Be(autoIndexId ? 1 : 0);
            }
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_create_collection_when_Capped_is_set(
            [Values(false, true)]
            bool capped)
        {
            var maxSize = capped ? (long?)10000 : null;
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Capped = capped,
                MaxSize = maxSize
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding, CancellationToken.None);

                result["ok"].ToBoolean().Should().BeTrue();

                var stats = await GetCollectionStatsAsync(binding);
                stats["ns"].ToString().Should().Be(_collectionNamespace.FullName);
                stats.GetValue("capped", false).ToBoolean().Should().Be(capped);
            }
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_create_collection_when_MaxDocuments_is_set()
        {
            var maxDocuments = 123L;
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Capped = true,
                MaxSize = 10000L,
                MaxDocuments = maxDocuments
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding, CancellationToken.None);

                result["ok"].ToBoolean().Should().BeTrue();

                var stats = await GetCollectionStatsAsync(binding);
                stats["ns"].ToString().Should().Be(_collectionNamespace.FullName);
                stats["capped"].ToBoolean().Should().BeTrue();
                stats["max"].ToInt64().Should().Be(maxDocuments);
            }
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_create_collection_when_MaxSize_is_set()
        {
            var maxSize = 10000L;
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Capped = true,
                MaxSize = maxSize
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding, CancellationToken.None);

                result["ok"].ToBoolean().Should().BeTrue();

                var stats = await GetCollectionStatsAsync(binding);
                stats["ns"].ToString().Should().Be(_collectionNamespace.FullName);
                stats["capped"].ToBoolean().Should().BeTrue();
                // TODO: not sure how to verify that the maxSize took effect
            }
        }

        [Test]
        [RequiresServer("DropCollection", StorageEngines = "mmapv1", ClusterTypes = ClusterTypes.StandaloneOrReplicaSet)]
        public async Task ExecuteAsync_should_create_collection_when_UsePowerOf2Sizes_is_set(
            [Values(false, true)]
            bool usePowerOf2Sizes)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                UsePowerOf2Sizes = usePowerOf2Sizes
            };

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding, CancellationToken.None);

                result["ok"].ToBoolean().Should().BeTrue();

                var stats = await GetCollectionStatsAsync(binding);
                stats["ns"].ToString().Should().Be(_collectionNamespace.FullName);
                stats["userFlags"].ToInt32().Should().Be(usePowerOf2Sizes ? 1 : 0);
            }
        }

        [Test]
        public void MaxDocuments_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.MaxDocuments.Should().NotHaveValue();

            subject.MaxDocuments = 1;

            subject.MaxDocuments.Should().Be(1);
        }

        [Test]
        public void MaxDocuments_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            long maxDocuments)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            Action action = () => { subject.MaxDocuments = maxDocuments; };

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Equals("value");
        }

        [Test]
        public void MaxSize_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.MaxSize.Should().NotHaveValue();

            subject.MaxSize = 1;

            subject.MaxSize.Should().Be(1);
        }

        [Test]
        public void MaxSize_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            long maxSize)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            Action action = () => { subject.MaxSize = maxSize; };

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Equals("value");
        }

        [Test]
        public void MessageEncoderSettings_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Test]
        public void UsePowerOf2Sizes_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            subject.UsePowerOf2Sizes.Should().NotHaveValue();

            subject.UsePowerOf2Sizes = true;

            subject.UsePowerOf2Sizes.Should().BeTrue();
        }

        // helper methods
        public void DropCollection()
        {
            var operation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                operation.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        public Task<BsonDocument> GetCollectionStatsAsync(IReadBinding binding)
        {
            var command = new BsonDocument
            {
                { "collStats", _collectionNamespace.CollectionName }
            };
            var operation = new ReadCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            return operation.ExecuteAsync(binding, CancellationToken.None);
        }
    }
}
