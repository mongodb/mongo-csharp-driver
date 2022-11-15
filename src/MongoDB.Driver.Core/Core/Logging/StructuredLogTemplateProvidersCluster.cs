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
            ClusterId,
            Message,
        };

        private static string ClusterCommonParams(params string[] @params) => Concat(__clusterCommonParams, @params);

        private static void AddClusterTemplates()
        {
            AddTemplateProvider<ClusterDescriptionChangedEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Description changed"));

            AddTemplateProvider<ClusterSelectingServerEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Selecting server"));

            AddTemplateProvider<ClusterSelectedServerEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Selected server"));

            AddTemplateProvider<ClusterSelectingServerFailedEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Selecting server failed"));

            AddTemplateProvider<ClusterClosingEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Cluster closing"));

            AddTemplateProvider<ClusterClosedEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Cluster closed"));

            AddTemplateProvider<ClusterOpeningEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Cluster opening"));

            AddTemplateProvider<ClusterOpenedEvent>(
                LogLevel.Debug,
                ClusterCommonParams(),
                (e, _) => GetParams(e.ClusterId, "Cluster opened"));

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
