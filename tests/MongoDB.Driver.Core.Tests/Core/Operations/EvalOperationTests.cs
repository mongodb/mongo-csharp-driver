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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class EvalOperationTests
    {
        private DatabaseNamespace _adminDatabaseNamespace;
        private MessageEncoderSettings _messageEncoderSettings;

        public EvalOperationTests()
        {
            _adminDatabaseNamespace = DatabaseNamespace.Admin;
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
        }

        [Fact]
        public void Args_should_work()
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);
            var args = new BsonValue[] { 1, 2, 3 };

            subject.Args = args;

            subject.Args.Should().Equal(args);
        }

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var function = new BsonJavaScript("return 1");

            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);

            subject.Args.Should().BeNull();
            subject.DatabaseNamespace.Should().Be(_adminDatabaseNamespace);
            subject.Function.Should().Be(function);
            subject.MaxTime.Should().NotHaveValue();
            // subject.MessageEncoderSettings.Should().Be(_messageEncoderSettings);
            Assert.Equal(_messageEncoderSettings, subject.MessageEncoderSettings);
            subject.NoLock.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            var function = new BsonJavaScript("return 1");

            Action action = () => new EvalOperation(null, function, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void constructor_should_throw_when_function_is_null()
        {
            Action action = () => new EvalOperation(_adminDatabaseNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateCommand_should_return_expected_result()
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "$eval", function }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result_when_args_are_provided()
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);
            var args = new BsonValue[] { 1, 2, 3 };
            subject.Args = args;
            var expectedResult = new BsonDocument
            {
                { "$eval", function },
                { "args", new BsonArray(args) }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result_when_maxTime_is_provided()
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);
            subject.MaxTime = TimeSpan.FromSeconds(1);
            var expectedResult = new BsonDocument
            {
                { "$eval", function },
                { "maxTimeMS", 1000.0 }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result_when_noLock_is_provided()
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);
            subject.NoLock = true;
            var expectedResult = new BsonDocument
            {
                { "$eval", function },
                { "nolock", true }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Authentication(false);
            var function = "return 1";
            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);

            var result = ExecuteOperation(subject, async);

            result.Should().Be(1.0);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_args_are_provided(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Authentication(false);
            var function = "function(x) { return x; }";
            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);
            subject.Args = new BsonValue[] { 1 };

            var result = ExecuteOperation(subject, async);

            result.Should().Be(1.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_maxTime_is_provided(
            [Values(false, true)]
            bool async)
        {
            if (Feature.MaxTime.IsSupported(CoreTestConfiguration.ServerVersion))
            {
                // TODO: implement EvalOperation MaxTime test
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_expected_result_when_noLock_is_provided(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check().Authentication(false);
            var function = "return 1";
            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);
            subject.NoLock = true;

            var result = ExecuteOperation(subject, async);

            result.Should().Be(1.0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_throw_when_binding_isNull(
            [Values(false, true)]
            bool async)
        {
            var function = "return 1";
            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);

            Action action = () => ExecuteOperation(subject, null, async);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_should_work(
            [Values(null, 0, 1)]
            int? value)
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);
            var maxTime = value.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(value.Value) : null;

            subject.MaxTime = maxTime;

            subject.MaxTime.Should().Be(maxTime);
        }

        [Theory]
        [ParameterAttributeData]
        public void NoLock_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_adminDatabaseNamespace, function, _messageEncoderSettings);

            subject.NoLock = value;

            subject.NoLock.Should().Be(value);
        }

        // private methods
        private BsonValue ExecuteOperation(EvalOperation operation, bool async)
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                return ExecuteOperation(operation, binding, async);
            }
        }

        private BsonValue ExecuteOperation(EvalOperation operation, IWriteBinding binding, bool async)
        {
            if (async)
            {
                return operation.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                return operation.Execute(binding, CancellationToken.None);
            }
        }
    }
}
