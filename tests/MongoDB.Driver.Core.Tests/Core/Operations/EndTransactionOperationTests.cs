﻿/* Copyright 2018-present MongoDB Inc.
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
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class EndTransactionOperationTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var writeConcern = new WriteConcern();
            var recoveryToken = new BsonDocument("section", 31);

            var result = new FakeEndTransactionOperation(recoveryToken, writeConcern);

            result._recoveryToken().Should().Be(recoveryToken);
            result.MessageEncoderSettings.Should().BeNull();
            result.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Fact]
        public void constructor_should_throw_when_writeConcern_is_null()
        {
            var exception = Record.Exception(
                () => new FakeEndTransactionOperation(recoveryToken: new BsonDocument(), writeConcern: null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("writeConcern");
        }

        [Theory]
        [ParameterAttributeData]
        public void MessageEncoderSettings_get_should_return_expected_result(
            [Values(false, true)] bool nullValue)
        {
            var value = nullValue ? null : new MessageEncoderSettings();
            var subject = CreateSubject(messageEncoderSettings: value);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void MessageEncoderSettings_set_should_return_expected_result(
            [Values(false, true)] bool nullValue)
        {
            var subject = CreateSubject();
            var value = nullValue ? null : new MessageEncoderSettings();

            subject.MessageEncoderSettings = value;

            subject.MessageEncoderSettings.Should().BeSameAs(value);
        }

        [Fact]
        public void WriteConcern_should_return_expected_result()
        {
            var value = new WriteConcern();
            var subject = CreateSubject(writeConcern: value);

            var result = subject.WriteConcern;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void CommandName_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.CommandName();

            result.Should().BeSameAs("fakeOperation");
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateCommand_should_return_expected_result(
            [Values(true, false)] bool useRecoveryToken,
            [Values(1, 2)] int w)
        {
            var writeConcern = new WriteConcern(w);
            var recoveryToken = useRecoveryToken ? new BsonDocument("cake", "false") : null;
            var subject = CreateSubject(recoveryToken, writeConcern);

            var result = subject.CreateCommand();

            var expectedResult = new BsonDocument {
                { "fakeOperation", 1 },
                { "writeConcern", new BsonDocument { {"w", w} } }
            };
            if (useRecoveryToken)
            {
                expectedResult.Add("recoveryToken", recoveryToken);
            }

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateCommand_should_return_expected_result_when_write_concern_is_server_default()
        {
            var writeConcern = new WriteConcern();
            var subject = CreateSubject(writeConcern: writeConcern);

            var result = subject.CreateCommand();

            result.Should().Be("{ fakeOperation : 1 }");
        }

        // private methods
        private EndTransactionOperation CreateSubject(
            BsonDocument recoveryToken = null,
            WriteConcern writeConcern = null,
            MessageEncoderSettings messageEncoderSettings = null)
        {
            writeConcern = writeConcern ?? new WriteConcern();
            return new FakeEndTransactionOperation(recoveryToken, writeConcern)
            {
                MessageEncoderSettings = messageEncoderSettings
            };
        }

        // nested types
        private class FakeEndTransactionOperation : EndTransactionOperation
        {
            // public constructors
            public FakeEndTransactionOperation(BsonDocument recoveryToken, WriteConcern writeConcern)
                : base(recoveryToken, writeConcern)
            {
            }

            // public properties
            protected override string CommandName => "fakeOperation";
        }
    }

    public class AbortTransactionOperationTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var writeConcern = new WriteConcern();
            var recoveryToken = new BsonDocument("generalOrder", 1);

            var result = new AbortTransactionOperation(recoveryToken, writeConcern);

            result._recoveryToken().Should().BeSameAs(recoveryToken);
            result.CommandName().Should().Be("abortTransaction");
            result.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Fact]
        public void CommandName_should_return_expected_result()
        {
            var writeConcern = new WriteConcern();
            var subject = new AbortTransactionOperation(writeConcern);

            var result = subject.CommandName();

            result.Should().Be("abortTransaction");
        }
    }

    public class CommitTransactionOperationTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var writeConcern = new WriteConcern();
            var recoveryToken = new BsonDocument("generalOrder", 1);
            var result = new CommitTransactionOperation(recoveryToken, writeConcern);

            result._recoveryToken().Should().Be(recoveryToken);
            result.CommandName().Should().Be("commitTransaction");
            result.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Fact]
        public void CommandName_should_return_expected_result()
        {
            var writeConcern = new WriteConcern();
            var subject = new CommitTransactionOperation(writeConcern);

            var result = subject.CommandName();

            result.Should().Be("commitTransaction");
        }
    }

    public static class EndTransactionOperationReflector
    {
        // fields
        public static BsonDocument _recoveryToken(this EndTransactionOperation obj) => (BsonDocument)Reflector.GetFieldValue(obj, nameof(_recoveryToken));

        // properties
        public static string CommandName(this EndTransactionOperation obj) => (string)Reflector.GetPropertyValue(obj, nameof(CommandName));

        // methods
        public static BsonDocument CreateCommand(this EndTransactionOperation obj)
        {
            var methodInfo = typeof(EndTransactionOperation).GetMethod("CreateCommand", BindingFlags.NonPublic | BindingFlags.Instance);
            return (BsonDocument)methodInfo.Invoke(obj, null);
        }
    }
}
