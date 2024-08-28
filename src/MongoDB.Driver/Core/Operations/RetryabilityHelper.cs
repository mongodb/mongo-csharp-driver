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
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

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
        private static readonly HashSet<string> __saslCommands;

        // static constructor
        static RetryabilityHelper()
        {
            var resumableAndRetryableExceptions = new HashSet<Type>()
            {
                typeof(MongoNotPrimaryException),
                typeof(MongoNodeIsRecoveringException),
                typeof(MongoConnectionPoolPausedException)
            };

            __resumableChangeStreamExceptions = new HashSet<Type>(resumableAndRetryableExceptions);

            __retryableReadExceptions = new HashSet<Type>(resumableAndRetryableExceptions);

            __retryableWriteExceptions = new HashSet<Type>(resumableAndRetryableExceptions);

            var retryableReadAndWriteErrorCodes = new HashSet<ServerErrorCode>
            {
                ServerErrorCode.ExceededTimeLimit,
                ServerErrorCode.HostNotFound,
                ServerErrorCode.HostUnreachable,
                ServerErrorCode.NetworkTimeout,
                ServerErrorCode.SocketException
            };

            __retryableReadErrorCodes = new HashSet<ServerErrorCode>(retryableReadAndWriteErrorCodes)
            {
                ServerErrorCode.ReadConcernMajorityNotAvailableYet
            };

            __retryableWriteErrorCodes = new HashSet<ServerErrorCode>(retryableReadAndWriteErrorCodes);

            __resumableChangeStreamErrorCodes = new HashSet<ServerErrorCode>()
            {
                ServerErrorCode.HostUnreachable,
                ServerErrorCode.HostNotFound,
                ServerErrorCode.NetworkTimeout,
                ServerErrorCode.ShutdownInProgress,
                ServerErrorCode.PrimarySteppedDown,
                ServerErrorCode.ExceededTimeLimit,
                ServerErrorCode.SocketException,
                ServerErrorCode.LegacyNotPrimary,
                ServerErrorCode.NotWritablePrimary,
                ServerErrorCode.InterruptedAtShutdown,
                ServerErrorCode.InterruptedDueToReplStateChange,
                ServerErrorCode.NotPrimaryNoSecondaryOk,
                ServerErrorCode.NotPrimaryOrSecondary,
                ServerErrorCode.StaleShardVersion,
                ServerErrorCode.StaleEpoch,
                ServerErrorCode.StaleConfig,
                ServerErrorCode.RetryChangeStream,
                ServerErrorCode.FailedToSatisfyReadPreference
            };

            __saslCommands = new HashSet<string>
            {
                SaslAuthenticator.SaslStartCommand,
                SaslAuthenticator.SaslContinueCommand
            };
        }

        // public static methods
        public static void AddRetryableWriteErrorLabelIfRequired(MongoException exception, ConnectionDescription connectionDescription)
        {
            if (ShouldRetryableWriteExceptionLabelBeAdded(exception, connectionDescription))
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

        public static bool IsResumableChangeStreamException(Exception exception, int maxWireVersion)
        {
            if (IsNetworkException(exception))
            {
                return true;
            }
            if (exception is MongoCursorNotFoundException)
            {
                return true;
            }
            if (exception is MongoConnectionPoolPausedException)
            {
                return true;
            }

            if (Feature.ServerReturnsResumableChangeStreamErrorLabel.IsSupported(maxWireVersion))
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

        /// <summary>
        /// Value indicating whether the exception requests additional authentication attempt.
        /// </summary>
        /// <param name="mongoCommandException">The command exception.</param>
        /// <param name="command">The command.</param>
        /// <returns>The flag.</returns>
        /// <remarks>
        /// This logic is completely separate from a standard retry mechanism and related only to authentication.
        /// </remarks>
        public static bool IsReauthenticationRequested(MongoCommandException mongoCommandException, BsonDocument command)
            => mongoCommandException.Code == (int)ServerErrorCode.ReauthenticationRequired &&
               // SASL commands should not be reauthenticated on sending level
               !__saslCommands.Overlaps(command.Names);

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

        private static bool ShouldRetryableWriteExceptionLabelBeAdded(Exception exception, ConnectionDescription connectionDescription)
        {
            if (IsNetworkException(exception))
            {
                return true;
            }

            var maxWireVersion = connectionDescription.MaxWireVersion;
            if (Feature.ServerReturnsRetryableWriteErrorLabel.IsSupported(maxWireVersion))
            {
                return false;
            }

            // on all servers from 4.4 on we would have returned false in the previous if statement
            // so from this point on we know we are connected to a pre 4.4 server

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

            var serverType = connectionDescription.HelloResult.ServerType;
            if (serverType != ServerType.ShardRouter)
            {
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
                            case ServerErrorCode.LegacyNotPrimary:
                            case ServerErrorCode.NotWritablePrimary:
                            case ServerErrorCode.NotPrimaryNoSecondaryOk:
                            case ServerErrorCode.NotPrimaryOrSecondary:
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
            }

            return false;
        }
    }
}
