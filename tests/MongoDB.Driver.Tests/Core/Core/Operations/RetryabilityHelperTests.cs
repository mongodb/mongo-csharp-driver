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
using System.IO;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using Xunit;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Operations
{
    public class RetryabilityHelperTests
    {
        [Theory]
        [InlineData(1, false)]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, true)]
        [InlineData(ServerErrorCode.InterruptedDueToReplStateChange, true)]
        [InlineData(ServerErrorCode.LegacyNotPrimary, true)]
        [InlineData(ServerErrorCode.NotWritablePrimary, true)]
        [InlineData(ServerErrorCode.NotPrimaryNoSecondaryOk, true)]
        [InlineData(ServerErrorCode.NotPrimaryOrSecondary, true)]
        [InlineData(ServerErrorCode.PrimarySteppedDown, true)]
        [InlineData(ServerErrorCode.ShutdownInProgress, true)]
        [InlineData(ServerErrorCode.HostNotFound, true)]
        [InlineData(ServerErrorCode.HostUnreachable, true)]
        [InlineData(ServerErrorCode.NetworkTimeout, true)]
        [InlineData(ServerErrorCode.SocketException, true)]
        [InlineData(ServerErrorCode.WriteConcernFailed, false)]
        [InlineData(ServerErrorCode.ExceededTimeLimit, true)]
        public void AddRetryableWriteErrorLabelIfRequired_should_add_RetryableWriteError_for_MongoWriteConcernException_when_required(int errorCode, bool shouldAddErrorLabel)
        {
            var exception = CoreExceptionHelper.CreateMongoWriteConcernException(BsonDocument.Parse($"{{ writeConcernError : {{ code : {errorCode} }} }}"));
            var maxWireVersion = Feature.ServerReturnsRetryableWriteErrorLabel.LastNotSupportedWireVersion;
            var connectionDescription = OperationTestHelper.CreateConnectionDescription(maxWireVersion);

            RetryabilityHelper.AddRetryableWriteErrorLabelIfRequired(exception, connectionDescription);

            var hasRetryableWriteErrorLabel = exception.HasErrorLabel("RetryableWriteError");
            hasRetryableWriteErrorLabel.Should().Be(shouldAddErrorLabel);
        }

        [Theory]
        [ParameterAttributeData]
        public void AddRetryableWriteErrorLabelIfRequired_should_add_RetryableWriteError_for_network_errors([Values(false, true)] bool serverReturnsRetryableWriteErrorLabel)
        {
            var exception = (MongoException)CoreExceptionHelper.CreateException(typeof(MongoConnectionException));
            var feature = Feature.ServerReturnsRetryableWriteErrorLabel;
            var wireVersion = serverReturnsRetryableWriteErrorLabel ? feature.FirstSupportedWireVersion : feature.LastNotSupportedWireVersion;
            var connectionDescription = OperationTestHelper.CreateConnectionDescription(wireVersion);

            RetryabilityHelper.AddRetryableWriteErrorLabelIfRequired(exception, connectionDescription);

            var hasRetryableWriteErrorLabel = exception.HasErrorLabel("RetryableWriteError");
            hasRetryableWriteErrorLabel.Should().BeTrue();
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(typeof(MongoCursorNotFoundException), false)]
        [InlineData(typeof(MongoConnectionException), true)]
        [InlineData(typeof(MongoNodeIsRecoveringException), true)]
        [InlineData(typeof(MongoNotPrimaryException), true)]
        [InlineData(ServerErrorCode.HostNotFound, true)]
        [InlineData(ServerErrorCode.HostUnreachable, true)]
        [InlineData(ServerErrorCode.NetworkTimeout, true)]
        [InlineData(ServerErrorCode.SocketException, true)]
        [InlineData(ServerErrorCode.WriteConcernFailed, false)]
        [InlineData(ServerErrorCode.ExceededTimeLimit, true)]
        public void AddRetryableWriteErrorLabelIfRequired_should_add_RetryableWriteError_when_required(object exceptionDescription, bool shouldAddErrorLabel)
        {
            MongoException exception;
            if (exceptionDescription is Type exceptionType)
            {
                exception = (MongoException)CoreExceptionHelper.CreateException(exceptionType);
            }
            else
            {
                exception = CoreExceptionHelper.CreateMongoCommandException((int)exceptionDescription);
            }
            var maxWireVersion = Feature.ServerReturnsRetryableWriteErrorLabel.LastNotSupportedWireVersion;
            var connectionDescription = OperationTestHelper.CreateConnectionDescription(maxWireVersion);

            RetryabilityHelper.AddRetryableWriteErrorLabelIfRequired(exception, connectionDescription);

            var hasRetryableWriteErrorLabel = exception.HasErrorLabel("RetryableWriteError");
            hasRetryableWriteErrorLabel.Should().Be(shouldAddErrorLabel);
        }

        [Theory]
        [InlineData("{ txnNumber : 1 }", true)]
        [InlineData("{ commitTransaction : 1 }", true)]
        [InlineData("{ abortTransaction : 1 }", true)]
        [InlineData("{ ping : 1 }", false)]
        public void IsCommandRetryable_should_return_expected_result(string command, bool isRetryable)
        {
            var commandDocument = BsonDocument.Parse(command);

            var result = RetryabilityHelper.IsCommandRetryable(commandDocument);

            result.Should().Be(isRetryable);
        }

        [Theory]
        [InlineData(typeof(MongoConnectionException), true)]
        [InlineData(typeof(MongoConnectionClosedException), false)]
        [InlineData(typeof(MongoNodeIsRecoveringException), true)]
        [InlineData(typeof(MongoNotPrimaryException), true)]
        [InlineData(typeof(MongoCursorNotFoundException), true)]
        [InlineData(typeof(MongoConnectionPoolPausedException), true)]
        [InlineData(ServerErrorCode.HostNotFound, true)]
        [InlineData(ServerErrorCode.HostUnreachable, true)]
        [InlineData(ServerErrorCode.NetworkTimeout, true)]
        [InlineData(ServerErrorCode.ShutdownInProgress, true)]
        [InlineData(ServerErrorCode.PrimarySteppedDown, true)]
        [InlineData(ServerErrorCode.ExceededTimeLimit, true)]
        [InlineData(ServerErrorCode.SocketException, true)]
        [InlineData(ServerErrorCode.LegacyNotPrimary, true)]
        [InlineData(ServerErrorCode.NotWritablePrimary, true)]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, true)]
        [InlineData(ServerErrorCode.InterruptedDueToReplStateChange, true)]
        [InlineData(ServerErrorCode.NotPrimaryNoSecondaryOk, true)]
        [InlineData(ServerErrorCode.NotPrimaryOrSecondary, true)]
        [InlineData(ServerErrorCode.StaleShardVersion, true)]
        [InlineData(ServerErrorCode.StaleEpoch, true)]
        [InlineData(ServerErrorCode.StaleConfig, true)]
        [InlineData(ServerErrorCode.RetryChangeStream, true)]
        [InlineData(ServerErrorCode.FailedToSatisfyReadPreference, true)]
        [InlineData(ServerErrorCode.ElectionInProgress, false)]
        [InlineData(ServerErrorCode.WriteConcernFailed, false)]
        [InlineData(ServerErrorCode.CappedPositionLost, false)]
        [InlineData(ServerErrorCode.CursorKilled, false)]
        [InlineData(ServerErrorCode.Interrupted, false)]
        public void IsResumableChangeStreamException_should_return_expected_result_for_servers_with_old_behavior(object exceptionDescription, bool isResumable)
        {
            MongoException exception;
            if (exceptionDescription is Type exceptionType)
            {
                exception = (MongoException)CoreExceptionHelper.CreateException(exceptionType);
            }
            else
            {
                exception = CoreExceptionHelper.CreateMongoCommandException((int)exceptionDescription);
            }

            var result = RetryabilityHelper.IsResumableChangeStreamException(exception, Feature.ServerReturnsResumableChangeStreamErrorLabel.LastNotSupportedWireVersion);

            result.Should().Be(isResumable);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsResumableChangeStreamException_should_return_expected_result_for_servers_with_new_behavior([Values(false, true)] bool hasResumableChangeStreamErrorLabel)
        {
            var exception = CoreExceptionHelper.CreateMongoCommandException(-1);
            if (hasResumableChangeStreamErrorLabel)
            {
                exception.AddErrorLabel("ResumableChangeStreamError");
            }

            var result = RetryabilityHelper.IsResumableChangeStreamException(exception, Feature.ServerReturnsResumableChangeStreamErrorLabel.FirstSupportedWireVersion);

            result.Should().Be(hasResumableChangeStreamErrorLabel);
        }

        [Theory]
        [InlineData(typeof(MongoConnectionException), true)] // network exception
        [InlineData(typeof(MongoConnectionClosedException), false)]
        [InlineData(typeof(MongoCursorNotFoundException), true)]
        [InlineData(typeof(MongoConnectionPoolPausedException), true)]
        public void IsResumableChangeStreamException_should_return_expected_result_for_servers_with_new_behavior_and_errors(Type exceptionType, bool isResumable)
        {
            var exception = (MongoException)CoreExceptionHelper.CreateException(exceptionType);

            var result = RetryabilityHelper.IsResumableChangeStreamException(exception, Feature.ServerReturnsResumableChangeStreamErrorLabel.FirstSupportedWireVersion);

            result.Should().Be(isResumable);
        }

        [Theory]
        [InlineData(ServerErrorCode.ReauthenticationRequired, "saslStart", false)]
        [InlineData(ServerErrorCode.ReauthenticationRequired, "saslContinue", false)]
        [InlineData(ServerErrorCode.ReauthenticationRequired, "saslDummy", true)]
        [InlineData(ServerErrorCode.ReauthenticationRequired, "dummy", true)]
        [InlineData(1, "saslStart", false)]
        [InlineData(1, "saslContinue", false)]
        [InlineData(1, "saslNotExisted", false)]
        [InlineData(1, "dummy", false)]
        public void IsRetryableCommandAuthenticationException_should_return_expected_result_using_exception_type(int errorCode, string commandName, bool expectedResult)
        {
            var exception = CoreExceptionHelper.CreateMongoCommandException(errorCode);

            var result = RetryabilityHelper.IsReauthenticationRequested(exception, new BsonDocument(commandName, 1));

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(typeof(IOException), false)]
        [InlineData(typeof(MongoCursorNotFoundException), false)]
        [InlineData(typeof(MongoConnectionException), true)]
        [InlineData(typeof(MongoNodeIsRecoveringException), true)]
        [InlineData(typeof(MongoNotPrimaryException), true)]
        public void IsRetryableReadException_should_return_expected_result_using_exception_type(Type exceptionType, bool expectedResult)
        {
            var exception = CoreExceptionHelper.CreateException(exceptionType);

            var result = RetryabilityHelper.IsRetryableReadException(exception);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(ServerErrorCode.HostNotFound, true)]
        [InlineData(ServerErrorCode.HostUnreachable, true)]
        [InlineData(ServerErrorCode.NetworkTimeout, true)]
        [InlineData(ServerErrorCode.SocketException, true)]
        public void IsRetryableReadException_should_return_expected_result_using_code(int code, bool expectedResult)
        {
            var exception = CoreExceptionHelper.CreateMongoCommandException(code);

            var result = RetryabilityHelper.IsRetryableReadException(exception);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsRetryableWriteException_should_return_expected_result([Values(false, true)] bool hasRetryableWriteLabel)
        {
            var exception = CoreExceptionHelper.CreateMongoCommandException(-1);
            if (hasRetryableWriteLabel)
            {
                exception.AddErrorLabel("RetryableWriteError");
            }

            var result = RetryabilityHelper.IsRetryableWriteException(exception);

            result.Should().Be(hasRetryableWriteLabel);
        }
    }
}
