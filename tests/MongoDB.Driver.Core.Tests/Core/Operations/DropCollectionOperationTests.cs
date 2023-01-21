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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Tests.Core.Operations;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class DropCollectionOperationTests : OperationTestBase
    {
        // test methods
        [Fact]
        public void CollectionNamespace_get_should_return_expected_result()
        {
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().BeSameAs(_collectionNamespace);
        }

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.EncryptedFields.Should().BeNull();
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            subject.WriteConcern.Should().BeNull();
        }

        [Fact]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            Action action = () => { new DropCollectionOperation(null, _messageEncoderSettings); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "drop", _collectionNamespace.CollectionName }
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_WriteConcern_is_set(
            [Values(null, 1, 2)]
            int? w)
        {
            var writeConcern = w.HasValue ? new WriteConcern(w.Value) : null;
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };
            var session = OperationTestHelper.CreateSession();

            var result = subject.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                { "drop", _collectionNamespace.CollectionName },
                { "writeConcern", () => writeConcern.ToBsonDocument(), writeConcern != null }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateEncryptedDropCollectionOperationIfConfigured_should_return_expected_result_when_EncryptedFields_is_null()
        {
            var subject = DropCollectionOperation.CreateEncryptedDropCollectionOperationIfConfigured(_collectionNamespace, encryptedFields: null, _messageEncoderSettings, null);
            var session = OperationTestHelper.CreateSession();

            var s = subject.Should().BeOfType<DropCollectionOperation>().Subject;

            var command = s.CreateCommand(session);

            var expectedResult = new BsonDocument
            {
                 { "drop", _collectionNamespace.CollectionName },
            };
            command.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(new[] { "'escCollection' : 'escCollectionName'", "escCollectionName" }, new[] { "'eccCollection' : 'eccCollectionName'", "eccCollectionName" }, new[] { "'ecocCollection' : 'ecocCollectionName'", "ecocCollectionName" })]
        [InlineData(new[] { null, "esc" }, new[] { null, "ecc" }, new[] { null, "ecoc" })]
        public void CreateEncryptedDropCollectionOperationIfConfigured_should_return_expected_result_when_EncryptedFields_is_set(string[] escCollectionStrElement, string[] eccCollectionStrElement, string[] ecocCollectionStrElement)
        {
            var encryptedFields = BsonDocument.Parse($@"
            {{
                {GetFirstElementWithCommaOrEmpty(escCollectionStrElement)}
                {GetFirstElementWithCommaOrEmpty(eccCollectionStrElement)}
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

            var subject = DropCollectionOperation.CreateEncryptedDropCollectionOperationIfConfigured(_collectionNamespace, encryptedFields, _messageEncoderSettings, null);
            var session = OperationTestHelper.CreateSession();
            
            var operations = ((CompositeWriteOperation<BsonDocument>)subject)._operations<BsonDocument>();

            // esc
            AssertDropCollectionCommand(
                operations[0],
                new CollectionNamespace(_collectionNamespace.DatabaseNamespace.DatabaseName, GetExpectedCollectionName(escCollectionStrElement)),
                encryptedFields: null,
                isMainOperation: false);
            // ecc
            AssertDropCollectionCommand(
                operations[1],
                new CollectionNamespace(_collectionNamespace.DatabaseNamespace.DatabaseName, GetExpectedCollectionName(eccCollectionStrElement)),
                encryptedFields: null,
                isMainOperation: false);
            // eco
            AssertDropCollectionCommand(
                operations[2],
                new CollectionNamespace(_collectionNamespace.DatabaseNamespace.DatabaseName, GetExpectedCollectionName(ecocCollectionStrElement)),
                encryptedFields: null,
                isMainOperation: false);

            // main
            AssertDropCollectionCommand(
                operations[3],
                _collectionNamespace,
                encryptedFields,
                isMainOperation: true);

            void AssertDropCollectionCommand((IWriteOperation<BsonDocument> Operation, bool IsMainOperation) operationInfo, CollectionNamespace collectionNamespace, BsonDocument encryptedFields, bool isMainOperation)
            {
                operationInfo.IsMainOperation.Should().Be(isMainOperation);
                var operation = operationInfo.Operation.Should().BeOfType<DropCollectionOperation>().Subject;
                var result = operation.CreateCommand(session);
                var expectedResult = new BsonDocument
                {
                    { "drop", collectionNamespace.CollectionName },
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
        public void Execute_should_not_throw_when_collection_does_not_exist(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            DropCollection();
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            ExecuteOperation(subject, async); // this will throw if we have a problem...
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            EnsureCollectionExists();
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result["ok"].ToBoolean().Should().BeTrue();
            if (result.Contains("ns"))
            {
                result["ns"].ToString().Should().Be(_collectionNamespace.FullName);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_a_write_concern_error_occurs(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().ClusterType(ClusterType.ReplicaSet);
            EnsureCollectionExists();
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings)
            {
                WriteConcern = new WriteConcern(9)
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async));

            exception.Should().BeOfType<MongoWriteConcernException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            EnsureCollectionExists();
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "drop", async);
        }

        [Fact]
        public void MessageEncoderSettings_get_should_return_expected_result()
        {
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? w)
        {
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var value = w.HasValue ? new WriteConcern(w.Value) : null;

            subject.WriteConcern = value;
            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        // private methods
        private void EnsureCollectionExists()
        {
            DropCollection();
            var operation = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            ExecuteOperation(operation, false);
        }
    }
}
