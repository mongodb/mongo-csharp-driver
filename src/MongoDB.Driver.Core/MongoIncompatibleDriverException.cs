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
using System.Runtime.Serialization;
using MongoDB.Driver.Core.Clusters;

namespace MongoDB.Driver
{
    [Serializable]
    public class MongoIncompatibleDriverException : MongoClientException
    {
        #region static
        // static methods
        private static string FormatMessage(ClusterDescription clusterDescription)
        {
            return string.Format(
                "This version of the driver is not compatible with one or more of the servers to which it is connected: {0}.",
                clusterDescription);
        }
        #endregion

        // constructors
        public MongoIncompatibleDriverException(ClusterDescription clusterDescription)
            : base(FormatMessage(clusterDescription), null)
        {
        }

        protected MongoIncompatibleDriverException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        // methods
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
