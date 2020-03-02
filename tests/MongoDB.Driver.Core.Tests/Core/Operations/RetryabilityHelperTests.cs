/* Copyright 2018-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class RetryabilityHelperTests
    {
        [Theory]
        [InlineData(1, false)]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, true)]
        [InlineData(ServerErrorCode.InterruptedDueToReplStateChange, true)]
        [InlineData(ServerErrorCode.NotMaster, true)]
        [InlineData(ServerErrorCode.NotMasterNoSlaveOk, true)]
        [InlineData(ServerErrorCode.NotMasterOrSecondary, true)]
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

            RetryabilityHelper.AddRetryableWriteErrorLabelIfRequired(exception);

            var hasRetryableWriteErrorLabel = exception.HasErrorLabel("RetryableWriteError");
            hasRetryableWriteErrorLabel.Should().Be(shouldAddErrorLabel);
        }

        [Fact]
        public void AddRetryableWriteErrorLabelIfRequired_should_add_RetryableWriteError_for_network_errors()
        {
            var exception = (MongoException)CoreExceptionHelper.CreateException(typeof(MongoConnectionException));

            RetryabilityHelper.AddRetryableWriteErrorLabelIfRequired(exception);

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

            RetryabilityHelper.AddRetryableWriteErrorLabelIfRequired(exception);

            var hasRetryableWriteErrorLabel = exception.HasErrorLabel("RetryableWriteError");
            hasRetryableWriteErrorLabel.Should().Be(shouldAddErrorLabel);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(ServerErrorCode.HostNotFound, true)]
        [InlineData(ServerErrorCode.HostUnreachable, true)]
        [InlineData(ServerErrorCode.NetworkTimeout, true)]
        [InlineData(ServerErrorCode.SocketException, true)]
        [InlineData(ServerErrorCode.ElectionInProgress, true)]
        [InlineData(ServerErrorCode.ExceededTimeLimit, true)]
        [InlineData(ServerErrorCode.RetryChangeStream, true)]
        [InlineData(ServerErrorCode.WriteConcernFailed, true)]
        [InlineData(ServerErrorCode.CappedPositionLost, false)]
        [InlineData(ServerErrorCode.CursorKilled, false)]
        [InlineData(ServerErrorCode.Interrupted, false)]
        public void IsResumableChangeStreamException_should_return_expected_result_using_code(int code, bool expectedResult)
        {
            var exception = CoreExceptionHelper.CreateMongoCommandException(code);

            var result = RetryabilityHelper.IsResumableChangeStreamException(exception);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("NonResumableChangeStreamError", false)]
        public void IsResumableChangeStreamException_should_return_expected_result_using_error_label(string label, bool expectedResult)
        {
            var exception = CoreExceptionHelper.CreateMongoCommandException(label: label);

            var result = RetryabilityHelper.IsResumableChangeStreamException(exception);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(typeof(IOException), false)]
        [InlineData(typeof(MongoConnectionException), true)]
        [InlineData(typeof(MongoCursorNotFoundException), true)]
        [InlineData(typeof(MongoNodeIsRecoveringException), true)]
        [InlineData(typeof(MongoNotPrimaryException), true)]
        public void IsResumableChangeStreamException_should_return_expected_result_using_exception_type(Type exceptionType, bool expectedResult)
        {
            var exception = CoreExceptionHelper.CreateException(exceptionType);

            var result = RetryabilityHelper.IsResumableChangeStreamException(exception);

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
