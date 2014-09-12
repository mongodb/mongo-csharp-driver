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

using FluentAssertions;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.AggregateToCollectionOperationTests
{
    [TestFixture]
    public class When_aggregating_to_a_collection : CollectionUsingSpecification
    {
        private AggregateToCollectionOperation _subject;

        protected override void Given()
        {
            Require.MinimumServerVersion("2.6.0");

            _subject = new AggregateToCollectionOperation(
                CollectionNamespace,
                new[] 
                { 
                    BsonDocument.Parse("{$match: {x: { $gt: 3}}}"),
                    BsonDocument.Parse("{$out: \"awesome\"}")
                },
                MessageEncoderSettings);

            Insert(new[] 
            {
                BsonDocument.Parse("{_id: 1, x: 1}"),
                BsonDocument.Parse("{_id: 2, x: 2}"),
                BsonDocument.Parse("{_id: 3, x: 3}"),
                BsonDocument.Parse("{_id: 4, x: 4}"),
                BsonDocument.Parse("{_id: 5, x: 5}"),
                BsonDocument.Parse("{_id: 6, x: 6}"),
            });
        }

        protected override void When()
        {
            ExecuteOperation(_subject);
        }

        protected override void DropCollection()
        {
            base.DropCollection();
            var operation = new DropCollectionOperation(
                new CollectionNamespace(CollectionNamespace.DatabaseNamespace, "awesome"),
                MessageEncoderSettings);

            ExecuteOperation(operation);
        }

        [Test]
        public void New_collection_should_contain_all_matching_documents()
        {
            var list = ReadAll(collectionNamespace: new CollectionNamespace(CollectionNamespace.DatabaseNamespace, "awesome"));

            list.Count.Should().Be(3);
        }

    }
}