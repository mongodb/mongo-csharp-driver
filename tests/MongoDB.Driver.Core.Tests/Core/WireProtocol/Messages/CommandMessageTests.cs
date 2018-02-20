/* Copyright 2018-present MongoDB Inc.
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    public class CommandMessageTests
    {
        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_instance(
            [Values(1, 2)] int requestId,
            [Values(3, 4)] int responseTo,
            [Values(1, 2, 3)] int numberOfSections,
            [Values(false, true)] bool moreToCome)
        {
            var sections = CreateSections(numberOfSections);

            var result = new CommandMessage(requestId, responseTo, sections, moreToCome);

            result.MoreToCome.Should().Be(moreToCome);
            result.RequestId.Should().Be(requestId);
            result.ResponseTo.Should().Be(responseTo);
            result.Sections.Should().Equal(sections, CommandMessageSectionEqualityComparer.Instance.Equals);
        }

        [Fact]
        public void MessageType_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.MessageType;

            result.Should().Be(MongoDBMessageType.Command);
        }

        [Theory]
        [ParameterAttributeData]
        public void MoreToCome_should_return_expected_result(
            [Values(false, true)] bool moreToCome)
        {
            var subject = CreateSubject(moreToCome: moreToCome);

            var result = subject.MoreToCome;

            result.Should().Be(moreToCome);
        }

        [Theory]
        [ParameterAttributeData]
        public void RequestId_should_return_expected_result(
            [Values(1, 2)] int requestId)
        {
            var subject = CreateSubject(requestId: requestId);

            var result = subject.RequestId;

            result.Should().Be(requestId);
        }

        [Theory]
        [ParameterAttributeData]
        public void ResponseTo_should_return_expected_result(
            [Values(1, 2)] int responseTo)
        {
            var subject = CreateSubject(responseTo: responseTo);

            var result = subject.ResponseTo;

            result.Should().Be(responseTo);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sections_should_return_expected_result(
            [Values(1, 2, 3)] int numberOfSections)
        {
            var sections = CreateSections(numberOfSections);
            var subject = CreateSubject(sections: sections);

            var result = subject.Sections;

            result.Should().Equal(sections, CommandMessageSectionEqualityComparer.Instance.Equals);
        }

        [Fact]
        public void GetEncoder_should_return_a_CommandMessageEncoder()
        {
            var subject = CreateSubject();
            var stream = new MemoryStream();
            var encoderSettings = new MessageEncoderSettings();
            var encoderFactory = new BinaryMessageEncoderFactory(stream, encoderSettings);

            var result = subject.GetEncoder(encoderFactory);

            result.Should().BeOfType<CommandMessageBinaryEncoder>();
        }

        // private methods
        private IReadOnlyList<CommandMessageSection> CreateSections(int numberOfSections)
        {
            var sections = new List<CommandMessageSection>();
            sections.Add(CreateType0Section());
            sections.AddRange(Enumerable.Range(0, numberOfSections - 1).Select(n => CreateType1Section(n)));
            return sections;
        }

        private CommandMessage CreateSubject(
            int requestId = 1,
            int responseTo = 2,
            IEnumerable<CommandMessageSection> sections = null,
            bool moreToCome = false)
        {
            sections = sections ?? new[] { CreateType0Section() };
            return new CommandMessage(requestId, responseTo, sections, moreToCome);
        }

        private Type0CommandMessageSection<BsonDocument> CreateType0Section()
        {
            var document = new BsonDocument("x", 1);
            return new Type0CommandMessageSection<BsonDocument>(document, BsonDocumentSerializer.Instance);
        }

        private Type1CommandMessageSection<BsonDocument> CreateType1Section(int n)
        {
            var identifier = $"id-{n}";
            var items = Enumerable.Range(0, n + 1).Select(x => new BsonDocument("x", x)).ToList();
            var documents = new BatchableSource<BsonDocument>(items, canBeSplit: false);
            return new Type1CommandMessageSection<BsonDocument>(identifier, documents, BsonDocumentSerializer.Instance);
        }
    }
}
