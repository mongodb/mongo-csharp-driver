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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Authentication.External;
using Xunit;

namespace MongoDB.Driver.Tests.Communication.Security
{
    [Trait("Category", "Authentication")]
    [Trait("Category", "AwsMechanism")]
    public class AwsAuthenticationTests
    {
        [SkippableFact]
        public void Aws_authentication_should_should_have_expected_result()
        {
            RequireEnvironment.Check().EnvironmentVariable("AWS_TESTS_ENABLED");

            using (var client = DriverTestConfiguration.CreateDisposableClient())
            {
                // test that a command that doesn't require auth completes normally
                var adminDatabase = client.GetDatabase("admin");
                var pingCommand = new BsonDocument("ping", 1);
                var pingResult = adminDatabase.RunCommand<BsonDocument>(pingCommand);

                // test that a command that does require auth completes normally
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
                var count = collection.CountDocuments(FilterDefinition<BsonDocument>.Empty);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public async Task Aws_external_credentials_caching_prose_test([Values(false, true)] bool async)
        {
            RequireEnvironment
                .Check()
                .EnvironmentVariable("AWS_TESTS_ENABLED")
                .EnvironmentVariable("AWS_EC2_ENABLED");

            // 1. Clear the cache.
            SetCache(date: null);
            // 2. Create a new client.
            using var client = DriverTestConfiguration.CreateDisposableClient();
            var cache = GetCache();
            cache.Should().BeNull();

            await Find(client);
            // 3. Ensure that a find operation adds credentials to the cache..
            GetCache().Should().NotBeNull();

            // 4. Override the cached credentials with an "Expiration" that is within one minute of the current UTC time.
            SetCache(date: DateTime.UtcNow.AddMinutes(1));

            // 5. Create a new client.
            using var client2 = DriverTestConfiguration.CreateDisposableClient();
            GetCache().Should().NotBeNull();

            await Find(client2);
            // 6. Ensure that a find operation updates the credentials in the cache.
            var cache2 = GetCache();
            cache2.Should().NotBeNull().And.Should().NotBeSameAs(cache);
            // 7. Poison the cache with garbage content.
            SetCache(awsKeyId: "garbage");
            GetCache().AccessKeyId.Should().Be("garbage");
            // 8. Create a new client.
            using var client3 = DriverTestConfiguration.CreateDisposableClient();
            // 9. Ensure that a find operation results in an error.
            var exception = await Record.ExceptionAsync(() => Find(client3));
            exception.Should().NotBeNull();

            // 10. Ensure that the cache has been cleared.
            GetCache().Should().BeNull();
            // 11. Ensure that a subsequent find operation succeeds.
            await Find(client3);
            // 12. Ensure that the cache has been set.
            var cache3 = GetCache();
            cache3.Should().NotBeNull();
            // reset cache
            SetCache(date: null);

            void SetCache(DateTime? date = null, string awsKeyId = null)
            {
                var cachebleProvider = (CacheableCredentialsProvider<AwsCredentials>)ExternalCredentialsAuthenticators.Instance.Aws;

                var currentCache = cachebleProvider._cachedCredentials();
                if (date.HasValue)
                {
                    currentCache._expiration(date);
                }
                else
                {
                    if (awsKeyId == null)
                    {
                        cachebleProvider._cachedCredentials(null);
                    }
                }

                if (awsKeyId != null)
                {
                    currentCache._accessKeyId(awsKeyId);
                }
            }

            AwsCredentials GetCache() => ((CacheableCredentialsProvider<AwsCredentials>)ExternalCredentialsAuthenticators.Instance.Aws)._cachedCredentials();

            async Task Find(IMongoClient client)
            {
                var collection = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName).GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName); ;
                using var cursor = async
                    ? await collection.FindAsync(FilterDefinition<BsonDocument>.Empty)
                    : collection.FindSync(FilterDefinition<BsonDocument>.Empty);
                cursor.ToList();
            }
        }

        [SkippableFact]
        public void Ecs_should_fill_AWS_CONTAINER_CREDENTIALS_RELATIVE_URI()
        {
            var isEcs = Environment.GetEnvironmentVariable("AWS_ECS_TEST") != null;
            var awsContainerRelativeUri = Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI");
            awsContainerRelativeUri.Should().Match(v => isEcs ? v != null : v == null);
        }
    }

    internal static class CacheableCredentialsProviderReflector
    {
        public static void _cachedCredentials(this CacheableCredentialsProvider<AwsCredentials> provider, AwsCredentials credentials) => Reflector.SetFieldValue(provider, nameof(_cachedCredentials), credentials);
        public static AwsCredentials _cachedCredentials(this CacheableCredentialsProvider<AwsCredentials> provider) => (AwsCredentials)Reflector.GetFieldValue(provider, nameof(_cachedCredentials));
    }

    internal static class AwsCredentialsReflector
    {
        public static void _accessKeyId(this AwsCredentials awsCredentials, string value) => Reflector.SetFieldValue(awsCredentials, nameof(_accessKeyId), value);
        public static void _expiration(this AwsCredentials awsCredentials, DateTime? value) => Reflector.SetFieldValue(awsCredentials, nameof(_expiration), value);
    }
}
