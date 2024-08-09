/* Copyright 2010-present MongoDB Inc.
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

using System.Collections.Generic;
using System.Net;

namespace MongoDB.Driver.Authentication
{
    /// <summary>
    /// Represents SASL context.
    /// </summary>
    public sealed class SaslContext
    {
        /// <summary>
        /// Remove endpoint of the current connection.
        /// </summary>
        public EndPoint EndPoint { get; init; }

        /// <summary>
        /// Cluster's end points.
        /// </summary>
        public IEnumerable<EndPoint> ClusterEndPoints { get; init; }

        /// <summary>
        /// Identity.
        /// </summary>
        public MongoIdentity Identity { get; init; }

        /// <summary>
        /// Identity Evidence.
        /// </summary>
        public MongoIdentityEvidence IdentityEvidence { get; init; }

        /// <summary>
        /// Configured SASL Mechanism.
        /// </summary>
        public string Mechanism { get; init; }

        /// <summary>
        /// SASL Mechanism's properties.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> MechanismProperties { get; init; }
    }
}
