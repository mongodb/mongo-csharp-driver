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

#if NETCOREAPP3_1_OR_GREATER
using Amazon.Lambda.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using System;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaTest
{
    public class ShareMongoClientLambdaHandler
    {
        // Start AWS Lambda Example 1
        private static MongoClient MongoClient { get; set; }
        private static MongoClient CreateMongoClient()
        {
            var mongoClientSettings = MongoClientSettings.FromConnectionString($"<MONGODB_URI>");
            mongoClientSettings.ServerApi = new ServerApi(ServerApiVersion.V1, strict: true);
            return new MongoClient(mongoClientSettings);
        }

        static ShareMongoClientLambdaHandler()
        {
            MongoClient = CreateMongoClient();
        }

        public string HandleRequest(ILambdaContext context)
        {
            var database = MongoClient.GetDatabase("db");
            var collection = database.GetCollection<BsonDocument>("coll");
            var result = collection.Find(FilterDefinition<BsonDocument>.Empty).First();
            return result.ToString();
        }
        // End AWS Lambda Example 1
    }

    public class ConnectUsingAwsIamAuthenticatorLambdaHandler
    {
        private static MongoClient MongoClient { get; set; }
        private static MongoClient CreateMongoClient()
        {
            // Start AWS Lambda Example 2
            string username = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            string password = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            string awsSessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");

            var awsCredentials =
                new MongoCredential("MONGODB-AWS", new MongoExternalIdentity(username), new PasswordEvidence(password))
                .WithMechanismProperty("AWS_SESSION_TOKEN", awsSessionToken);

            var mongoUrl = MongoUrl.Create($"<MONGODB_URI>");

            var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);
            mongoClientSettings.Credential = awsCredentials;
            mongoClientSettings.ServerApi = new ServerApi(ServerApiVersion.V1, strict: true);

            return new MongoClient(mongoClientSettings);
            // End AWS Lambda Example 2
        }

        static ConnectUsingAwsIamAuthenticatorLambdaHandler()
        {
            MongoClient = CreateMongoClient();
        }

        public string HandleRequest(ILambdaContext context)
        {
            var database = MongoClient.GetDatabase("db");
            var collection = database.GetCollection<BsonDocument>("coll");
            var result = collection.Find(FilterDefinition<BsonDocument>.Empty).First();
            return result.ToString();
        }
    }
}
#endif
