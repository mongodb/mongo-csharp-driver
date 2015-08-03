/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Jira
{
    [TestFixture]
    public class CSharp779
    {
        public class RepresentationExample
        {
            [BsonRepresentation(BsonType.String)]
            public Guid[] MultipleEntry { get; set; }

            [BsonRepresentation(BsonType.String)]
            public Guid SingleEntry { get; set; }
        }

        [Test]
        public void InRepresentationIssue()
        {
            var guid = Guid.NewGuid();

            var singleUpdate = Query<RepresentationExample>
                .In(x => x.SingleEntry, new[] { guid });

            Assert.That(singleUpdate.ToJson().Contains(guid.ToString()));
            Assert.That(!singleUpdate.ToJson().Contains("CSUUID"));

            var multipleUpdate = Query<RepresentationExample>
                .In(x => x.MultipleEntry, new[] { guid });

            Assert.That(multipleUpdate.ToJson(), Contains.Substring(guid.ToString()));
            Assert.That(!multipleUpdate.ToJson().Contains("CSUUID"));
        }

        [Test]
        public void UpdateRepresentationIssue()
        {
            var guid = Guid.NewGuid();

            var singleUpdate = Update<RepresentationExample>
                .Set(x => x.SingleEntry, guid);

            Assert.That(singleUpdate.ToJson().Contains(guid.ToString()));
            Assert.That(!singleUpdate.ToJson().Contains("CSUUID"));

            var multipleUpdate = Update<RepresentationExample>
                .PullAll(x => x.MultipleEntry, new[] { guid });

            Assert.That(multipleUpdate.ToJson(), Contains.Substring(guid.ToString()));
            Assert.That(!multipleUpdate.ToJson().Contains("CSUUID"));
        }
    }
}