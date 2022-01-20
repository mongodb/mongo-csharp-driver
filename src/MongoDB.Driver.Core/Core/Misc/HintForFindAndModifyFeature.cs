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

namespace MongoDB.Driver.Core.Misc
{
    /// <summary>
    /// Represents the hint for find and modify feature.
    /// </summary>
    public class HintForFindAndModifyFeature : Feature
    {
        private readonly int _firstWireVersionWhereWeRelyOnServerToReturnError = WireVersion.Server42;

        /// <summary>
        /// Initializes a new instance of the <see cref="HintForFindAndModifyFeature"/> class.
        /// </summary>
        /// <param name="name">The name of the feature.</param>
        /// <param name="firstSupportedWireVersion">The first wire version that supports the feature.</param>
        public HintForFindAndModifyFeature(string name, int firstSupportedWireVersion)
            : base(name, firstSupportedWireVersion)
        {
        }

        /// <summary>
        /// Determines whether the driver must throw an exception if the feature is not supported by the server.
        /// </summary>
        /// <param name="wireVersion">The wire version.</param>
        /// <returns>Whether the driver must throw if feature is not supported.</returns>
        public bool DriverMustThrowIfNotSupported(int wireVersion)
        {
            return wireVersion < _firstWireVersionWhereWeRelyOnServerToReturnError;
        }
    }
}
