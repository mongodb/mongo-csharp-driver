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
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Communication.FeatureDetection
{
    internal class FeatureTableCreator
    {
        // private static fields
        private static readonly IFeatureDetector[] __featureDetectors = new[]
        {
            // added in 2.3.0
            new ServerParameterDependentFeatureDetector(FeatureId.FailPoints, new Version(2, 3, 0), "enableTestCommands"),

            // added in 2.5.2
            new VersionDependentFeatureDetector(FeatureId.AggregateWithCursor, new Version(2, 5, 2)),
            new VersionDependentFeatureDetector(FeatureId.AggregateWithDollarOut, new Version(2, 5, 2)),

            // added in 2.5.3
            new VersionDependentFeatureDetector(FeatureId.MaxTime, new Version(2, 5, 3)) // while MaxTime was added in 2.5.2 the FailPoint for it wasn't added until 2.5.3
        };

        // private fields
        private readonly MongoServerBuildInfo _buildInfo;
        private readonly MongoConnection _connection;
        private readonly MongoServerInstance _serverInstance;

        // constructors
        public FeatureTableCreator(MongoServerInstance serverInstance, MongoConnection connection, MongoServerBuildInfo buildInfo)
        {
            _serverInstance = serverInstance;
            _connection = connection;
            _buildInfo = buildInfo;
        }

        // public methods
        public FeatureTable CreateFeatureTable()
        {
            var featureTable = new FeatureTable();

            foreach (var featureDetector in __featureDetectors)
            {
                featureTable.AddFeature(featureDetector.DetectFeature(_serverInstance, _connection, _buildInfo));
            }

            return featureTable;
        }
    }
}
