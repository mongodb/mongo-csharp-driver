﻿/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class GroupOperationTests : OperationTestBase
    {
        private readonly BsonDocument _filter = BsonDocument.Parse("{ y : 'a' }");
        private readonly BsonJavaScript _finalizeFunction = new BsonJavaScript("function(result) { result.count = -result.count; }");
        private readonly BsonDocument _initial = BsonDocument.Parse("{ count : 0.0 }");
        private readonly BsonDocument _key = BsonDocument.Parse("{ x : 1 }");
        private readonly BsonJavaScript _keyFunction = new BsonJavaScript("function(doc) { return { x : doc.x }; }");
        private readonly BsonJavaScript _reduceFunction = new BsonJavaScript("function(doc, result) { result.count += 1; }");

        [Fact]
        public void constructor_with_key_should_initialize_subject()
        {
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, _filter, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.Key.Should().BeSameAs(_key);
            subject.Initial.Should().BeSameAs(_initial);
            subject.ReduceFunction.Should().BeSameAs(_reduceFunction);
            subject.Filter.Should().BeSameAs(_filter);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.Collation.Should().BeNull();
            subject.FinalizeFunction.Should().BeNull();
            subject.KeyFunction.Should().BeNull();
            subject.MaxTime.Should().NotHaveValue();
            subject.ResultSerializer.Should().BeNull();
        }

        [Fact]
        public void constructor_with_key_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new GroupOperation<BsonDocument>(null, _key, _initial, _reduceFunction, _filter, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_with_key_should_throw_when_key_is_null()
        {
            var exception = Record.Exception(() => new GroupOperation<BsonDocument>(_collectionNamespace, (BsonDocument)null, _initial, _reduceFunction, _filter, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("key");
        }

        [Fact]
        public void constructor_with_key_should_throw_when_initial_is_null()
        {
            var exception = Record.Exception(() => new GroupOperation<BsonDocument>(_collectionNamespace, _key, null, _reduceFunction, _filter, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("initial");
        }

        [Fact]
        public void constructor_with_key_should_throw_when_reduceFunction_is_null()
        {
            var exception = Record.Exception(() => new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, null, _filter, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("reduceFunction");
        }

        [Fact]
        public void constructor_with_keyFunction_should_initialize_subject()
        {
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _keyFunction, _initial, _reduceFunction, _filter, _messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(_collectionNamespace);
            subject.KeyFunction.Should().Be(_keyFunction);
            subject.Initial.Should().Be(_initial);
            subject.ReduceFunction.Should().Be(_reduceFunction);
            subject.Filter.Should().Be(_filter);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);

            subject.Collation.Should().BeNull();
            subject.FinalizeFunction.Should().BeNull();
            subject.Key.Should().BeNull();
            subject.MaxTime.Should().Be(default(TimeSpan?));
            subject.ResultSerializer.Should().BeNull();
        }

        [Fact]
        public void constructor_with_keyFunction_should_throw_when_collectionNamespace_is_null()
        {
            var exception = Record.Exception(() => new GroupOperation<BsonDocument>(null, _keyFunction, _initial, _reduceFunction, _filter, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_with_keyFunction_should_throw_when_keyFunction_is_null()
        {
            var exception = Record.Exception(() => new GroupOperation<BsonDocument>(_collectionNamespace, (BsonJavaScript)null, _initial, _reduceFunction, _filter, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("keyFunction");
        }

        [Fact]
        public void constructor_with_keyFunction_should_throw_when_initial_is_null()
        {
            var exception = Record.Exception(() => new GroupOperation<BsonDocument>(_collectionNamespace, _keyFunction, null, _reduceFunction, _filter, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("initial");
        }

        [Fact]
        public void constructor_with_keyFunction_should_throw_when_reduceFunction_is_null()
        {
            var exception = Record.Exception(() => new GroupOperation<BsonDocument>(_collectionNamespace, _keyFunction, _initial, null, _filter, _messageEncoderSettings));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("reduceFunction");
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, _filter, _messageEncoderSettings);
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void FinalizeFunction_get_and_set_should_work(
            [Values(null, "x", "y")]
            string code)
        {
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, _filter, _messageEncoderSettings);
            var value = code == null ? null : new BsonJavaScript(code);

            subject.FinalizeFunction = value;
            var result = subject.FinalizeFunction;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_get_and_set_should_work(
            [Values(-10000, 0, 1, 10000, 99999)] long maxTimeTicks)
        {
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, _filter, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            subject.MaxTime = value;
            var result = subject.MaxTime;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_throw_when_value_is_invalid(
            [Values(-10001, -9999, -1)] long maxTimeTicks)
        {
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, _filter, _messageEncoderSettings);
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var exception = Record.Exception(() => subject.MaxTime = value);

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void ResultSerializer_get_and_set_should_work(
            [Values(false, true)]
            bool isNull)
        {
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, _filter, _messageEncoderSettings);
            var value = isNull ? null : new BsonDocumentSerializer();

            subject.ResultSerializer = value;
            var result = subject.ResultSerializer;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_key_is_used(
            [Values(false, true)]
            bool useFilter)
        {
            var filter = useFilter ? _filter : null;
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, filter, _messageEncoderSettings);

            var result = subject.CreateCommand();

            var expectedResult = new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", _collectionNamespace.CollectionName },
                        { "key", _key },
                        { "$reduce", _reduceFunction },
                        { "initial", _initial },
                        { "cond", filter, filter != null }
                    }
                }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_keyFunction_is_used(
            [Values(false, true)]
            bool isFilterNull)
        {
            var filter = isFilterNull ? _filter : null;
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _keyFunction, _initial, _reduceFunction, filter, _messageEncoderSettings);

            var result = subject.CreateCommand();

            var expectedResult = new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", _collectionNamespace.CollectionName },
                        { "$keyf", _keyFunction },
                        { "$reduce", _reduceFunction },
                        { "initial", _initial },
                        { "cond", filter, filter != null }
                    }
                }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result_when_finalizeFunction_is_set(
            [Values(false, true)]
            bool isFinalizeFunctionNull)
        {
            var finalizeFunction = isFinalizeFunctionNull ? null : _finalizeFunction;
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, null, _messageEncoderSettings)
            {
                FinalizeFunction = finalizeFunction
            };

            var result = subject.CreateCommand();

            var expectedResult = new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", _collectionNamespace.CollectionName },
                        { "key", _key },
                        { "$reduce", _reduceFunction },
                        { "initial", _initial },
                        { "finalize", finalizeFunction, finalizeFunction != null }
                    }
                }
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
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, null, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = subject.CreateCommand();

            var expectedResult = new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", _collectionNamespace.CollectionName },
                        { "key", _key },
                        { "$reduce", _reduceFunction },
                        { "initial", _initial },
                        { "collation", () => collation.ToBsonDocument(), collation != null }
                    }
                }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(-10000, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(9999, 1)]
        [InlineData(10000, 1)]
        [InlineData(10001, 2)]
        public void CreateCommand_should_return_expected_result_when_MaxTime_is_set(long maxTimeTicks, int expectedMaxTimeMS)
        {
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, null, _messageEncoderSettings)
            {
                MaxTime = TimeSpan.FromTicks(maxTimeTicks)
            };

            var result = subject.CreateCommand();

            var expectedResult = new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", _collectionNamespace.CollectionName },
                        { "key", _key },
                        { "$reduce", _reduceFunction },
                        { "initial", _initial }
                    }
                },
                { "maxTimeMS", expectedMaxTimeMS }
            };
            result.Should().Be(expectedResult);
            result["maxTimeMS"].BsonType.Should().Be(BsonType.Int32);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_key_is_used(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.GroupCommand);
            EnsureTestData();
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, null, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result.Should().Equal(
                BsonDocument.Parse("{ x : 1, count : 2 }"),
                BsonDocument.Parse("{ x : 2, count : 1 }"),
                BsonDocument.Parse("{ x : 3, count : 3 }"));
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_keyFunction_is_used(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.GroupCommand);
            EnsureTestData();
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _keyFunction, _initial, _reduceFunction, null, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result.Should().Equal(
                BsonDocument.Parse("{ x : 1, count : 2 }"),
                BsonDocument.Parse("{ x : 2, count : 1 }"),
                BsonDocument.Parse("{ x : 3, count : 3 }"));
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_Collation_is_set(
            [Values(false, true)]
            bool caseSensitive,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.GroupCommand);
            EnsureTestData();
            var collation = new Collation("en_US", caseLevel: caseSensitive, strength: CollationStrength.Primary);
            var filter = BsonDocument.Parse("{ y : 'a' }");
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, filter, _messageEncoderSettings)
            {
                Collation = collation
            };

            var result = ExecuteOperation(subject, async);

            BsonDocument[] expectedResult;
            if (caseSensitive)
            {
                expectedResult = new[]
                {
                    BsonDocument.Parse("{ x : 1, count : 2 }"),
                    BsonDocument.Parse("{ x : 3, count : 2 }")
                };
            }
            else
            {
                expectedResult = new[]
                {
                    BsonDocument.Parse("{ x : 1, count : 2 }"),
                    BsonDocument.Parse("{ x : 2, count : 1 }"),
                    BsonDocument.Parse("{ x : 3, count : 3 }")
                };
            }
            result.Should().Equal(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_FinalizeFunction_is_set(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.GroupCommand);
            EnsureTestData();
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, null, _messageEncoderSettings)
            {
                FinalizeFunction = _finalizeFunction
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Equal(
                BsonDocument.Parse("{ x : 1, count : -2 }"),
                BsonDocument.Parse("{ x : 2, count : -1 }"),
                BsonDocument.Parse("{ x : 3, count : -3 }"));
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_MaxTime_is_used(
            [Values(null, 1000)]
            int? seconds,
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.GroupCommand);
            EnsureTestData();
            var maxTime = seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, null, _messageEncoderSettings)
            {
                MaxTime = maxTime
            };

            // TODO: force a timeout on the server? for now we're just smoke testing
            var result = ExecuteOperation(subject, async);

            result.Should().Equal(
                BsonDocument.Parse("{ x : 1, count : 2 }"),
                BsonDocument.Parse("{ x : 2, count : 1 }"),
                BsonDocument.Parse("{ x : 3, count : 3 }"));
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_ResultSerializer_is_used(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Supports(Feature.GroupCommand);
            EnsureTestData();
            var resultSerializer = new ElementDeserializer<int>("x", new Int32Serializer());
            var subject = new GroupOperation<int>(_collectionNamespace, _key, _initial, _reduceFunction, null, _messageEncoderSettings)
            {
                ResultSerializer = resultSerializer
            };

            var result = ExecuteOperation(subject, async);

            result.Should().Equal(1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, _filter, _messageEncoderSettings);

            var exception = Record.Exception(() =>
            {
                if (async)
                {
                    subject.ExecuteAsync(null, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Execute(null, CancellationToken.None);
                }
            });

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("binding");
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_send_session_id_when_supported(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.GroupCommand);
            EnsureTestData();
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, null, _messageEncoderSettings);

            VerifySessionIdWasSentWhenSupported(subject, "group", async);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_maxTime_is_exceeded(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.GroupCommand).ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet);
            var subject = new GroupOperation<BsonDocument>(_collectionNamespace, _key, _initial, _reduceFunction, null, _messageEncoderSettings);
            subject.MaxTime = TimeSpan.FromSeconds(9001);

            using (var failPoint = FailPoint.ConfigureAlwaysOn(_cluster, _session, FailPointName.MaxTimeAlwaysTimeout))
            {
                var exception = Record.Exception(() => ExecuteOperation(subject, failPoint.Binding, async));

                exception.Should().BeOfType<MongoExecutionTimeoutException>();
            }
        }

        // helper methods
        private void EnsureTestData()
        {
            RunOncePerFixture(() =>
            {
                DropCollection();
                Insert(
                    BsonDocument.Parse("{ _id : 1, x : 1, y : 'a' }"),
                    BsonDocument.Parse("{ _id : 2, x : 1, y : 'a' }"),
                    BsonDocument.Parse("{ _id : 3, x : 2, y : 'A' }"),
                    BsonDocument.Parse("{ _id : 4, x : 3, y : 'a' }"),
                    BsonDocument.Parse("{ _id : 5, x : 3, y : 'a' }"),
                    BsonDocument.Parse("{ _id : 6, x : 3, y : 'A' }")
                    );
                CreateIndexes(new CreateIndexRequest(new BsonDocument("Location", "2d")));
            });
        }
    }
}
