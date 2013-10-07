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
    internal class FeatureSetDetector
    {
        // private static fields
        private static readonly IFeatureDetector[] __featureDetectors = new[]
        {
            // added in 2.5.2
            new FeatureDetector(FeatureId.AggregateWithCursor, new ServerVersionDependency(2, 5, 2)),
            new FeatureDetector(FeatureId.AggregateWithDollarOut, new ServerVersionDependency(2, 5, 2)),
            new FeatureDetector(FeatureId.BatchModifyCommands,
                new ServerVersionDependency(new Version(2, 5, 2), new Version(2, 5, 2)), // prototype implementation only works with 2.5.2
                new ServerParameterDependency("enableExperimentalWriteCommands")), // and for now must be explicitly enabled in the server

            // added in 2.5.3
            new FeatureDetector(FeatureId.MaxTime, new ServerVersionDependency(2, 5, 3)) // while MaxTime was added in 2.5.2 the FailPoint for it wasn't added until 2.5.3
        };

        // public methods
        public FeatureSet DetectFeatureSet(FeatureContext context)
        {
            var featureSet = new FeatureSet();

            foreach (var featureDetector in __featureDetectors)
            {
                if (featureDetector.IsFeatureSupported(context))
                {
                    featureSet.AddFeature(featureDetector.FeatureId);
                }
            }

            return featureSet;
        }
    }
}
