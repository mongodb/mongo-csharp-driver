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
    internal class ServerParameterDependentFeatureDetector : VersionDependentFeatureDetector
    {
        // private fields
        private readonly string _serverParameterName;

        // constructors
        public ServerParameterDependentFeatureDetector(
            FeatureId featureId,
            Version firstSupportedInVersionId,
            string serverParameterName)
            : this(featureId, firstSupportedInVersionId, null, serverParameterName)
        {
        }

        public ServerParameterDependentFeatureDetector(
            FeatureId featureId,
            Version firstSupportedInVersionId,
            Version lastSupportedInVersionId,
            string serverParameterName)
            : base(featureId, firstSupportedInVersionId, lastSupportedInVersionId)
        {
            _serverParameterName = serverParameterName;
        }

        // public methods
        public override Feature DetectFeature(MongoServerInstance serverInstance, MongoConnection connection, MongoServerBuildInfo buildInfo)
        {
            var feature = base.DetectFeature(serverInstance, connection, buildInfo);
            if (!feature.IsSupported)
            {
                return feature;
            }

            if (!IsServerParameterSet(serverInstance, connection, _serverParameterName))
            {
                return NotSupportedInstance;
            }

            return feature;
        }

        // private methods
        private bool IsServerParameterSet(MongoServerInstance serverInstance, MongoConnection connection, string parameterName)
        {
            var command = new CommandDocument
            {
                { "getParameter", 1 },
                { parameterName, 1 }
            };
            var result = serverInstance.RunCommandAs<CommandResult>(connection, "admin", command);
            return result.Response[parameterName].ToBoolean();
        }
    }
}
