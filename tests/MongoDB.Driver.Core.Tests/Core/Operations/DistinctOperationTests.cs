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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class DistinctOperationTests : OperationTestBase
    {
        private string _fieldName;
        private IBsonSerializer<int> _valueSerializer;

        public DistinctOperationTests()
        {
            _fieldName = "y";
            _valueSerializer = new Int32Serializer();
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.ValueSerializer.Should().BeSameAs(_valueSerializer);
            subject.FieldName.Should().BeSameAs(_fieldName);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.Collation.Should().BeNull();
            subject.Filter.Should().BeNull();
            subject.MaxTime.Should().NotHaveValue();
            subject.ReadConcern.Should().BeSameAs(ReadConcern.Default);
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new DistinctOperation<int>(null, _valueSerializer, _fieldName, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_valueSerializer_is_null()
        {
            var exception = Record.Exception(() => new DistinctOperation<int>(_collectionNamespace, null, _fieldName, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("valueSerializer");
        }

        [Fact]
        public void constructor_should_throw_when_fieldName_is_null()
        {
            var exception = Record.Exception(() => new DistinctOperation<int>(_collectionNamespace, _valueSerializer, null, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("fieldName");
        }

        [Fact]
        public void constructor_should_throw_when_messageEncoderSettings_is_null()
        {
            var exception = Record.Exception(() => new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("messageEncoderSettings");
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Filter_get_and_set_should_work(
            [Values(null, "{ x :  1}", "{ x : 2 }")]
            string filterString)
        {
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings);
            var value = filterString == null ? null : BsonDocument.Parse(filterString);

            subject.Filter = value;
            var result = subject.Filter;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? seconds)
        {
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings);
            var value = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadConcern_get_and_set_should_work(
            [Values(null, ReadConcernLevel.Local, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings);
            var value = level.HasValue ? new ReadConcern(level) : ReadConcern.Default;

            subject.ReadConcern = value;
            var result = subject.ReadConcern;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings);

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "distinct", _collectionNamespace.CollectionName },
                { "key", _fieldName }
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
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateCommand(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "distinct", _collectionNamespace.CollectionName },
                { "key", _fieldName },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_Filter_is_set(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string filterString)
        {
            var filter = filterString == null ? null : BsonDocument.Parse(filterString);
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                Filter = filter
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "distinct", _collectionNamespace.CollectionName },
                { "key", _fieldName },
                { "query", filter, filter != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_MaxTime_is_set(
            [Values(null, 1, 2)]
            int? milliseconds)
        {
            var maxTime = milliseconds.HasValue ? TimeSpan.FromMilliseconds(milliseconds.Value) : (TimeSpan?)null;
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            var result = subject.CreateCommand(null);

            var expectedResult = new BsonDocument
            {
                { "distinct", _collectionNamespace.CollectionName },
                { "key", _fieldName },
                { "maxTimeMS", () => maxTime.Value.TotalMilliseconds, maxTime.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_ReadConcern_is_set(
            [Values(null, ReadConcernLevel.Linearizable, ReadConcernLevel.Local)]
            ReadConcernLevel? level)
        {
            var readConcern = level.HasValue ? new ReadConcern(level.Value) : ReadConcern.Default;
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                ReadConcern = readConcern
            };

            var result = subject.CreateCommand(Feature.ReadConcern.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "distinct", _collectionNamespace.CollectionName },
                { "key", _fieldName },
                { "readConcern", () => readConcern.ToBsonDocument(), !readConcern.IsServerDefault }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_throw_when_Collation_is_set_and_not_supported()
        {
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.Collation.LastNotSupportedVersion));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void CreateCommand_should_throw_when_ReadConcern_is_set_and_not_supported()
        {
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                ReadConcern = new ReadConcern(ReadConcernLevel.Local)
            };

            var exception = Record.Exception(() => subject.CreateCommand(Feature.ReadConcern.LastNotSupportedVersion));

            exception.Should().BeOfType<MongoClientException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings);

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(3);
            result.Should().OnlyHaveUniqueItems();
            result.Should().Contain(new[] { 1, 2, 3 });
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Collation_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.Collation);
            EnsureTestData();
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                Collation = new Collation("en_US", caseLevel: false, strength: CollationStrength.Primary),
                Filter = BsonDocument.Parse("{ x : \"d\" }")
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(2);
            result.Should().OnlyHaveUniqueItems();
            result.Should().Contain(new[] { 2, 3 });
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Filter_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                Filter = BsonDocument.Parse("{ _id : { $gt : 2 } }")
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(2);
            result.Should().OnlyHaveUniqueItems();
            result.Should().Contain(new[] { 2, 3 });
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_MaxTime_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureTestData();
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                MaxTime = TimeSpan.FromSeconds(1)
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(3);
            result.Should().OnlyHaveUniqueItems();
            result.Should().Contain(new[] { 1, 2, 3 });
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_ReadConcern_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.ReadConcern);
            EnsureTestData();
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                ReadConcern = new ReadConcern(ReadConcernLevel.Local)
            };

            var cursor = ExecuteOperation(subject, async);
            var result = ReadCursorToEnd(cursor, async);

            result.Should().HaveCount(3);
            result.Should().OnlyHaveUniqueItems();
            result.Should().Contain(new[] { 1, 2, 3 });
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_Collation_is_set_and_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().DoesNotSupport(Feature.Collation);
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_ReadConcern_is_set_and_not_supported(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().DoesNotSupport(Feature.ReadConcern);
            var subject = new DistinctOperation<int>(_collectionNamespace, _valueSerializer, _fieldName, _messageEncoderSettings)
            {
                ReadConcern = new ReadConcern(ReadConcernLevel.Local)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoClientException>();
        }

        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(
                    new BsonDocument { { "_id", 1 }, { "x", "a" }, { "y", 1 } },
                    new BsonDocument { { "_id", 2 }, { "x", "b" }, { "y", 1 } },
                    new BsonDocument { { "_id", 3 }, { "x", "c" }, { "y", 2 } },
                    new BsonDocument { { "_id", 4 }, { "x", "d" }, { "y", 2 } },
                    new BsonDocument { { "_id", 5 }, { "x", "D" }, { "y", 3 } }
                );
            });
        }
    }
}
