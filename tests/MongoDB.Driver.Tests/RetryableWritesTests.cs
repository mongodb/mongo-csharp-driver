/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class RetryableWritesTests
    {
        [SkippableFact]
        public void Insert_with_RetryWrites_true_should_work_whether_retryable_writes_are_supported_or_not()
        {
            RequireServer.Check();

            using (var client = GetClient())
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
                var document = new BsonDocument("x", 1);
                collection.InsertOne(document);
            }
        }

        [SkippableFact]
        public void Retryable_write_errorlabel_should_not_be_added_with_retryWrites_false()
        {
            if (CoreTestConfiguration.Cluster.Description.Type == ClusterType.Sharded)
            {
                RequireServer
                    .Check()
                    .Supports(Feature.RetryableWrites, Feature.FailPointsFailCommandForSharded)
                    .ClusterTypes(ClusterType.Sharded);
            }
            else
            {
                RequireServer
                    .Check()
                    .Supports(Feature.RetryableWrites, Feature.FailPointsFailCommand)
                    .ClusterTypes(ClusterType.ReplicaSet);
            }

            var failPointWithRetryableError = @"
            {
                configureFailPoint : 'failCommand',
                mode : { times : 1 },
                data : {
                    failCommands : ['insert'],
                    errorCode : 262
                }
            }"; // no error label

            using (var client = GetClient(retryWrites: false))
            using (ConfigureFailPoint(failPointWithRetryableError))
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
                var document = new BsonDocument("x", 1);

                var exception = Record.Exception(() => collection.InsertOne(document));

                var e = exception.Should().BeOfType<MongoCommandException>().Subject;
                e.HasErrorLabel("RetryableWriteError").Should().BeFalse();
            }
        }

        [SkippableFact]
        public void Retryable_write_operation_should_throw_custom_exception_on_servers_using_mmapv1()
        {
            RequireSupportForRetryableWrites();
            RequireServer.Check().ClusterType(ClusterType.ReplicaSet).StorageEngine("mmapv1");

            using (var client = GetClient())
            using (var session = client.StartSession())
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
                var document = new BsonDocument("x", 1);
                var exception = Record.Exception(() => collection.InsertOne(document));

                exception.Message.Should().Contain(
                    "This MongoDB deployment does not support retryable writes. " +
                    "Please add retryWrites=false to your connection string.");
            }
        }

        [SkippableFact]
        public void TxnNumber_should_be_included_with_FindOneAndDelete()
        {
            RequireSupportForRetryableWrites();

            var events = new EventCapturer().Capture<CommandStartedEvent>(x => x.CommandName == "findAndModify");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .FindOneAndDelete("{x: 'asdfafsdf'}");

                var commandStartedEvent = (CommandStartedEvent)events.Next();

                commandStartedEvent.Command.GetValue("txnNumber").Should().Be(new BsonInt64(1));
            }
        }

        [SkippableFact]
        public void TxnNumber_should_be_included_with_FindOneAndReplace()
        {
            RequireSupportForRetryableWrites();

            var events = new EventCapturer().Capture<CommandStartedEvent>(x => x.CommandName == "findAndModify");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .FindOneAndReplace("{x: 'asdfafsdf'}", new BsonDocument("x", 1));

                var commandStartedEvent = (CommandStartedEvent)events.Next();

                commandStartedEvent.Command.GetValue("txnNumber").Should().Be(new BsonInt64(1));
            }
        }

        [SkippableFact]
        public void TxnNumber_should_be_included_with_FindOneAndUpdate()
        {
            RequireSupportForRetryableWrites();

            var events = new EventCapturer().Capture<CommandStartedEvent>(x => x.CommandName == "findAndModify");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .FindOneAndUpdate("{x: 'asdfafsdf'}", new BsonDocument("$set", new BsonDocument("x", 1)));

                var commandStartedEvent = (CommandStartedEvent)events.Next();

                commandStartedEvent.Command.GetValue("txnNumber").Should().Be(new BsonInt64(1));
            }
        }

        [SkippableFact]
        public void TxnNumber_should_be_included_with_DeleteOne()
        {
            RequireSupportForRetryableWrites();

            var events = new EventCapturer().Capture<CommandStartedEvent>(x => x.CommandName == "delete");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .DeleteOne("{x: 'asdfafsdf'}");

                var commandStartedEvent = (CommandStartedEvent)events.Next();

                commandStartedEvent.Command.GetValue("txnNumber").Should().Be(new BsonInt64(1));
            }
        }

        [SkippableFact]
        public void TxnNumber_should_be_included_with_InsertOne()
        {
            RequireSupportForRetryableWrites();

            var events = new EventCapturer().Capture<CommandStartedEvent>(x => x.CommandName == "insert");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .InsertOne(new BsonDocument("x", 1));

                var commandStartedEvent = (CommandStartedEvent)events.Next();

                commandStartedEvent.Command.GetValue("txnNumber").Should().Be(new BsonInt64(1));
            }
        }

        [SkippableFact]
        public void TxnNumber_should_be_included_with_ReplaceOne()
        {
            RequireSupportForRetryableWrites();

            var events = new EventCapturer().Capture<CommandStartedEvent>(x => x.CommandName == "update");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .ReplaceOne("{x: 'asdfafsdf'}", new BsonDocument("x", 1));

                var commandStartedEvent = (CommandStartedEvent)events.Next();

                commandStartedEvent.Command.GetValue("txnNumber").Should().Be(new BsonInt64(1));
            }
        }

        [SkippableFact]
        public void TxnNumber_should_be_included_with_UpdateOne()
        {
            RequireSupportForRetryableWrites();

            var events = new EventCapturer().Capture<CommandStartedEvent>(x => x.CommandName == "update");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .UpdateOne("{x: 'asdfafsdf'}", new BsonDocument("$set", new BsonDocument("x", 1)));

                var commandStartedEvent = (CommandStartedEvent)events.Next();

                commandStartedEvent.Command.GetValue("txnNumber").Should().Be(new BsonInt64(1));
            }
        }

        // private methods
        private DisposableMongoClient GetClient(bool retryWrites = true)
        {
            return GetClient(cb => { }, retryWrites);
        }

        private DisposableMongoClient GetClient(Action<ClusterBuilder> clusterConfigurator, bool retryWrites = true)
        {
            return DriverTestConfiguration.CreateDisposableClient((MongoClientSettings clientSettings) =>
            {
                clientSettings.ClusterConfigurator = clusterConfigurator;
                clientSettings.RetryWrites = retryWrites;
            });
        }

        private DisposableMongoClient GetClient(EventCapturer capturer)
        {
            return GetClient(cb => cb.Subscribe(capturer));
        }

        private FailPoint ConfigureFailPoint(string failpointCommand)
        {
            var cluster = DriverTestConfiguration.Client.Cluster;
            var session = NoCoreSession.NewHandle();
            return FailPoint.Configure(cluster, session, BsonDocument.Parse(failpointCommand));
        }

        private void RequireSupportForRetryableWrites()
        {
            RequireServer.Check()
                .VersionGreaterThanOrEqualTo("3.6.0-rc0")
                .ClusterTypes(ClusterType.Sharded, ClusterType.ReplicaSet);
        }
    }
}
