/* Copyright 2010-present MongoDB Inc.
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

using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Logging
{
    internal static partial class StructuredLogTemplateProviders
    {
        private static string[] __cmapCommonParams = new[]
        {
            TopologyId,
            ServerHost,
            ServerPort,
            Message,
        };

        private static string CmapCommonParams(params string[] @params) => Concat(__cmapCommonParams, @params);

        private static void AddCmapTemplates()
        {
            AddTemplateProvider<ConnectionPoolAddingConnectionEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Connection adding"));

            AddTemplateProvider<ConnectionPoolCheckingInConnectionEvent>(
                LogLevel.Debug,
                ConnectionCommonParams(),
                (e, _) => GetParams(e.ConnectionId, "Connection checking in"));

            AddTemplateProvider<ConnectionPoolCheckedInConnectionEvent>(
                LogLevel.Debug,
                ConnectionCommonParams(),
                (e, _) => GetParams(e.ConnectionId, "Connection checked in"));

            AddTemplateProvider<ConnectionPoolCheckingOutConnectionEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Connection checkout started"));

            AddTemplateProvider<ConnectionPoolCheckedOutConnectionEvent>(
                LogLevel.Debug,
                ConnectionCommonParams(DurationMS),
                (e, _) => GetParams(e.ConnectionId, "Connection checked out", e.Duration.TotalMilliseconds));

            AddTemplateProvider<ConnectionPoolCheckingOutConnectionFailedEvent>(
                LogLevel.Debug,
                CmapCommonParams(Reason, Error, DurationMS),
                (e, o) => GetParams(
                    e.ServerId,
                    "Connection checkout failed",
                    GetCheckoutFailedReason(e.Reason),
                    FormatException(e.Exception, o),
                    e.Duration.TotalMilliseconds));

            AddTemplateProvider<ConnectionPoolRemovingConnectionEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Connection removing"));

            AddTemplateProvider<ConnectionPoolRemovedConnectionEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Connection removed"));

#pragma warning disable CS0618 // Type or member is obsolete
            AddTemplate<ConnectionPoolOpeningEvent, ConnectionSettings>(
                LogLevel.Debug,
                CmapCommonParams(MaxIdleTimeMS, MinPoolSize, MaxPoolSize, MaxConnecting, WaitQueueTimeoutMS, WaitQueueSize),
                (e, _, s) => GetParams(
                    e.ServerId,
                    "Connection pool opening",
                    s.MaxIdleTime.TotalMilliseconds,
                    e.ConnectionPoolSettings.MinConnections,
                    e.ConnectionPoolSettings.MaxConnections,
                    e.ConnectionPoolSettings.MaxConnecting,
                    e.ConnectionPoolSettings.WaitQueueTimeout.TotalMilliseconds,
                    e.ConnectionPoolSettings.WaitQueueSize));

            AddTemplate<ConnectionPoolOpenedEvent, ConnectionSettings>(
                LogLevel.Debug,
                CmapCommonParams(MaxIdleTimeMS, MinPoolSize, MaxPoolSize, MaxConnecting, WaitQueueTimeoutMS, WaitQueueSize),
                (e, _, s) => GetParams(
                    e.ServerId,
                    "Connection pool created",
                    s.MaxIdleTime.TotalMilliseconds,
                    e.ConnectionPoolSettings.MinConnections,
                    e.ConnectionPoolSettings.MaxConnections,
                    e.ConnectionPoolSettings.MaxConnecting,
                    e.ConnectionPoolSettings.WaitQueueTimeout.TotalMilliseconds,
                    e.ConnectionPoolSettings.WaitQueueSize));

#pragma warning restore CS0618 // Type or member is obsolete

            AddTemplateProvider<ConnectionPoolReadyEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Connection pool ready"));

            AddTemplateProvider<ConnectionPoolClearingEvent>(
                LogLevel.Debug,
                CmapCommonParams(ServiceId),
                (e, _) => GetParams(e.ServerId, "Connection pool clearing", e.ServiceId));

            AddTemplateProvider<ConnectionPoolClearedEvent>(
                LogLevel.Debug,
                CmapCommonParams(ServiceId),
                (e, _) => GetParams(e.ServerId, "Connection pool cleared", e.ServiceId));

            AddTemplateProvider<ConnectionPoolClosingEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Connection pool closing"));

            AddTemplateProvider<ConnectionPoolClosedEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Connection pool closed"));
        }

        private static string GetCheckoutFailedReason(ConnectionCheckOutFailedReason connectionCheckOutFailedReason) =>
            connectionCheckOutFailedReason switch
            {
                ConnectionCheckOutFailedReason.ConnectionError => "An error occurred while trying to establish a new connection",
                ConnectionCheckOutFailedReason.PoolClosed => "Connection pool was closed",
                ConnectionCheckOutFailedReason.Timeout => "Wait queue timeout elapsed without a connection becoming available",
                _ => null
            };
    }
}
