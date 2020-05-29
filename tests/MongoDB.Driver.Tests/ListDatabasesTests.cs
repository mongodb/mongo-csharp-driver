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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ListDatabasesTests
    {
        private string _databaseName = $"authorizedDatabases{Guid.NewGuid()}";
        private string _password = "authorizedDatabases";
        private string _roleName = $"listDatabases{Guid.NewGuid()}";
        private string _userName = $"authorizedDatabases{Guid.NewGuid()}";

        [SkippableTheory]
        [ParameterAttributeData]
        public void Execute_should_return_the_expected_result_when_AuthorizedDatabases_is_used(
            [Values(null, false, true)] bool? authorizedDatabases)
        {
            RequireServer.Check().Supports(Feature.ListDatabasesAuthorizedDatabases).Authentication(true);

            var setupClient = DriverTestConfiguration.Client;
            CreateListDatabasesRole(setupClient, _roleName);
            CreateListDatabasesUser(setupClient, _userName, _password, _databaseName, _roleName);
            setupClient.GetDatabase(_databaseName).GetCollection<BsonDocument>("test").InsertOne(new BsonDocument());

            var settings = DriverTestConfiguration.Client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(mechanism: null, source: null, username: _userName, password: _password);
            var testClient = new MongoClient(settings);

            var options = new ListDatabasesOptions
            {
                AuthorizedDatabases = authorizedDatabases,
                NameOnly = true,
            };
            var result = testClient.ListDatabases(options).ToList();

            if (authorizedDatabases.HasValue && authorizedDatabases.Value)
            {
                result.Should().BeEquivalentTo(new BsonArray { new BsonDocument { { "name", _databaseName } } });
            }
            else
            {
                result.Count.Should().BeGreaterThan(1);
            }
        }

        private void CreateListDatabasesRole(MongoClient client, string roleName)
        {
            var privileges = new BsonArray
            {
                new BsonDocument { { "resource", new BsonDocument { { "cluster", true } } }, { "actions", new BsonArray { "listDatabases" } } },
            };
            var command = new BsonDocument
            {
                { "createRole", roleName },
                { "privileges", privileges },
                { "roles", new BsonArray() },
            };

            client.GetDatabase("admin").RunCommand<BsonDocument>(command);
        }

        private void CreateListDatabasesUser(MongoClient client, string username, string password, string databaseName, string roleName)
        {
            var roles = new BsonArray
            {
                new BsonDocument { { "role", "read" }, { "db", databaseName } },
                new BsonDocument { { "role", roleName }, { "db", "admin" } },
            };
            var command = new BsonDocument
            {
                { "createUser", username },
                { "pwd", password },
                { "roles", roles },
            };

            client.GetDatabase("admin").RunCommand<BsonDocument>(command);
        }
    }
}
