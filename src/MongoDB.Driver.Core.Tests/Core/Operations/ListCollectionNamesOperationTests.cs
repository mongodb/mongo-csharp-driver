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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.SyncExtensionMethods;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class ListCollectionNamesOperationTests : OperationTestBase
    {
        [Test]
        public void Constructor_should_throw_when_database_namespace_is_null()
        {
            Action act = () => new ListCollectionNamesOperation(null, _messageEncoderSettings);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_message_encoder_settings_is_null()
        {
            Action act = () => new ListCollectionNamesOperation(_databaseNamespace, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_initialize_subject()
        {
            var subject = new ListCollectionNamesOperation(_databaseNamespace, _messageEncoderSettings);

            subject.DatabaseNamespace.DatabaseName.Should().Be(_databaseNamespace.DatabaseName);
            subject.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Test]
        [RequiresServer("EnsureCollectionsExists")]
        public async Task ExecuteAsync_should_return_the_correct_collections_when_the_database_exists()
        {
            var subject = new ListCollectionNamesOperation(_databaseNamespace, _messageEncoderSettings);

            var result = await ExecuteOperation(subject);

            result.Count.Should().BeGreaterThan(0);
            result.Should().Contain(_collectionNamespace.CollectionName);
        }

        [Test]
        [RequiresServer("EnsureCollectionsExists")]
        public async Task ExecuteAsync_should_return_an_empty_result_when_the_collection_does_not_exist()
        {
            var databaseNamespace = new DatabaseNamespace("asdlkjaflkgoiuewkljasdg");
            var subject = new ListCollectionNamesOperation(databaseNamespace, _messageEncoderSettings);

            var result = await ExecuteOperation(subject);

            result.Should().HaveCount(0);
        }

        // helper methods
        private void EnsureCollectionsExists()
        {
            Insert(BsonDocument.Parse("{x:1}"));
        }
    }
}
