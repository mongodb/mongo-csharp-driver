/* Copyright 2021-present MongoDB Inc.
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

namespace MongoDB.Driver.Examples
{
    public class VersionedApiExamples
    {
        public void ConfigureServerApi()
        {
            // Start Versioned API Example 1
            var connectionString = "mongodb://localhost";
            var serverApi = new ServerApi(ServerApiVersion.V1);
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            mongoClientSettings.ServerApi = serverApi; // ServerApi is not available as a connection string option and should be configured on the settings object
            var mongoClient = new MongoClient(mongoClientSettings);
            // End Versioned API Example 1
        }

        public void ConfigureServerApiStrict()
        {
            // Start Versioned API Example 2
            var connectionString = "mongodb://localhost";
            var serverApi = new ServerApi(ServerApiVersion.V1, strict: true);
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            mongoClientSettings.ServerApi = serverApi;
            var mongoClient = new MongoClient(mongoClientSettings);
            // End Versioned API Example 2
        }

        public void ConfigureServerApiNonStrict()
        {
            // Start Versioned API Example 3
            var connectionString = "mongodb://localhost";
            var serverApi = new ServerApi(ServerApiVersion.V1, strict: false); // Current server default is false, but it can be specified explicitly
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            mongoClientSettings.ServerApi = serverApi;
            var mongoClient = new MongoClient(mongoClientSettings);
            // End Versioned API Example 3
        }

        public void ConfigureServerApiDeprecationErrors()
        {
            // Start Versioned API Example 4
            var connectionString = "mongodb://localhost";
            var serverApi = new ServerApi(ServerApiVersion.V1, deprecationErrors: true);
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            mongoClientSettings.ServerApi = serverApi;
            var mongoClient = new MongoClient(mongoClientSettings);
            // End Versioned API Example 4
        }
    }
}
