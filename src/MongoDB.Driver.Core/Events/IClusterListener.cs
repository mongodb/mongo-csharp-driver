/* Copyright 2013-2014 MongoDB Inc.
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
using System.Net;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;
namespace MongoDB.Driver.Core.Events
{
    public interface IClusterListener : IListener
    {
        // methods
        void ClusterBeforeClosing(ClusterId clusterId);
        void ClusterAfterClosing(ClusterId clusterId, TimeSpan elapsed);

        void ClusterBeforeOpening(ClusterId clusterId, ClusterSettings settings);
        void ClusterAfterOpening(ClusterId clusterId, ClusterSettings settings, TimeSpan elapsed);

        void ClusterBeforeAddingServer(ClusterId clusterId, EndPoint endPoint);
        void ClusterAfterAddingServer(ServerId serverId, TimeSpan elapsed);
        
        void ClusterBeforeRemovingServer(ServerId serverId, string reason);
        void ClusterAfterRemovingServer(ServerId serverId, string reason, TimeSpan elapsed);

        void ClusterDescriptionChanged(ClusterDescription oldClusterDescription, ClusterDescription newClusterDescription);
    }
}