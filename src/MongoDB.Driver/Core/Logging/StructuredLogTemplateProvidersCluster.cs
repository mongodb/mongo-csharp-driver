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
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Logging
{
    internal static partial class StructuredLogTemplateProviders
    {
        private static string[] __clusterCommonParams = new[]
        {
            TopologyId,
            Message,
        };

        private static string[] __serverSelectionCommonParams = new[]
        {
            TopologyId,
            Selector,
            OperationId,
            Operation,
            TopologyDescription,
            Message,
        };

        private static string ClusterCommonParams(params string[] @params) => Concat(__clusterCommonParams, @params);

        private static string ServerSelectionCommonParams(params string[] @params) => Concat(__serverSelectionCommonParams, @params);

        private static void AddClusterTemplates()
        {
            AddTemplateProvider<ClusterDescriptionChangedEvent>(
                LogLevel.Debug,
                ClusterCommonParams(PreviousDescription, NewDescription),
                (e, _) => GetParams(e.ClusterId, "Topology description changed", e.OldDescription, e.NewDescription));

            AddTemplateProvider<ClusterSelectingServerEvent>(
              LogLevel.Debug,
              ServerSelectionCommonParams(),
              (e, _) => GetParams(
                  e.ClusterId,
                  e.ServerSelector.ToString(),
                  e.OperationId,
                  e.OperationName,
                  e.ClusterDescription.ToString(),
                  "Server selection started"));

            AddTemplateProvider<ClusterEnteredSelectionQueueEvent>(
                LogLevel.Information,
                ServerSelectionCommonParams("remainingTimeMS"),
                (e, _) => GetParams(
                    e.ClusterId,
                    e.ServerSelector.ToString(),
                    e.OperationId,
                    e.OperationName,
                    e.ClusterDescription.ToString(),
                    "Waiting for suitable server to become available",
                    (long)e.RemainingTimeout.TotalMilliseconds));

            AddTemplateProvider<ClusterSelectedServerEvent>(
                LogLevel.Debug,
                ServerSelectionCommonParams(ServerHost, ServerPort),
                (e, _) => GetParams(
                    e.ClusterId,
                    e.ServerSelector.ToString(),
                    e.OperationId,
                    e.OperationName,
                    e.ClusterDescription.ToString(),
                    "Server selection succeeded",
                    e.SelectedServer.ServerId.EndPoint));

            AddTemplateProvider<ClusterSelectingServerFailedEvent>(
                LogLevel.Debug,
                ServerSelectionCommonParams(Failure),
                (e, s) => GetParams(
                    e.ClusterId,
                    e.ServerSelector.ToString(),
                    e.OperationId,
                    e.OperationName,
                    e.ClusterDescription.ToString(),
                    "Server selection failed",
                    FormatException(e.Exception, s)));

            AddTemplateProvider<ClusterClosingEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Stopping topology monitoring"));

            AddTemplateProvider<ClusterClosedEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Stopped topology monitoring"));

            AddTemplateProvider<ClusterOpeningEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Starting topology monitoring"));

            AddTemplateProvider<ClusterOpenedEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Started topology monitoring"));

            AddTemplateProvider<ClusterAddingServerEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ClusterId, e.EndPoint, "Adding server"));

            AddTemplateProvider<ClusterAddedServerEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Added server"));

            AddTemplateProvider<ClusterRemovingServerEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Removing server"));

            AddTemplateProvider<ClusterRemovedServerEvent>(
                LogLevel.Debug,
                CmapCommonParams(),
                (e, _) => GetParams(e.ServerId, "Removed server"));
        }
    }
}
