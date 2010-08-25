/* Copyright 2010 10gen Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.MongoDBClient.Internal {
    internal static class MongoConnectionPool {
        // TODO: implement a real connection pool
        #region public static methods
        public static MongoConnection AcquireConnection(
            MongoDatabase database
        ) {
            MongoServer server = database.Server;
            MongoServerAddress address = server.Addresses.FirstOrDefault();
            return new MongoConnection(address.Host, address.Port);
        }

        public static void ReleaseConnection(
            MongoConnection connection
        ) {
            connection.Dispose();
        }
        #endregion
    }
}
