/* Copyright 2013-2014 MongoDB Inc.
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
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Connections
{
    [TestFixture]
    public class BuildInfoResultTests
    {
        [Test]
        public void Constructor_should_throw_an_ArgumentNullException_when_wrapped_is_null()
        {
            Action act = () => new BuildInfoResult(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void ServerVersion_should_get_the_semantic_version()
        {
            var doc = new BsonDocument
            {
                { "version", "2.6.3" }
            };
            var subject = new BuildInfoResult(doc);

            subject.ServerVersion.Should().Be(new SemanticVersion(2, 6, 3));
        }

        [Test]
        public void Wrapped_should_return_the_document_passed_in_the_constructor()
        {
            var doc = new BsonDocument();
            var subject = new BuildInfoResult(doc);

            subject.Wrapped.Should().BeSameAs(doc);
        }
    }
}