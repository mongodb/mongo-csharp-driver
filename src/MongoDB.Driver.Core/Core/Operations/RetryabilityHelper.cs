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
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    internal static class RetryabilityHelper
    {
        // private constants
        private const string ResumableChangeStreamErrorLabel = "ResumableChangeStreamError";
        private const string RetryableWriteErrorLabel = "RetryableWriteError";

        // private static fields
        private static readonly HashSet<ServerErrorCode> __resumableChangeStreamErrorCodes;
        private static readonly HashSet<Type> __resumableChangeStreamExceptions;
        private static readonly HashSet<Type> __retryableReadExceptions;
        private static readonly HashSet<Type> __retryableWriteExceptions;
        private static readonly HashSet<ServerErrorCode> __retryableReadErrorCodes;
        private static readonly HashSet<ServerErrorCode> __retryableWriteErrorCodes;

        // static constructor
        static RetryabilityHelper()
        {
            var resumableAndRetryableExceptions = new HashSet<Type>()
            {
                typeof(MongoNotPrimaryException),
                typeof(MongoNodeIsRecoveringException)
            };

            __resumableChangeStreamExceptions = new HashSet<Type>(resumableAndRetryableExceptions);

            __retryableReadExceptions = new HashSet<Type>(resumableAndRetryableExceptions);

            __retryableWriteExceptions = new HashSet<Type>(resumableAndRetryableExceptions);

            var resumableAndRetryableErrorCodes = new HashSet<ServerErrorCode>
            {
                ServerErrorCode.HostNotFound,
                ServerErrorCode.HostUnreachable,
                ServerErrorCode.NetworkTimeout,
                ServerErrorCode.SocketException
            };

            __retryableReadErrorCodes = new HashSet<ServerErrorCode>(resumableAndRetryableErrorCodes);

            __retryableWriteErrorCodes = new HashSet<ServerErrorCode>(resumableAndRetryableErrorCodes)
            {
                ServerErrorCode.ExceededTimeLimit
            };

            __resumableChangeStreamErrorCodes = new HashSet<ServerErrorCode>()
            {
                ServerErrorCode.HostUnreachable,
                ServerErrorCode.HostNotFound,
                ServerErrorCode.NetworkTimeout,
                ServerErrorCode.ShutdownInProgress,
                ServerErrorCode.PrimarySteppedDown,
                ServerErrorCode.ExceededTimeLimit,
                ServerErrorCode.SocketException,
                ServerErrorCode.NotMaster,
                ServerErrorCode.InterruptedAtShutdown,
                ServerErrorCode.InterruptedDueToReplStateChange,
                ServerErrorCode.NotMasterNoSlaveOk,
                ServerErrorCode.NotMasterOrSecondary,
                ServerErrorCode.StaleShardVersion,
                ServerErrorCode.StaleEpoch,
                ServerErrorCode.StaleConfig,
                ServerErrorCode.RetryChangeStream,
                ServerErrorCode.FailedToSatisfyReadPreference
            };
        }

        // public static methods
        public static void AddRetryableWriteErrorLabelIfRequired(MongoException exception, SemanticVersion serverVersion)
        {
            if (ShouldRetryableWriteExceptionLabelBeAdded(exception, serverVersion))
            {
                exception.AddErrorLabel(RetryableWriteErrorLabel);
            }
        }

        public static bool IsCommandRetryable(BsonDocument command)
        {
            return
                command.Contains("txnNumber") || // retryWrites=true
                command.Contains("commitTransaction") ||
                command.Contains("abortTransaction");
        }

        public static bool IsResumableChangeStreamException(Exception exception, SemanticVersion serverVersion)
        {
            if (IsNetworkException(exception))
            {
                return true;
            }

            if (Feature.ServerReturnsResumableChangeStreamErrorLabel.IsSupported(serverVersion))
            {
                return exception is MongoException mongoException ? mongoException.HasErrorLabel(ResumableChangeStreamErrorLabel) : false;
            }
            else
            {
                var commandException = exception as MongoCommandException;
                if (commandException != null)
                {
                    var code = (ServerErrorCode)commandException.Code;
                    if (__resumableChangeStreamErrorCodes.Contains(code))
                    {
                        return true;
                    }
                }

                return __resumableChangeStreamExceptions.Contains(exception.GetType());
            }
        }

        public static bool IsRetryableReadException(Exception exception)
        {
            if (__retryableReadExceptions.Contains(exception.GetType()) || IsNetworkException(exception))
            {
                return true;
            }

            var commandException = exception as MongoCommandException;
            if (commandException != null)
            {
                var code = (ServerErrorCode)commandException.Code;
                if (__retryableReadErrorCodes.Contains(code))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsRetryableWriteException(Exception exception)
        {
            return exception is MongoException mongoException ? mongoException.HasErrorLabel(RetryableWriteErrorLabel) : false;
        }

        // private static methods
        private static bool IsNetworkException(Exception exception)
        {
            return exception is MongoConnectionException mongoConnectionException && mongoConnectionException.IsNetworkException;
        }

        private static bool ShouldRetryableWriteExceptionLabelBeAdded(Exception exception, SemanticVersion serverVersion)
        {
            if (!Feature.RetryableWrites.IsSupported(serverVersion))
            {
                return false;
            }

            if (IsNetworkException(exception))
            {
                return true;
            }

            if (Feature.ServerReturnsRetryableWriteErrorLabel.IsSupported(serverVersion))
            {
                return false;
            }

            if (__retryableWriteExceptions.Contains(exception.GetType()))
            {
                return true;
            }

            var commandException = exception as MongoCommandException;
            if (commandException != null)
            {
                var code = (ServerErrorCode)commandException.Code;
                if (__retryableWriteErrorCodes.Contains(code))
                {
                    return true;
                }
            }

            var writeConcernException = exception as MongoWriteConcernException;
            if (writeConcernException != null)
            {
                var writeConcernError = writeConcernException.WriteConcernResult.Response.GetValue("writeConcernError", null)?.AsBsonDocument;
                if (writeConcernError != null)
                {
                    var code = (ServerErrorCode)writeConcernError.GetValue("code", -1).AsInt32;
                    switch (code)
                    {
                        case ServerErrorCode.InterruptedAtShutdown:
                        case ServerErrorCode.InterruptedDueToReplStateChange:
                        case ServerErrorCode.NotMaster:
                        case ServerErrorCode.NotMasterNoSlaveOk:
                        case ServerErrorCode.NotMasterOrSecondary:
                        case ServerErrorCode.PrimarySteppedDown:
                        case ServerErrorCode.ShutdownInProgress:
                        case ServerErrorCode.HostNotFound:
                        case ServerErrorCode.HostUnreachable:
                        case ServerErrorCode.NetworkTimeout:
                        case ServerErrorCode.SocketException:
                        case ServerErrorCode.ExceededTimeLimit:
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
