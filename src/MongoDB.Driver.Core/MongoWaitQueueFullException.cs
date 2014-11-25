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
using System.Net;
using System.Runtime.Serialization;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    [Serializable]
    public class MongoWaitQueueFullException : MongoClientException
    {
        #region static
        // static methods
        public static MongoWaitQueueFullException ForConnectionPool(EndPoint endPoint)
        {
            var message = string.Format(
                "The wait queue for acquiring a connection to server {0} is full.",
                EndPointHelper.ToString(endPoint));
            return new MongoWaitQueueFullException(message);
        }

        public static MongoWaitQueueFullException ForServerSelection()
        {
            var message = "The wait queue for server selection is full.";
            return new MongoWaitQueueFullException(message);
        }
        #endregion

        // constructors
        public MongoWaitQueueFullException(string message)
            : base(message, null)
        {
        }

        protected MongoWaitQueueFullException(SerializationInfo info, StreamingContext context)
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
