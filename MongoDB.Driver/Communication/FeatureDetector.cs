/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.Driver.Internal
{
    internal class FeatureDetector
    {
        // private static fields
        private static readonly Feature[] __features = new[]
        {
            // added in 2.3.0
            new Feature(FeatureId.FailPoints, true, new Version(2, 3, 0)),

            // added in 2.5.2
            new Feature(FeatureId.AggregateWithCursor, true, new Version(2, 5, 2)),
            new Feature(FeatureId.AggregateWithDollarOut, true, new Version(2, 5, 2)),

            // added in 2.5.3
            new Feature(FeatureId.MaxTime, true, new Version(2, 5, 3)) // while MaxTime was added in 2.5.2 the FailPoint for it wasn't added until 2.5.3
        };

        // private fields
        private readonly MongoServerBuildInfo _buildInfo;
        private readonly MongoConnection _connection;
        private readonly MongoServerInstance _serverInstance;

        // constructors
        public FeatureDetector(MongoServerInstance serverInstance, MongoConnection connection, MongoServerBuildInfo buildInfo)
        {
            _serverInstance = serverInstance;
            _connection = connection;
            _buildInfo = buildInfo;
        }

        // public methods
        public FeatureTable CreateFeatureTable()
        {
            var featureTable = new FeatureTable();

            foreach (var feature in __features)
            {
                if (IsFeatureSupportedByThisServerInstance(feature))
                {
                    featureTable.AddFeature(feature);
                }
                else
                {
                    featureTable.AddFeature(UnsupportedFeatureFrom(feature));
                }
            }

            return featureTable;
        }

        // private methods
        private bool IsFeatureSupportedByThisServerInstance(Feature feature)
        {
            if (_buildInfo.Version < feature.FirstSupportedInVersion)
            {
                return false;
            }

            if (feature.LastSupportedInVersion != null && _buildInfo.Version > feature.LastSupportedInVersion)
            {
                return false;
            }

            if (feature.Id == FeatureId.FailPoints && !IsServerParameterSet("enableTestCommands"))
            {
                return false;
            }

            return true;
        }

        private bool IsServerParameterSet(string parameterName)
        {
            var command = new CommandDocument
            {
                { "getParameter", 1 },
                { parameterName, 1 }
            };
            var result = _serverInstance.RunCommandAs<CommandResult>(_connection, "admin", command);
            return result.Response[parameterName].ToBoolean();
        }

        private Feature UnsupportedFeatureFrom(Feature feature)
        {
            return new Feature(
                feature.Id,
                false, // isSupported
                feature.FirstSupportedInVersion,
                feature.LastSupportedInVersion);
        }
    }
}
