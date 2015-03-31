﻿/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class EvalOperationTests
    {
        private DatabaseNamespace _databaseNamespace;
        private MessageEncoderSettings _messageEncoderSettings;

        [SetUp]
        public void Setup()
        {
            _databaseNamespace = CoreTestConfiguration.DatabaseNamespace;
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
        }

        [Test]
        public void Args_should_work()
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);
            var args = new BsonValue[] { 1, 2, 3 };

            subject.Args = args;

            subject.Args.Should().Equal(args);
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var function = new BsonJavaScript("return 1");

            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);

            subject.Args.Should().BeNull();
            subject.DatabaseNamespace.Should().Be(_databaseNamespace);
            subject.Function.Should().Be(function);
            subject.MaxTime.Should().NotHaveValue();
            // subject.MessageEncoderSettings.Should().Be(_messageEncoderSettings);
            Assert.That(subject.MessageEncoderSettings, Is.EqualTo(_messageEncoderSettings));
            subject.NoLock.Should().NotHaveValue();
        }

        [Test]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            var function = new BsonJavaScript("return 1");

            Action action = () => new EvalOperation(null, function, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void constructor_should_throw_when_function_is_null()
        {
            Action action = () => new EvalOperation(_databaseNamespace, null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void CreateCommand_should_return_expected_result()
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "$eval", function }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_args_are_provided()
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);
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

        [Test]
        public void CreateCommand_should_return_expected_result_when_maxTime_is_provided()
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);
            subject.MaxTime = TimeSpan.FromSeconds(1);
            var expectedResult = new BsonDocument
            {
                { "$eval", function },
                { "maxTimeMS", 1000.0 }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_noLock_is_provided()
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);
            subject.NoLock = true;
            var expectedResult = new BsonDocument
            {
                { "$eval", function },
                { "nolock", true }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer(Authentication = AuthenticationRequirement.Off)]
        public async Task ExecuteAsync_should_return_expected_result()
        {
            var function = "return 1";
            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);

            BsonValue result;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                result = await subject.ExecuteAsync(binding, CancellationToken.None);
            }

            result.Should().Be(1);
        }

        [Test]
        [RequiresServer(Authentication = AuthenticationRequirement.Off)]
        public async Task ExecuteAsync_should_return_expected_result_when_args_are_provided()
        {
            var function = "function(x) { return x; }";
            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);
            subject.Args = new BsonValue[] { 1 };

            BsonValue result;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                result = await subject.ExecuteAsync(binding, CancellationToken.None);
            }

            result.Should().Be(1);
        }

        [Test]
        public void ExecuteAsync_should_return_expected_result_when_maxTime_is_provided()
        {
            if (CoreTestConfiguration.ServerVersion >= new SemanticVersion(2, 6, 0))
            {
                // TODO: implement EvalOperation MaxTime test
            }
        }

        [Test]
        [RequiresServer(Authentication = AuthenticationRequirement.Off)]
        public async Task ExecuteAsync_should_return_expected_result_when_noLock_is_provided()
        {
            var function = "return 1";
            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);
            subject.NoLock = true;

            BsonValue result;
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                result = await subject.ExecuteAsync(binding, CancellationToken.None);
            }

            result.Should().Be(1);
        }

        [Test]
        public void ExecuteAsync_should_throw_when_binding_isNull()
        {
            var function = "return 1";
            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);

            Action action = () => subject.ExecuteAsync(null, CancellationToken.None).GetAwaiter().GetResult();

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void MaxTime_should_work(
            [Values(null, 0, 1)]
            int? value)
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);
            var maxTime = value.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(value.Value) : null;

            subject.MaxTime = maxTime;

            subject.MaxTime.Should().Be(maxTime);
        }

        [Test]
        public void NoLock_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var function = new BsonJavaScript("return 1");
            var subject = new EvalOperation(_databaseNamespace, function, _messageEncoderSettings);

            subject.NoLock = value;

            subject.NoLock.Should().Be(value);
        }
    }
}
