/* Copyright 2020–present MongoDB Inc.
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
using MongoDB.Driver.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests
{
    public class OcspIntegrationTests
    {
        private static readonly string _shouldSucceedEnvironmentVariableName = "OCSP_TLS_SHOULD_SUCCEED";

        /*
         * This test must be run as its own task in Evergreen.
         * When testing locally, this test should not run alongside other integration tests.
         * Clearing local OCSP caches between test runs is also required when testing locally.
         * Clearing the OCSP caches may involve clearing out the caches in ~\.dotnet folder
         * and $DOTNET_CLI_HOME/.dotnet, as well as running `dotnet clean` prior to running the tests.
         * See spec for OCSP OS-level cache manipulation commands.
         *   https://github.com/mongodb/specifications/blob/master/source/ocsp-support/ocsp-support.rst#os-level-ocsp-cache-manipulation
         * When testing on Windows, the certificate should be added to the trust store prior to each run in order to
         * reduce the chances of Windows pruning the certificate from the trust store prior to the test running.
         */
        [SkippableFact]
        public void MongoClientShouldRespectCertificateStatusAndTlsInsecure()
        {
            /* We cannot call RequireServer.Check() because this would result in a connection being made to the mongod
             * and that connection will not succeed if we're testing the revoked certificate case. */

            RequireEnvironment.Check().EnvironmentVariable(_shouldSucceedEnvironmentVariableName);

            var shouldSucceed = GetShouldSucceed();
            /* To prevent OCSP caching from polluting the test results, we MUST run the "secure" version before
             * the tlsInsecure version.  */
            var secureClientException = Record.Exception(() => Ping(tlsInsecure: false));
            var tlsInsecureClientException = Record.Exception(() => Ping(tlsInsecure: true));

            tlsInsecureClientException.Should().BeNull();
            if (shouldSucceed)
            {
                secureClientException.Should().BeNull();
            }
            else
            {
                secureClientException.Should().BeOfType<TimeoutException>();
                var message = secureClientException.Message;
                // The exception will lack this message if the heartbeat doesn't fire
                message.Should().Contain("The remote certificate is invalid according to the validation procedure.");
            }

            void Ping(bool tlsInsecure)
            {
                using (var client = CreateDisposableMongoClient(tlsInsecure))
                {
                    client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                    if (client.Settings.SdamLogFilename != null)
                    { // Log file needs a bit of time to be written before we dispose the client
                        System.Threading.Thread.Sleep(2000);
                    }
                }
            }
        }

        // private methods
        /* We can't use DriverTestConfiguration.CreateDisposableClient because that results in a call to check
         * the cluster type, which fails because a connection cannot be established to the mongod when testing
         * the revoked certificate case */
        private DisposableMongoClient CreateDisposableMongoClient(bool tlsInsecure)
        {
            var settings = DriverTestConfiguration.GetClientSettings().Clone();
            settings.SslSettings = new SslSettings { CheckCertificateRevocation = true };
            // setting AllowInsecureTls= true will automatically set CheckCertificateRevocation to false
            settings.AllowInsecureTls = tlsInsecure;
            /* We want the heartbeat to fire so that we can get the HeartBeat exception in the cluster description
             * in the exception that will be thrown when testing invalid certificates */
            settings.HeartbeatInterval = TimeSpan.FromMilliseconds(500);
            /* We lower the server selection timeout to speed up tests, but choosing a value <=5s may
             * result in the driver being unable to perform OCSP endpoint checking in time, causing a
             * ServerSelectionTimeout that does include a certificate revocation status error message. */
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5 * 2); // must be > 5s
            // settings.SdamLogFilename = @"C:\temp\sdam" + $"{tlsInsecure}.log";

            return new DisposableMongoClient(new MongoClient(settings));
        }

        private bool GetShouldSucceed()
        {
            var ocspOutcomeEnvironmentVariableValue
                = Environment.GetEnvironmentVariable(_shouldSucceedEnvironmentVariableName);
            if (!Boolean.TryParse(ocspOutcomeEnvironmentVariableValue, out var successExpected))
            {
                throw new Exception(
                    $"Invalid value of {ocspOutcomeEnvironmentVariableValue} in {_shouldSucceedEnvironmentVariableName}."
                    + $" Expected true/false.");
            }
            return successExpected;
        }
    }
}
