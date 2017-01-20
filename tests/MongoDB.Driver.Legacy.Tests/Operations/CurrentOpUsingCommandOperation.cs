/* Copyright 2015-2016 MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Operations;
using Xunit;

namespace MongoDB.Driver.Tests.Operations
{
    public class CurrentOpUsingCommandOperationTests
    {
        // private fields
        private DatabaseNamespace _adminDatabaseNamespace;
        private MessageEncoderSettings _messageEncoderSettings;

        // public methods
        public CurrentOpUsingCommandOperationTests()
        {
            _adminDatabaseNamespace = new DatabaseNamespace("admin");
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new CurrentOpUsingCommandOperation(_adminDatabaseNamespace, _messageEncoderSettings);

            result.DatabaseNamespace.Should().Be(_adminDatabaseNamespace);
            result.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Fact]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            Action action = () => new CurrentOpUsingCommandOperation(null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("databaseNamespace");
        }

        [Fact]
        public void CreateOperation_should_return_expected_result()
        {
            var subject = new CurrentOpUsingCommandOperation(_adminDatabaseNamespace, _messageEncoderSettings);

            var result = subject.CreateOperation();

            result.Command.Should().Be("{ currentOp : 1 }");
            result.DatabaseNamespace.Should().BeSameAs(_adminDatabaseNamespace);
            result.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
            result.ResultSerializer.Should().BeSameAs(BsonDocumentSerializer.Instance);
        }

        [SkippableFact]
        public void Execute_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.CurrentOpCommand);
            var subject = new CurrentOpUsingCommandOperation(_adminDatabaseNamespace, _messageEncoderSettings);
            using (var binding = new ReadPreferenceBinding(CoreTestConfiguration.Cluster, ReadPreference.PrimaryPreferred))
            {
                var result = subject.Execute(binding, CancellationToken.None);

                result.Contains("inprog");
            }
        }
    }
}
