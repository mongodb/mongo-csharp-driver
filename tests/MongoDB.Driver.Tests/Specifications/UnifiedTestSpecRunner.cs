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

using System;
using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using MongoDB.Driver.Tests.UnifiedTestOperations;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.Driver.Tests.Specifications
{
    [Trait("Category", "Integration")]
    public class UnifiedTestSpecRunner : LoggableTestClass
    {
        public UnifiedTestSpecRunner(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper, true)
        {
        }

        [Category("Authentication", "MongoDbOidc")]
        [UnifiedTestsTheory("auth.tests.unified")]
        public void Auth(JsonDrivenTestCase testCase) => Run(testCase);

        [Category("SupportLoadBalancing")]
        [UnifiedTestsTheory("change_streams.tests.unified")]
        public void ChangeStreams(JsonDrivenTestCase testCase) => Run(testCase);

        [UnifiedTestsTheory("client_side_operations_timeout.tests")]
        public void ClientSideOperationsTimeout(JsonDrivenTestCase testCase)
        {
            SkipNotSupportedTestCases("dropIndexes");
            SkipNotSupportedTestCases("findOne");
            SkipNotSupportedTestCases("listIndexNames");
            // TODO: CSOT: further skipped tests should be unblocked by upcoming fixes
            SkipNotSupportedTestCases("with only 1 RTT"); // blocked by CSHARP-5627
            SkipNotSupportedTestCases("createChangeStream"); // TODO: CSOT not implemented yet, CSHARP-3539
            SkipNotSupportedTestCases("runCommand"); // TODO: CSOT: TimeoutMS is not implemented yet for runCommand
            SkipNotSupportedTestCases("timeoutMS applies to whole operation, not individual attempts"); // blocked by DRIVERS-3247
            SkipNotSupportedTestCases("WaitQueueTimeoutError does not clear the pool"); // TODO: CSOT: TimeoutMS is not implemented yet for runCommand
            SkipNotSupportedTestCases("write concern error MaxTimeMSExpired is transformed"); // TODO: CSOT: investigate error transformation, implementing the requirement might be breaking change
            SkipNotSupportedTestCases("operation succeeds after one socket timeout - listDatabases on client"); // TODO: listDatabases is not retryable in CSharp Driver, CSHARP-5714

            Run(testCase);

            void SkipNotSupportedTestCases(string operationName)
            {
                if (testCase.Name.Contains(operationName))
                {
                    throw new SkipException($"Test skipped because {operationName} is not supported.");
                }
            }
        }

        [Category("CSFLE")]
        [UnifiedTestsTheory("client_side_encryption.tests.unified")]
        public void ClientSideEncryption(JsonDrivenTestCase testCase)
        {
            var testCaseNameLower = testCase.Name.ToLower();

            if (testCaseNameLower.Contains("fle2v2-encryptedfields-vs-encryptedfieldsmap.json") ||
                testCaseNameLower.Contains("localschema.json") ||
                testCaseNameLower.Contains("qe-text"))
            {
                CoreTestConfiguration.SkipMongocryptdTests_SERVER_106469(true);
            }

            // This spec test includes an AWS sessionToken in its config, indicating it should use temporary AWS credentials
            if (testCaseNameLower.Contains("localschema.json"))
            {
                RequireEnvironment.Check().EnvironmentVariable("CSFLE_AWS_TEMPORARY_CREDS_ENABLED");
            }

            if (testCaseNameLower.Contains("kmip") ||
                testCase.Shared.ToString().ToLower().Contains("kmip"))
            {
                // kmip requires configuring kms mock server
                RequireKmsMock();
            }

            Run(testCase);
        }

        [UnifiedTestsTheory("connection_monitoring_and_pooling.tests.logging")]
        public void ConnectionMonitoringAndPooling(JsonDrivenTestCase testCase) => Run(testCase, IsCmapLogValid);

        [UnifiedTestsTheory("collection_management.tests")]
        public void CollectionManagement(JsonDrivenTestCase testCase) => Run(testCase);

        [UnifiedTestsTheory("command_logging_and_monitoring.tests.logging")]
        public void CommandLogging(JsonDrivenTestCase testCase) => Run(testCase);

        [UnifiedTestsTheory("command_logging_and_monitoring.tests.monitoring")]
        public void CommandMonitoring(JsonDrivenTestCase testCase) => Run(testCase);

        [Category("SupportLoadBalancing")]
        [UnifiedTestsTheory("crud.tests.unified")]
        public void Crud(JsonDrivenTestCase testCase) => Run(testCase);

        [UnifiedTestsTheory("gridfs.tests")]
        public void GridFS(JsonDrivenTestCase testCase) => Run(testCase);

        [UnifiedTestsTheory("index_management.tests")]
        public void IndexManagement(JsonDrivenTestCase testCase)
        {
            // Skip sharded due to CSHARP-5833/SERVER-117001
            RequireServer
                .Check()
                .VersionGreaterThanOrEqualTo(SemanticVersion.Parse("8.0.0"));

            Run(testCase);
        }

        [Category("SupportLoadBalancing")]
        [UnifiedTestsTheory("load_balancers.tests")]
        public void LoadBalancers(JsonDrivenTestCase testCase)
        {
#if DEBUG
            RequirePlatform
                .Check()
                .SkipWhen(SupportedOperatingSystem.Linux)
                .SkipWhen(SupportedOperatingSystem.MacOS);
            // Make sure that LB is started. "nginx" is a LB we use for windows testing
            RequireEnvironment.Check().ProcessStarted("nginx");
            Environment.SetEnvironmentVariable("MONGODB_URI", "mongodb://localhost:17017?loadBalanced=true");
            Environment.SetEnvironmentVariable("MONGODB_URI_WITH_MULTIPLE_MONGOSES", "mongodb://localhost:17018?loadBalanced=true");
            RequireServer
                .Check()
                .LoadBalancing(enabled: true, ignorePreviousSetup: true)
                .Authentication(authentication: false); // auth server requires credentials in connection string
#else
            RequireEnvironment // these env variables are used only on the scripting side
                .Check()
                .EnvironmentVariable("SINGLE_MONGOS_LB_URI")
                .EnvironmentVariable("MULTI_MONGOS_LB_URI");
            // EG currently supports LB only for Ubuntu
            RequirePlatform
                .Check()
                .SkipWhen(SupportedOperatingSystem.Windows)
                .SkipWhen(SupportedOperatingSystem.MacOS);
#endif

           Run(testCase);
        }

        [UnifiedTestsTheory("open_telemetry.operation")]
        public void OpenTelemetry(JsonDrivenTestCase testCase)
        {
            // TODO: Unskip the tests once CSHARP-5856 is fixed.
            if (testCase.Name.Contains("create_collection.json"))
            {
                var serverVersion = CoreTestConfiguration.ServerVersion;
                if (serverVersion < SemanticVersion.Parse("7.0.0"))
                {
                    throw new SkipException($"Test skipped for MongoDB < 7.0 due to non-idempotent createCollection. See CSHARP-5856.");
                }
            }

            Run(testCase);
        }

        [UnifiedTestsTheory("open_telemetry.transaction")]
        public void OpenTelemetryTransactions(JsonDrivenTestCase testCase) => Run(testCase);

        [Category("SupportLoadBalancing")]
        [UnifiedTestsTheory("read_write_concern.tests.operation")]
        public void ReadWriteConcern(JsonDrivenTestCase testCase) => Run(testCase);

        [Category("SupportLoadBalancing")]
        [UnifiedTestsTheory("retryable_reads.tests.unified")]
        public void RetryableReads(JsonDrivenTestCase testCase) => Run(testCase);

        [Category("SupportLoadBalancing")]
        [UnifiedTestsTheory("retryable_writes.tests.unified")]
        public void RetryableWrites(JsonDrivenTestCase testCase)
        {
            if (testCase.Name.Contains("bulkWrite.json") && testCase.Name.Contains("is never committed"))
            {
                // Unskip the tests once CSHARP-5444 is fixed.
                throw new SkipException("This test is skipped because csharp driver has bug with handling connection closing while mixedBulkWrite operation.");
            }

            Run(testCase);
        }

        [Category("SDAM", "SupportLoadBalancing")]
        [UnifiedTestsTheory("server_discovery_and_monitoring.tests.unified")]
        public void ServerDiscoveryAndMonitoring(JsonDrivenTestCase testCase)
            => Run(testCase, IsSdamLogValid, new SdamRunnerEventsProcessor(testCase.Name));

        [Category("SupportLoadBalancing")]
        [UnifiedTestsTheory("server_selection.tests.logging")]
        public void ServerSelection(JsonDrivenTestCase testCase) => Run(testCase);

        [UnifiedTestsTheory("sessions.tests")]
        public void Sessions(JsonDrivenTestCase testCase) => Run(testCase);

        [Category("SupportLoadBalancing")]
        [UnifiedTestsTheory("transactions.tests.unified")]
        public void Transactions(JsonDrivenTestCase testCase)
        {
            if (CoreTestConfiguration.Cluster.Description.Type == ClusterType.Sharded &&
                (testCase.Name.StartsWith("read-concern.json:only first distinct includes readConcern") ||
                testCase.Name.StartsWith("read-concern.json:distinct ignores collection readConcern") ||
                testCase.Name.StartsWith("pin-mongos.json:distinct") ||
                testCase.Name.StartsWith("reads.json:distinct")))
            {
                RequireServer.Check().VersionGreaterThanOrEqualTo(SemanticVersion.Parse("4.4"));
            }

            Run(testCase);
        }

        [UnifiedTestsTheory("transactions_convenient_api.tests.unified")]
        public void TransactionsConvenientApi(JsonDrivenTestCase testCase) => Run(testCase);

        [UnifiedTestsTheory("unified_test_format.tests.valid_fail")]
        public void UnifiedTestFormatValidFail(JsonDrivenTestCase testCase)
        {
            if (testCase.Name.Contains("kmsProviders"))
            {
                // kmip requires configuring kms mock server
                RequireKmsMock();
            }

            Record.Exception(() => Run(testCase)).Should().NotBeNull();
        }

        [UnifiedTestsTheory("unified_test_format.tests.valid_pass")]
        public void UnifiedTestFormatValidPass(JsonDrivenTestCase testCase)
        {
            if (testCase.Name.Contains("kmsProviders"))
            {
                // kmip requires configuring kms mock server
                RequireKmsMock();
            }

            Run(testCase);
        }

        [Category("SupportLoadBalancing")]
        [UnifiedTestsTheory("versioned_api.tests")]
        public void VersionedApi(JsonDrivenTestCase testCase) => Run(testCase);

        private void Run(JsonDrivenTestCase testCase, Predicate<LogEntry> loggingFilter = null, IEventsProcessor eventsProcessor = null)
        {
            using (var runner = new UnifiedTestRunner(loggingService: this, loggingFilter: loggingFilter, eventsProcessor: eventsProcessor))
            {
                runner.Run(testCase);
            }
        }

        private static void RequireKmsMock() =>
            RequireEnvironment.Check().EnvironmentVariable("KMS_MOCK_SERVERS_ENABLED");

        // used by SkippedTestsProvider property in UnifiedTests attribute.
        private static readonly HashSet<string> __ignoredTests = new(
        [
            // CMAP
            "waitQueueMultiple should be included in connection pool created message when specified",

            // commandLogging
            // .NET driver has a fallback logic to get a server connectionId based on an additional getLastError call which is not expected by the spec.
            "command log messages do not include server connection id",

            // commandMonitoring
            // CSHARP-3823
            "hello with speculative authenticate",
            "hello without speculative authenticate is not redacted",
            "legacy hello with speculative authenticate",
            "legacy hello without speculative authenticate is not redacted",

            // retryableReads
            "collection.findOne succeeds after retryable handshake network error",
            "collection.findOne succeeds after retryable handshake server error (ShutdownInProgress)",
            "collection.listIndexNames succeeds after retryable handshake server error (ShutdownInProgress)",
            "collection.listIndexNames succeeds after retryable handshake network error",

            // SDAM
            // "Not implemented: https://jira.mongodb.org/browse/CSHARP-3138"
            "connectTimeoutMS=0",
            // https://jira.mongodb.org/browse/CSHARP-4459
            "Ignore network timeout error on find",
            "Network timeout on Monitor check",
            "Reset server and pool after network timeout error during authentication",

            // unifiedTestFormatPassFail
            // CSHARP-3823
            "hello with speculativeAuthenticate",
            "hello without speculativeAuthenticate is always observed",
            "legacy hello with speculativeAuthenticate",
            "legacy hello without speculativeAuthenticate is always observed",

            // transactions
            // CSHARP Driver does not comply with the requirement to throw in case explicit writeConcern were used, see CSHARP-5468
            "client bulkWrite with writeConcern in a transaction causes a transaction error",
        ]);

        private static readonly HashSet<string> __ignoredTestFiles = new(
        [
            // retryableReads
            // .NET/C# driver does not implement FindOne, ListCollectionObjects, ListDatabaseObjects, ListIndexNames helpers
            "findOne.json",
            "findOne-serverErrors.json",
            "listCollectionObjects.json",
            "listCollectionObjects.json",
            "listCollectionObjects-serverErrors.json",
            "listDatabaseObjects.json",
            "listDatabaseObjects-serverErrors.json",
            "listIndexNames.json",
            "listIndexNames-serverErrors.json"
        ]);

        #region CMAP helpers

        private static readonly HashSet<string> __ignoredCmapLogsMessages = new HashSet<string>(
            new[]
            {
               "Connection pool opening",
               "Connection adding",
               "Connection added",
               "Connection adding",
               "Connection checking in",
               "Connection removing",
               "Connection removed",
               "Connection pool closing",
               "Connection pool clearing",
               "Connection opening",
               "Connection failed",
               "Connection opening failed",
               "Connection closing",
               "Sending",
               "Sending failed",
               "Sent",
               "Receiving",
               "Receiving failed",
               "Received"
            });

        private static string __connectionCategoryName = LogCategoryHelper.GetCategoryName<LogCategories.Connection>();

        private static bool IsCmapLogValid(LogEntry logEntry) =>
            logEntry.Category != __connectionCategoryName ||
            !__ignoredCmapLogsMessages.Contains(logEntry.GetParameter<string>(StructuredLogTemplateProviders.Message));

        #endregion

        #region SDAM helpers

        private sealed class SdamRunnerEventsProcessor : IEventsProcessor
        {
            private readonly string _testCaseName;

            public SdamRunnerEventsProcessor(string testCaseName)
            {
                _testCaseName = Ensure.IsNotNull(testCaseName, nameof(testCaseName));
            }

            public void PostProcessEvents(List<object> events, string type)
            {
                // This is workaround. Our current implementation doesn't generate connection closing events in the order expected by the spec.
                // Spec expected order:
                //      1. PoolClear; 2. CheckIn; 3. ConnectionClosed;
                // but the currently triggered:
                //      1. PoolClear; 2. ConnectionClosed; 3. CheckIn;
                // So, below we manually change the events order for single pair of events for a specific connection to match the spec expectations
                // See for details: CSHARP-4458
                if (type == "cmap" && _testCaseName.Contains("InUseConnections"))
                {
                    var clearWithCloseInUseIndex = events.FindIndex(p => p is ConnectionPoolClearedEvent cpc && cpc.CloseInUseConnections);
                    if (clearWithCloseInUseIndex != -1)
                    {
                        var connectionClosedEventIndex = events.FindIndex(startIndex: clearWithCloseInUseIndex, p => p is ConnectionClosedEvent);
                        if (connectionClosedEventIndex != -1)
                        {
                            var connectionClosedEvent = (ConnectionClosedEvent)events[connectionClosedEventIndex];
                            var relatedCheckedInEventIndex = events.FindIndex(startIndex: connectionClosedEventIndex, p => p is ConnectionPoolCheckedInConnectionEvent checkedInEvent && checkedInEvent.ConnectionId == connectionClosedEvent.ConnectionId);
                            if (relatedCheckedInEventIndex != -1)
                            {
                                (events[relatedCheckedInEventIndex], events[connectionClosedEventIndex]) = (events[connectionClosedEventIndex], events[relatedCheckedInEventIndex]);
                            }
                        }
                    }
                }
            }
        }

        private static readonly HashSet<string> __ignoredSdamLogsMessages = new HashSet<string>(
          new[]
          {
               "Added server",
               "Adding server",
               "Removed server",
               "Removing server",
               "Started server monitoring",
               "Stopping server monitoring",
               "Started topology monitoring",
               "Stopping topology monitoring"
          });

        private static string __sdamCategoryName = LogCategoryHelper.GetCategoryName<LogCategories.SDAM>();

        private static bool IsSdamLogValid(LogEntry logEntry) =>
            logEntry.Category != __sdamCategoryName ||
            !__ignoredSdamLogsMessages.Contains(logEntry.GetParameter<string>(StructuredLogTemplateProviders.Message));

        #endregion
    }
}
