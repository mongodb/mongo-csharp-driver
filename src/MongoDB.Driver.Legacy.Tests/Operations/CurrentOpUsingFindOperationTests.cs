/* Copyright 2015 MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Operations;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Operations
{
    [TestFixture]
    public class CurrentOpUsingFindOperationTests
    {
        // private fields
        private DatabaseNamespace _adminDatabaseNamespace;
        private MessageEncoderSettings _messageEncoderSettings;

        // public methods
        [TestFixtureSetUp]
        public virtual void TestFixtureSetUp()
        {
            _adminDatabaseNamespace = new DatabaseNamespace("admin");
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
        }

        [Test]
        public void constructor_should_initialize_instance()
        {
            var result = new CurrentOpUsingFindOperation(_adminDatabaseNamespace, _messageEncoderSettings);

            result.DatabaseNamespace.Should().Be(_adminDatabaseNamespace);
            result.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Test]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            Action action = () => new CurrentOpUsingFindOperation(null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("databaseNamespace");
        }

        [Test]
        public void CreateOperation_should_return_expected_result()
        {
            var subject = new CurrentOpUsingFindOperation(_adminDatabaseNamespace, _messageEncoderSettings);

            var result = subject.CreateOperation();

            result.CollectionNamespace.Should().Be(new CollectionNamespace(_adminDatabaseNamespace, "$cmd.sys.inprog"));
            result.Limit.Should().Be(1);
            result.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            result.ResultSerializer.Should().BeSameAs(BsonDocumentSerializer.Instance);
        }

        [Test]
        [RequiresServer(ClusterTypes = ClusterTypes.StandaloneOrReplicaSet, StorageEngines = "mmapv1")]
        public void Execute_should_return_expected_result()
        {
            var subject = new CurrentOpUsingFindOperation(_adminDatabaseNamespace, _messageEncoderSettings);
            using (var binding = new ReadPreferenceBinding(CoreTestConfiguration.Cluster, ReadPreference.PrimaryPreferred))
            {
                var result = subject.Execute(binding, CancellationToken.None);

                result.Contains("inprog");
            }
        }
    }
}
