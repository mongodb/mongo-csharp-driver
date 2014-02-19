/* Copyright 2010-2014 MongoDB Inc.
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
    internal class FeatureSetDetector
    {
        // private static fields
        private static readonly FeatureSetDependency[] __featureSetDependencies = new[]
        {
            // present in all versions
            new FeatureSetDependency(
                new ServerVersionDependency(0, 0, 0),
                FeatureId.WriteOpcodes),

            // added in 2.4.0
            new FeatureSetDependency(
                new ServerVersionDependency(new Version(2, 4, 0)),
                FeatureId.TextSearchCommand),

            // added in 2.5.2
            new FeatureSetDependency(
                new ServerVersionDependency(2, 5, 2),
                FeatureId.AggregateAllowDiskUse,
                FeatureId.AggregateCursor,
                FeatureId.AggregateOutputToCollection),

            // added in 2.5.3
            new FeatureSetDependency(
                new ServerVersionDependency(2, 5, 3),
                FeatureId.AggregateExplain,
                FeatureId.MaxTime,
                FeatureId.UserManagementCommands),

            // added in 2.5.5
            new FeatureSetDependency(
                new ServerVersionDependency(2, 5, 5),
                FeatureId.CreateIndexCommand,
                FeatureId.TextSearchQuery,
                FeatureId.WriteCommands),

            // added in 2.5.5 but not on mongos
            new FeatureSetDependency(
                new AndDependency(
                    new ServerVersionDependency(2, 5, 5),
                    new NotDependency(new InstanceTypeDependency(MongoServerInstanceType.ShardRouter))),
                FeatureId.ParallelScanCommand)
       };

        // public methods
        public FeatureSet DetectFeatureSet(FeatureContext context)
        {
            var featureSet = new FeatureSet();

            foreach (var featureSetDependency in __featureSetDependencies)
            {
                if (featureSetDependency.Dependency.IsMet(context))
                {
                    foreach (var featureId in featureSetDependency.FeatureIds)
                    {
                        featureSet.AddFeature(featureId);
                    }
                }
            }

            return featureSet;
        }
    }
}
