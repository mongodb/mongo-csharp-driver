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
    internal class VersionDependentFeatureDetector : IFeatureDetector
    {
        // private fields
        private readonly FeatureId _featureId;
        private readonly Version _firstSupportedInVersion;
        private readonly Version _lastSupportedInVersion;
        private readonly Feature _supportedInstance;
        private readonly Feature _notSupportedInstance;

        // constructors
        public VersionDependentFeatureDetector(
            FeatureId featureId,
            Version firstSupportedInVersionId)
            : this(featureId, firstSupportedInVersionId, null)
        {
        }

        public VersionDependentFeatureDetector(
            FeatureId featureId,
            Version firstSupportedInVersionId,
            Version lastSupportedInVersionId)
        {
            _featureId = featureId;
            _firstSupportedInVersion = firstSupportedInVersionId;
            _lastSupportedInVersion = lastSupportedInVersionId;
            _supportedInstance = new Feature(featureId, true, firstSupportedInVersionId, lastSupportedInVersionId);
            _notSupportedInstance = new Feature(featureId, false, firstSupportedInVersionId, lastSupportedInVersionId);
        }

        // protected properties
        protected Feature NotSupportedInstance
        {
            get { return _notSupportedInstance; }
        }

        protected Feature SupportedInstance
        {
            get { return _supportedInstance; }
        }

        // public methods
        public virtual Feature DetectFeature(MongoServerInstance serverInstance, MongoConnection connection, MongoServerBuildInfo buildInfo)
        {
            if (_firstSupportedInVersion != null && buildInfo.Version < _firstSupportedInVersion)
            {
                return _notSupportedInstance;
            }

            if (_lastSupportedInVersion != null && buildInfo.Version > _lastSupportedInVersion)
            {
                return _notSupportedInstance;
            }

            return _supportedInstance;
        }
    }
}
