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

using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class TrieNameDecoderTests
    {
        [Fact]
        public void Should_read_name_when_trie_does_not_know_about_the_name()
        {
            var trie = new BsonTrie<int>();
            trie.Add("known", 10);

            Assert(trie, "different", false, default);
        }

        [Fact]
        public void Should_read_name_when_trie_holds_a_longer_version_of_the_name()
        {
            var trie = new BsonTrie<int>();
            trie.Add("longer", 10);

            Assert(trie, "long", false, default);
        }

        [Fact]
        public void Should_read_name_when_trie_knows_about_the_name()
        {
            var trie = new BsonTrie<int>();
            trie.Add("known", 10);

            Assert(trie, "known", true, 10);
        }

        // private methods
        private void Assert(string name, TrieNameDecoder<int> subject, bool found, int value)
        {
            using (var memoryStream = new MemoryStream())
            using (var bsonStream = new BsonStreamAdapter(memoryStream))
            {
                bsonStream.WriteCString(name);
                bsonStream.WriteInt32(20);
                bsonStream.Position = 0;

                var result = subject.Decode(bsonStream, Utf8Encodings.Strict);

                result.Should().Be(name);
                subject.Found.Should().Be(found);
                subject.Value.Should().Be(value);
            }
        }

        private void Assert(BsonTrie<int> trie, string name, bool found, int value)
        {
            // test multiple calls to the same TrieDecoder instance
            var subject = new TrieNameDecoder<int>(trie);

            // 1st pass: assert calling test's expectation
            Assert(name, subject, found, value);

            // 2rd pass: assert calling test's expectation once again
            Assert(name, subject, found, value);

            // 3rd pass: different value in stream --> assert "found: false" and "value: default"
            Assert("doesn't exist in trie", subject, false, default);

            // 4th pass: assert calling test's expectation once again
            Assert(name, subject, found, value);
        }
    }
}
