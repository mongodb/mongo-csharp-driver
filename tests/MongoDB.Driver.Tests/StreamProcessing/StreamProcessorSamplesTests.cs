/* Copyright 2026-present MongoDB Inc.
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
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.StreamProcessing;
using Xunit;

namespace MongoDB.Driver.Tests.StreamProcessing
{
    public class StreamProcessorSamplesTests
    {
        private static StreamProcessorSamples CreateFrom(long cursorId, IReadOnlyList<BsonDocument> docs)
        {
            var ctor = typeof(StreamProcessorSamples).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(long), typeof(IReadOnlyList<BsonDocument>) },
                null);
            ctor.Should().NotBeNull();
            return (StreamProcessorSamples)ctor.Invoke(new object[] { cursorId, docs });
        }

        [Fact]
        public void IsExhausted_returns_true_when_cursor_id_is_zero()
        {
            CreateFrom(0, new List<BsonDocument>()).IsExhausted.Should().BeTrue();
        }

        [Fact]
        public void IsExhausted_returns_false_when_cursor_id_is_non_zero()
        {
            CreateFrom(42, new List<BsonDocument>()).IsExhausted.Should().BeFalse();
        }

        [Fact]
        public void Documents_returns_the_documents_passed_in()
        {
            var docs = new List<BsonDocument>
            {
                new BsonDocument("x", 1),
                new BsonDocument("x", 2)
            };
            var samples = CreateFrom(7, docs);
            samples.Documents.Should().BeEquivalentTo(docs);
        }
    }
}
