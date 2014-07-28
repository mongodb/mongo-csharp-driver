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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Servers
{
    public enum ServerType
    {
        Unknown = 0,
        Standalone,
        ShardRouter,
        ReplicaSetPrimary,
        ReplicaSetSecondary,
        ReplicaSetPassive,
        ReplicaSetArbiter,
        ReplicaSetOther,
        ReplicaSetGhost
    }

    public static class ServerTypeExtensions
    {
        public static bool IsReplicaSetMember(this ServerType serverType)
        {
            switch (serverType)
            {
                case ServerType.ReplicaSetPrimary:
                case ServerType.ReplicaSetSecondary:
                case ServerType.ReplicaSetArbiter:
                case ServerType.ReplicaSetOther:
                case ServerType.ReplicaSetGhost:
                    return true;
                default:
                    return false;
            }
        }
    }
}
