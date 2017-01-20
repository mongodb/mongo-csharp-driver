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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class BuildInfoResultTests
    {
        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_wrapped_is_null()
        {
            Action act = () => new BuildInfoResult(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Equals_should_be_true_when_both_have_the_same_result()
        {
            var subject1 = new BuildInfoResult(new BsonDocument("x", 1));
            var subject2 = new BuildInfoResult(new BsonDocument("x", 1));

            subject1.Equals(subject2).Should().BeTrue();
        }

        [Fact]
        public void Equals_should_be_false_when_both_have_different_results()
        {
            var subject1 = new BuildInfoResult(new BsonDocument("x", 1));
            var subject2 = new BuildInfoResult(new BsonDocument("x", 2));

            subject1.Equals(subject2).Should().BeFalse();
        }

        [Fact]
        public void ServerVersion_should_get_the_semantic_version()
        {
            var doc = new BsonDocument
            {
                { "version", "2.6.3" }
            };
            var subject = new BuildInfoResult(doc);

            subject.ServerVersion.Should().Be(new SemanticVersion(2, 6, 3));
        }

        [Fact]
        public void Wrapped_should_return_the_document_passed_in_the_constructor()
        {
            var doc = new BsonDocument();
            var subject = new BuildInfoResult(doc);

            subject.Wrapped.Should().BeSameAs(doc);
        }
    }
}