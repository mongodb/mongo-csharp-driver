/* Copyright 2010-present MongoDB Inc.
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
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class BsonWriterTests
    {
        [Fact]
        public void WriteName_should_throw_when_value_contains_nulls()
        {
            using (var stream = new MemoryStream())
            using (var bsonWriter = new BsonBinaryWriter(stream, BsonBinaryWriterSettings.Defaults))
            {
                Assert.Throws<BsonSerializationException>(() => { bsonWriter.WriteName("a\0b"); });
            }
        }

        [Fact]
        public void WriteName_should_throw_when_value_is_null()
        {
            using (var stream = new MemoryStream())
            using (var bsonWriter = new BsonBinaryWriter(stream, BsonBinaryWriterSettings.Defaults))
            {
                Assert.Throws<ArgumentNullException>(() => { bsonWriter.WriteName(null); });
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BsonWriter_should_fail_on_invalid_child_elements_names(
            [Values(1, 2)]
            int levelsCount,
            [Values(true, false)]
            bool addValidatorOnRoot)
        {
            var levelNames = Enumerable.Range(0, levelsCount).Select(i => $"level{i}").ToArray();
            var validatorsMocks = CreatePrefixValidatorsNestedMock(levelNames);

            using (var stream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(stream))
            {
                if (addValidatorOnRoot)
                {
                    writer.PushElementNameValidator(validatorsMocks[0].Object);
                }

                writer.WriteStartDocument();

                if (!addValidatorOnRoot)
                {
                    writer.PushElementNameValidator(validatorsMocks[0].Object);
                }

                foreach (var levelName in levelNames)
                {
                    Record.Exception(() => writer.WriteInt32($"wrongname", 1)).Should().BeOfType<BsonSerializationException>();

                    writer.WriteInt32($"{levelName}_int", 1);
                    writer.WriteStartDocument($"{levelName}_nested");
                }

                for (int i = 0; i < levelsCount; i++)
                {
                    writer.WriteEndDocument(); // $"{levelName}_nested"
                }

                writer.WriteEndDocument();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BsonWriter_should_validate_child_elements_names(
            [Values(1, 2, 3)]
            int levelsCount,
            [Values(true, false)]
            bool addValidatorOnRoot)
        {
            var levelNames = Enumerable.Range(0, levelsCount).Select(i => $"level{i}").ToArray();
            var validatorsMocks = CreatePrefixValidatorsNestedMock(levelNames);

            using (var stream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(stream))
            {
                if (addValidatorOnRoot)
                {
                    writer.PushElementNameValidator(validatorsMocks[0].Object);
                }

                writer.WriteStartDocument();

                if (!addValidatorOnRoot)
                {
                    writer.PushElementNameValidator(validatorsMocks[0].Object);
                }

                foreach (var levelName in levelNames)
                {
                    writer.WriteInt32($"{levelName}_int", 1);

                    writer.WriteStartDocument($"{levelName}_nested_empty");
                    writer.WriteEndDocument();

                    writer.WriteStartDocument($"{levelName}_nested");
                }

                // Add additional level
                writer.WriteStartDocument($"{levelsCount}_nested");
                writer.WriteEndDocument(); // $"{levelsCount}_nested"

                for (int i = 0; i < levelsCount; i++)
                {
                    writer.WriteEndDocument(); // $"{levelName}_nested"
                }

                // Pop validator
                if (!addValidatorOnRoot)
                {
                    writer.PopElementNameValidator();
                    writer.WriteInt32("nonvalidatedname", 1);
                }
                writer.WriteEndDocument();
            }

            for (int i = 0; i < levelsCount; i++)
            {
                var levelName = levelNames[i];
                var validatorMock = validatorsMocks[i];

                var children = new[] { $"{levelName}_nested_empty", $"{levelName}_nested" };
                var fieldsAll = children.Concat(new[] { $"{levelName}_int" }).ToArray();

                foreach (var field in fieldsAll)
                {
                    validatorMock.Verify(v => v.IsValidElementName(field), Times.Once);
                }

                foreach (var child in children)
                {
                    validatorMock.Verify(v => v.GetValidatorForChildContent(child), Times.Once);
                }

                validatorMock.Verify(v => v.IsValidElementName(It.IsNotIn(fieldsAll)), Times.Never);
                validatorMock.Verify(v => v.GetValidatorForChildContent(It.IsNotIn(children)), Times.Never);
            }
        }

        // private methods
        private Mock<IElementNameValidator>[] CreatePrefixValidatorsNestedMock(string[] prefixes)
        {
            var result = new Mock<IElementNameValidator>[prefixes.Length];
            IElementNameValidator childValidator = null;

            for (int i = prefixes.Length - 1; i >= 0; i--)
            {
                var currentValidator = CreatePrefixValidatorMock(prefixes[i], childValidator);

                result[i] = currentValidator;
                childValidator = currentValidator.Object;
            }

            return result;
        }

        private Mock<IElementNameValidator> CreatePrefixValidatorMock(string prefix, IElementNameValidator childValidator = null)
        {
            var mockValidator = new Mock<IElementNameValidator>();

            mockValidator
                .Setup(m => m.GetValidatorForChildContent(It.IsAny<string>()))
                .Returns(childValidator ?? NoOpElementNameValidator.Instance);

            mockValidator
                .Setup(m => m.IsValidElementName(It.IsAny<string>()))
                .Returns<string>((name) => name.StartsWith(prefix));

            return mockValidator;
        }
    }
}
