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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.SyncExtensionMethods;
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
            _collectionNamespace = SuiteConfiguration.GetCollectionNamespaceForTestFixture();
            _messageEncoderSettings = SuiteConfiguration.MessageEncoderSettings;
        }

        // test methods
        [Test]
        public void AutoIndexId_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            Assert.That(subject.AutoIndexId, Is.Null);

            subject.AutoIndexId = true;

            Assert.That(subject.AutoIndexId, Is.True);
        }

        [Test]
        public void Capped_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            Assert.That(subject.Capped, Is.Null);

            subject.Capped = true;

            Assert.That(subject.Capped, Is.True);
        }

        [Test]
        public void CollectionNamespace_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            Assert.That(subject.CollectionNamespace, Is.SameAs(_collectionNamespace));
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            Assert.That(subject.AutoIndexId, Is.Null);
            Assert.That(subject.Capped, Is.Null);
            Assert.That(subject.CollectionNamespace, Is.SameAs(_collectionNamespace));
            Assert.That(subject.MaxDocuments, Is.Null);
            Assert.That(subject.MaxSize, Is.Null);
            Assert.That(subject.MessageEncoderSettings, Is.SameAs(_messageEncoderSettings));
            Assert.That(subject.UsePowerOf2Sizes, Is.Null);
        }

        [Test]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var ex = Assert.Catch(() => new CreateCollectionOperation(null, _messageEncoderSettings));

            Assert.That(ex, Is.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("collectionNamespace"));
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

            Assert.That(result, Is.EqualTo(expectedResult));
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

            Assert.That(result, Is.EqualTo(expectedResult));
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

            Assert.That(result, Is.EqualTo(expectedResult));
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

            Assert.That(result, Is.EqualTo(expectedResult));
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

            Assert.That(result, Is.EqualTo(expectedResult));
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

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_create_collection()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            using (var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding);
                Assert.That(result["ok"].ToBoolean(), Is.True);

                var stats = await GetCollectionStatsAsync(binding);
                Assert.That(stats["ns"].ToString(), Is.EqualTo(_collectionNamespace.FullName));
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

            using (var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding);
                Assert.That(result["ok"].ToBoolean(), Is.True);

                var stats = await GetCollectionStatsAsync(binding);
                Assert.That(stats["ns"].ToString(), Is.EqualTo(_collectionNamespace.FullName));
                Assert.That(stats["systemFlags"].ToInt32(), Is.EqualTo(autoIndexId ? 1 : 0));
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

            using (var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding);
                Assert.That(result["ok"].ToBoolean(), Is.True);

                var stats = await GetCollectionStatsAsync(binding);
                Assert.That(stats["ns"].ToString(), Is.EqualTo(_collectionNamespace.FullName));
                if (capped)
                {
                    Assert.That(stats["capped"].ToBoolean(), Is.True);
                }
                else
                {
                    Assert.That(!stats.Contains("capped"));
                }
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

            using (var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding);
                Assert.That(result["ok"].ToBoolean(), Is.True);

                var stats = await GetCollectionStatsAsync(binding);
                Assert.That(stats["ns"].ToString(), Is.EqualTo(_collectionNamespace.FullName));
                Assert.That(stats["capped"].ToBoolean(), Is.True);
                Assert.That(stats["max"].ToInt64(), Is.EqualTo(maxDocuments));
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

            using (var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding);
                Assert.That(result["ok"].ToBoolean(), Is.True);

                var stats = await GetCollectionStatsAsync(binding);
                Assert.That(stats["ns"].ToString(), Is.EqualTo(_collectionNamespace.FullName));
                Assert.That(stats["capped"].ToBoolean(), Is.True);
                // TODO: not sure how to verify that the maxSize took effect
            }
        }

        [Test]
        [RequiresServer("DropCollection")]
        public async Task ExecuteAsync_should_create_collection_when_UsePowerOf2Sizes_is_set(
            [Values(false, true)]
            bool usePowerOf2Sizes)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                UsePowerOf2Sizes = usePowerOf2Sizes
            };

            using (var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                var result = await subject.ExecuteAsync(binding);
                Assert.That(result["ok"].ToBoolean(), Is.True);

                var stats = await GetCollectionStatsAsync(binding);
                Assert.That(stats["ns"].ToString(), Is.EqualTo(_collectionNamespace.FullName));
                Assert.That(stats["userFlags"].ToInt32(), Is.EqualTo(usePowerOf2Sizes ? 1 : 0));
            }
        }

        [Test]
        public void MaxDocuments_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            Assert.That(subject.MaxDocuments, Is.Null);

            subject.MaxDocuments = 1;

            Assert.That(subject.MaxDocuments, Is.EqualTo(1));
        }

        [Test]
        public void MaxDocuments_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            long maxDocuments)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            var ex = Assert.Catch(() => subject.MaxDocuments = maxDocuments);

            Assert.That(ex, Is.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("value"));
        }

        [Test]
        public void MaxSize_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            Assert.That(subject.MaxSize, Is.Null);

            subject.MaxSize = 1;

            Assert.That(subject.MaxSize, Is.EqualTo(1));
        }

        [Test]
        public void MaxSize_set_should_throw_when_value_is_invalid(
            [Values(-1, 0)]
            long maxSize)
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            var ex = Assert.Catch(() => subject.MaxSize = maxSize);

            Assert.That(ex, Is.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("value"));
        }

        [Test]
        public void MessageEncoderSettings_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            Assert.That(subject.MessageEncoderSettings, Is.SameAs(_messageEncoderSettings));
        }

        [Test]
        public void UsePowerOf2Sizes_should_work()
        {
            var subject = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            Assert.That(subject.UsePowerOf2Sizes, Is.Null);

            subject.UsePowerOf2Sizes = true;

            Assert.That(subject.UsePowerOf2Sizes, Is.True);
        }

        // helper methods
        public void DropCollection()
        {
            var operation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            using (var binding = SuiteConfiguration.GetReadWriteBinding())
            {
                operation.Execute(binding);
            }
        }

        public Task<BsonDocument> GetCollectionStatsAsync(IReadBinding binding)
        {
            var command = new BsonDocument
            {
                { "collStats", _collectionNamespace.CollectionName }
            };
            var operation = new ReadCommandOperation(_collectionNamespace.DatabaseNamespace, command, _messageEncoderSettings);

            return operation.ExecuteAsync(binding);
        }
    }
}
