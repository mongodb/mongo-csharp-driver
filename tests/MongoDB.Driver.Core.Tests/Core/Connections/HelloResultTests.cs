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
using System.Net;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class HelloResultTests
    {
        [Theory]
        [InlineData("{ compression : ['zlib'] }", new[] { CompressorType.Zlib })]
        [InlineData("{ compression : ['Zlib'] }", new[] { CompressorType.Zlib })]
        [InlineData("{ compression : ['zlib', 'snappy'] }", new[] { CompressorType.Zlib, CompressorType.Snappy })]
        [InlineData("{ compression : ['zlib', 'snAppy'] }", new[] { CompressorType.Zlib, CompressorType.Snappy })]
        [InlineData("{ compression : ['noop'] }", new[] { CompressorType.Noop })]
        [InlineData("{ compression : ['nOop'] }", new[] { CompressorType.Noop })]
        [InlineData("{ compression : ['zstd'] }", new[] { CompressorType.ZStandard})]
        [InlineData("{ compression : ['zsTd'] }", new[] { CompressorType.ZStandard })]
        [InlineData("{ compression : [] }", new CompressorType[0])]
        [InlineData("{ }", new CompressorType[0])]
        public void Compression_should_parse_document_correctly(string json, CompressorType[] expectedCompression)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));

            var result = subject.Compressions;

            result.Should().Equal(expectedCompression);
        }

        [Theory]
        [InlineData("{ compression : ['unsupported'] }", "unsupported")]
        [InlineData("{ compression : ['zlib', 'unsupported'] }", "unsupported")]
        public void Compression_should_throw_the_exception_for_an_unsupported_compression_type(string json, string expectedUnsupportedCompressor)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));

            var exception = Record.Exception(() => subject.Compressions);

            var e = exception.Should().BeOfType<NotSupportedException>().Subject;
            e.Message.Should().Be($"The unsupported compressor name: '{expectedUnsupportedCompressor}'.");
        }

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_if_wrapped_is_null()
        {
            Action act = () => new HelloResult(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Wrapped_should_return_the_document_passed_in_the_constructor()
        {
            var doc = new BsonDocument();
            var subject = new HelloResult(doc);

            subject.Wrapped.Should().BeSameAs(doc);
        }

        [Fact]
        public void Equals_should_be_true_when_both_have_the_same_result()
        {
            var subject1 = new HelloResult(new BsonDocument("x", 1));
            var subject2 = new HelloResult(new BsonDocument("x", 1));

            subject1.Equals(subject2).Should().BeTrue();
        }

        [Fact]
        public void Equals_should_be_false_when_both_have_different_results()
        {
            var subject1 = new HelloResult(new BsonDocument("x", 1));
            var subject2 = new HelloResult(new BsonDocument("x", 2));

            subject1.Equals(subject2).Should().BeFalse();
        }

        [Theory]
        [InlineData("{ }", null)]
        [InlineData("{ electionId: ObjectId('555925bfb69aa7d5be29126b') }", "555925bfb69aa7d5be29126b")]
        public void ElectionId_should_parse_document_correctly(string json, string expectedObjectId)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));
            var expected = expectedObjectId == null ? (ElectionId)null : new ElectionId(ObjectId.Parse(expectedObjectId));

            subject.ElectionId.Should().Be(expected);
        }

        [Theory]
        [InlineData("{ lastWrite : { lastWriteDate : ISODate(\"2015-01-01T00:00:00Z\") } }", 2015)]
        [InlineData("{ lastWrite : { lastWriteDate : ISODate(\"2016-01-01T00:00:00Z\") } }", 2016)]
        [InlineData("{ }", null)]
        public void LastWriteTimestamp_should_parse_document_correctly(string json, int? expectedYear)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));

            var result = subject.LastWriteTimestamp;

            var expectedResult = expectedYear.HasValue ? new DateTime(expectedYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc) : (DateTime?)null;
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("{ }", null)]
        [InlineData("{ logicalSessionTimeoutMinutes : null }", null)]
        [InlineData("{ logicalSessionTimeoutMinutes : NumberInt(1) }", 1)]
        [InlineData("{ logicalSessionTimeoutMinutes : NumberLong(2) }", 2)]
        [InlineData("{ logicalSessionTimeoutMinutes : 3.0 }", 3)]
        public void LogicalSessionTimeout_should_parse_document_correctly(string json, int? expectedResultMinutes)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));
            var expectedResult = expectedResultMinutes == null ? (TimeSpan?)null : TimeSpan.FromMinutes(expectedResultMinutes.Value);

            var result = subject.LogicalSessionTimeout;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("{ maxWriteBatchSize: 100 }", 100)]
        [InlineData("{ maxWriteBatchSize: 0 }", 0)]
        [InlineData("{ }", 1000)]
        public void MaxBatchCount_should_parse_document_correctly(string json, int expected)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));

            subject.MaxBatchCount.Should().Be(expected);
        }

        [Theory]
        [InlineData("{ maxBsonObjectSize: 100 }", 100)]
        [InlineData("{ maxBsonObjectSize: 0 }", 0)]
        [InlineData("{ }", 4 * 1024 * 1024)]
        public void MaxDocumentSize_should_parse_document_correctly(string json, int expected)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));

            subject.MaxDocumentSize.Should().Be(expected);
        }

        [Theory]
        [InlineData("{ maxMessageSizeBytes: 100 }", 100)]
        [InlineData("{ maxMessageSizeBytes: 0 }", 0)]
        [InlineData("{ maxBsonObjectSize: 16000000 }", 16001024)]
        [InlineData("{ }", 16000000)]
        public void MaxMessageSize_should_parse_document_correctly(string json, int expected)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));

            subject.MaxMessageSize.Should().Be(expected);
        }

        [Theory]
        [InlineData("{ maxWireVersion: 100 }", 100)]
        [InlineData("{ maxWireVersion: 0 }", 0)]
        [InlineData("{ }", 0)]
        public void MaxWireVersion_should_parse_document_correctly(string json, int expected)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));

            subject.MaxWireVersion.Should().Be(expected);
        }

        [Theory]
        [InlineData("{ minWireVersion: 100 }", 100)]
        [InlineData("{ minWireVersion: 0 }", 0)]
        [InlineData("{ }", 0)]
        public void MinWireVersion_should_parse_document_correctly(string json, int expected)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));

            subject.MinWireVersion.Should().Be(expected);
        }

        [Theory]
        [InlineData("{ }", null)]
        [InlineData("{ me: 'localhost:27018' }", "localhost:27018")]
        public void Me_should_parse_document_correctly(string json, string expectedEndPoint)
        {
            var endPoint = expectedEndPoint == null ? (EndPoint)null : EndPointHelper.Parse(expectedEndPoint);

            var subject = new HelloResult(BsonDocument.Parse(json));

            subject.Me.Should().Be(endPoint);
        }

        [Theory]
        [InlineData("{ ok: 1, isreplicaset: true, setName: \"awesome\", isWritablePrimary: true }", ServerType.ReplicaSetGhost)]
        [InlineData("{ ok: 1, setName: \"awesome\", " + OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName + ": true }", ServerType.ReplicaSetPrimary)]
        [InlineData("{ ok: 1, setName: \"awesome\", isWritablePrimary: true }", ServerType.ReplicaSetPrimary)]
        [InlineData("{ ok: 1, setName: \"awesome\", isWritablePrimary: true, secondary: true }", ServerType.ReplicaSetPrimary)]
        [InlineData("{ ok: 1, setName: \"awesome\", secondary: true }", ServerType.ReplicaSetSecondary)]
        [InlineData("{ ok: 1, setName: \"awesome\", secondary: true, passive: true }", ServerType.ReplicaSetSecondary)]
        [InlineData("{ ok: 1, setName: \"awesome\", arbiterOnly: true }", ServerType.ReplicaSetArbiter)]
        [InlineData("{ ok: 1, setName: \"awesome\", isWritablePrimary: false, secondary: false, arbiterOnly: false }", ServerType.ReplicaSetOther)]
        [InlineData("{ ok: 1, setName: \"awesome\", isWritablePrimary: false, secondary: false }", ServerType.ReplicaSetOther)]
        [InlineData("{ ok: 1, setName: \"awesome\", isWritablePrimary: false }", ServerType.ReplicaSetOther)]
        [InlineData("{ ok: 1, setName: \"awesome\", secondary: false }", ServerType.ReplicaSetOther)]
        [InlineData("{ ok: 1, setName: \"awesome\", arbiterOnly: false }", ServerType.ReplicaSetOther)]
        [InlineData("{ ok: 1, setName: \"awesome\", secondary: true, hidden: true }", ServerType.ReplicaSetOther)]
        [InlineData("{ ok: 1, setName: \"awesome\", secondary: true, hidden: false }", ServerType.ReplicaSetSecondary)]
        [InlineData("{ ok: 1, setName: \"awesome\" }", ServerType.ReplicaSetOther)]
        [InlineData("{ ok: 1, isreplicaset: true }", ServerType.ReplicaSetGhost)]
        [InlineData("{ ok: 1, isreplicaset: 1 }", ServerType.ReplicaSetGhost)]
        [InlineData("{ ok: 1, isreplicaset: false }", ServerType.Standalone)]
        [InlineData("{ ok: 1, isreplicaset: 0 }", ServerType.Standalone)]
        [InlineData("{ ok: 1, msg: \"isdbgrid\" }", ServerType.ShardRouter)]
        [InlineData("{ ok: 1, msg: \"isdbgrid\" }", ServerType.ShardRouter)]
        [InlineData("{ ok: 1, serviceId: ObjectId('111111111111111111111111') }", ServerType.LoadBalanced)]
        [InlineData("{ ok: 1 }", ServerType.Standalone)]
        [InlineData("{ ok: 0 }", ServerType.Unknown)]
        public void ServerType_should_parse_document_correctly(string json, ServerType expected)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));

            subject.ServerType.Should().Be(expected);
        }

        [Theory]
        [InlineData("{ }", false)]
        [InlineData("{ serviceId : ObjectId('000000000000000000000000') }", true)]
        public void ServiceId_should_parse_document_correctly(string json, bool shouldBeParsed)
        {
            var subject = new HelloResult(BsonDocument.Parse(json));

            subject.ServiceId.HasValue.Should().Be(shouldBeParsed);
            if (shouldBeParsed)
            {
                subject.ServiceId.Should().Be(ObjectId.Empty);
            }
        }

        [Fact]
        public void Tags_should_be_null_when_no_tags_exist()
        {
            var subject = new HelloResult(new BsonDocument());

            subject.Tags.Should().BeNull();
        }

        [Fact]
        public void Tags_should_parse_document_correctly()
        {
            var subject = new HelloResult(BsonDocument.Parse("{ tags: { a: \"one\", b: \"two\" } }"));
            var expected = new TagSet(new[] { new Tag("a", "one"), new Tag("b", "two") });

            subject.Tags.Should().Be(expected);
        }

        [Fact]
        public void GetReplicaSetConfig_should_return_correct_info_when_the_server_is_a_replica_set()
        {
            var doc = new BsonDocument
            {
                { "ok", 1 },
                { "setName", "funny" },
                { "primary", "localhost:1000" },
                { "hosts", new BsonArray(new [] { "localhost:1000", "localhost:1001" })},
                { "passives", new BsonArray(new [] { "localhost:1002"}) },
                { "arbiters", new BsonArray(new [] { "localhost:1003"}) },
                { "setVersion", 20 }
            };

            var subject = new HelloResult(doc);
            var config = subject.GetReplicaSetConfig();

            config.Name.Should().Be("funny");
            config.Primary.Should().Be(new DnsEndPoint("localhost", 1000));
            config.Members.Should().BeEquivalentTo(
                new DnsEndPoint("localhost", 1000),
                new DnsEndPoint("localhost", 1001),
                new DnsEndPoint("localhost", 1002),
                new DnsEndPoint("localhost", 1003));
            config.Version.Should().Be(20);
        }

        [Fact]
        public void HelloOk_should_return_true_when_response_contains_helloOk_true()
        {
            var doc = new BsonDocument
            {
                { "ok", 1 },
                { "helloOk", true }
            };

            var subject = new HelloResult(doc);
            subject.HelloOk.Should().BeTrue();
        }

        [Fact]
        public void HelloOk_should_return_false_when_response_does_not_contain_helloOk()
        {
            var doc = new BsonDocument
            {
                { "ok", 1 }
            };

            var subject = new HelloResult(doc);
            subject.HelloOk.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void IsMongocryptd_should_return_expected_result([Values(false, true, null)] bool? isMongocryptd)
        {
            var helloResultDocument = new BsonDocument
            {
                { "iscryptd", () => isMongocryptd.Value, isMongocryptd.HasValue }
            };

            var subject = new HelloResult(helloResultDocument);

            subject.IsMongocryptd.Should().Be(isMongocryptd.GetValueOrDefault());
        }
    }
}
