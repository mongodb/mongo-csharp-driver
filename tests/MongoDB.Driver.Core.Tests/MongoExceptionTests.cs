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
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if NET452
using System.Runtime.Serialization.Formatters.Binary;
#endif
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver
{
    public class MongoExceptionTests
    {
        private Exception _innerException = new Exception("inner");
        private string _message = "message";

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new MongoException(_message);

            subject.Message.Should().BeSameAs(_message);
            subject.InnerException.Should().BeNull();
            subject.ErrorLabels.Should().HaveCount(0);
        }

        [Fact]
        public void constructor_with_innerException_should_initialize_subject()
        {
            var subject = new MongoException(_message, _innerException);

            subject.Message.Should().BeSameAs(_message);
            subject.InnerException.Should().BeSameAs(_innerException);
            subject.ErrorLabels.Should().HaveCount(0);
        }

        [Theory]
        [InlineData(new object[] { new string[0] })]
        [InlineData(new object[] { new[] { "one" } })]
        [InlineData(new object[] { new[] { "one", "two" } })]
        [InlineData(new object[] { new[] { "one", "two", "three" } })]
        public void ErrorLabels_should_return_expected_result(string[] errorLabels)
        {
            var subject = new MongoException(_message);
            foreach (var errorLabel in errorLabels)
            {
                subject.AddErrorLabel(errorLabel);
            }

            var result = subject.ErrorLabels;

            result.Should().Equal(errorLabels);
        }

        [Theory]
        [ParameterAttributeData]
        public void AddErrorLabels_should_have_expected_result(
            [Values(0, 1, 2, 3)] int existingCount)
        {
            var subject = new MongoException(_message);
            for (var i = 0; i < existingCount; i++)
            {
                var errorLabel = $"label{i}";
                subject.AddErrorLabel(errorLabel);
            }
            var existingErrorLabels = new List<string>(subject.ErrorLabels);
            var newErrorLabel = "x";

            subject.AddErrorLabel(newErrorLabel);

            subject.ErrorLabels.Should().Equal(existingErrorLabels.Concat(new[] { newErrorLabel }));
        }

        [Fact]
        public void AddErrorLabels_should_not_add_duplicates()
        {
            var subject = new MongoException(_message);

            subject.AddErrorLabel("one");
            subject.AddErrorLabel("one");

            subject.ErrorLabels.Should().HaveCount(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void HasErrorLabel_should_have_expected_result(
            [Values(0, 1, 2, 3)] int existingCount)
        {
            var subject = new MongoException(_message);
            for (var i = 0; i < existingCount; i++)
            {
                var errorLabel = $"label{i}";
                subject.AddErrorLabel(errorLabel);
            }

            foreach (var errorLabel in subject.ErrorLabels)
            {
                subject.HasErrorLabel(errorLabel).Should().BeTrue();
            }
            subject.HasErrorLabel("x").Should().BeFalse();
        }

        [Theory]
        [InlineData(new string[0], "x")]
        [InlineData(new[] { "one" }, "one")]
        [InlineData(new[] { "one" }, "x")]
        [InlineData(new[] { "one", "two" }, "one")]
        [InlineData(new[] { "one", "two" }, "two")]
        [InlineData(new[] { "one", "two" }, "x")]
        [InlineData(new[] { "one", "two", "three" }, "one")]
        [InlineData(new[] { "one", "two", "three" }, "two")]
        [InlineData(new[] { "one", "two", "three" }, "three")]
        [InlineData(new[] { "one", "two", "three" }, "x")]
        public void RemoveErrorLabels_should_have_expected_result(string[] errorLabels, string removeErrorLabel)
        {
            var subject = new MongoException(_message);
            foreach (var errorLabel in errorLabels)
            {
                subject.AddErrorLabel(errorLabel);
            }

            subject.RemoveErrorLabel(removeErrorLabel);

            subject.ErrorLabels.Should().Equal(errorLabels.Where(x => x != removeErrorLabel));
        }

#if NET452
        [Fact]
        public void Serialization_should_work()
        {
            var subject = new MongoException(_message, _innerException);
            subject.AddErrorLabel("one");

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoException)formatter.Deserialize(stream);

                rehydrated.Message.Should().Be(subject.Message);
                rehydrated.InnerException.Message.Should().Be(subject.InnerException.Message); // Exception does not override Equals
                rehydrated.ErrorLabels.Should().Equal(subject.ErrorLabels);
            }
        }
#endif
    }
}
