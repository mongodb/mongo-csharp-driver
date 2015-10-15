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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Operations;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Operations
{
    [TestFixture]
    public class CurrentOpOperationTests
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
            var result = new CurrentOpOperation(_adminDatabaseNamespace, _messageEncoderSettings);

            result.DatabaseNamespace.Should().Be(_adminDatabaseNamespace);
            result.MessageEncoderSettings.Should().BeEquivalentTo(_messageEncoderSettings);
        }

        [Test]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            Action action = () => new CurrentOpOperation(null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("databaseNamespace");
        }

        [Test]
        public void CreateOperation_should_return_expected_result_when_server_version_does_not_support_command()
        {
            var subject = new CurrentOpOperation(_adminDatabaseNamespace, _messageEncoderSettings);

            var result = subject.CreateOperation(new SemanticVersion(3, 1, 1));

            result.Should().BeOfType<CurrentOpUsingFindOperation>();
            result.As<CurrentOpUsingFindOperation>().DatabaseNamespace.Should().Be(_adminDatabaseNamespace);
            result.As<CurrentOpUsingFindOperation>().MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Test]
        public void CreateOperation_should_return_expected_result_when_server_version_does_support_command()
        {
            var subject = new CurrentOpOperation(_adminDatabaseNamespace, _messageEncoderSettings);

            var result = subject.CreateOperation(new SemanticVersion(3, 1, 2));

            result.Should().BeOfType<CurrentOpUsingCommandOperation>();
            result.As<CurrentOpUsingCommandOperation>().DatabaseNamespace.Should().Be(_adminDatabaseNamespace);
            result.As<CurrentOpUsingCommandOperation>().MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.2")]
        public void Execute_should_return_expected_result()
        {
            var subject = new CurrentOpOperation(_adminDatabaseNamespace, _messageEncoderSettings);
            using (var binding = new ReadPreferenceBinding(CoreTestConfiguration.Cluster, ReadPreference.PrimaryPreferred))
            {
                var result = subject.Execute(binding);

                result.Contains("inprog");
            }
        }
    }
}
