/* Copyright 2020-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Clusters
{
    internal static class ClusterConnectionModeHelper
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public static void EnsureConnectionModeValuesAreValid(ClusterConnectionMode connectionMode, ConnectionModeSwitch connectionModeSwitch, bool? directConnection)
        {
            switch (connectionModeSwitch)
            {
                case ConnectionModeSwitch.NotSet:
                    if (connectionMode != default)
                    {
                        throw new ArgumentException($"When connectionModeSwitch is NotSet connectionMode must have the default value.", nameof(connectionMode));
                    }
                    if (directConnection != default)
                    {
                        throw new ArgumentException($"When connectionModeSwitch is NotSet directConnection must have the default value.", nameof(directConnection));
                    }
                    break;
                case ConnectionModeSwitch.UseConnectionMode:
                    if (directConnection != default)
                    {
                        throw new ArgumentException($"When connectionModeSwitch is UseConnectionMode directConnection must have the default value.", nameof(directConnection));
                    }
                    break;
                case ConnectionModeSwitch.UseDirectConnection:
                    if (connectionMode != default)
                    {
                        throw new ArgumentException($"When connectionModeSwitch is UseDirectConnection connectionMode must have the default value.", nameof(connectionMode));
                    }
                    break;
                default:
                    throw new ArgumentException($"Invalid connectionMode: {connectionMode}.", nameof(connectionMode));
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
