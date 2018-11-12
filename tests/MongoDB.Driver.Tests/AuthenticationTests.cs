/* Copyright 2018–present MongoDB Inc.
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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
namespace MongoDB.Driver.Tests
{
    /// <summary>
    /// Authentication integration tests.
    /// </summary>
    public class AuthenticationTests
    {
        [SkippableFact]
        public void Authentication_fails_when_user_has_Scram_Sha_1_mechanism_and_mechanism_is_Scram_Sha_256()
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            // mechanisms field in createUser command requires server >=4.0
            var client = DriverTestConfiguration.Client;
            var userName = $"sha1{Guid.NewGuid()}";
            var password = "sha1";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-1");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: "SCRAM-SHA-256", source: null, username: userName, password: password);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            
            AssertAuthenticationFails(settings);
        }
        
        [SkippableFact]
        public void Authentication_fails_when_user_has_Scram_Sha_256_mechanism_and_mechanism_is_Scram_Sha_1()
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var userName = $"sha256{Guid.NewGuid()}";
            var password = "sha256";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: "SCRAM-SHA-1", source: null, username: userName, password: password);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            AssertAuthenticationFails(settings);
        }
        
        [SkippableFact]
        public void Authentication_fails_when_user_is_non_extant_and_mechanism_is_not_specified()
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var userName = $"cipher{Guid.NewGuid()}";
            var password = "bluepill";
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: null, source: null, username: userName, password: password);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            AssertAuthenticationFails(settings);
        }
            
        [SkippableFact]
        public void Authentication_succeeds_when_user_has_both_Scram_Sha_mechanisms_and_mechanism_is_not_specified()
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var userName = $"both{Guid.NewGuid()}";
            var password = "both";

            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-256", "SCRAM-SHA-1");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: null, source: null, username: userName, password: password);
            
            AssertAuthenticationSucceeds(settings);
        }
        
        [SkippableFact]
        public void Authentication_succeeds_when_user_has_both_scram_sha_mechanisms_and_mechanism_is_Scram_Sha_256()
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            
            var client = DriverTestConfiguration.Client;
            var userName = $"both{Guid.NewGuid()}";
            var password = "both";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-256", "SCRAM-SHA-1");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: "SCRAM-SHA-256", source: null, username: userName, password: password);
            
            AssertAuthenticationSucceeds(settings);
        }
        
        [SkippableFact]
        public void Authentication_succeeds_when_user_has_Scram_Sha_1_Mechanism_and_mechanism_is_not_specified()
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            // mechanisms field in createUser command requires server >=4.0
            var client = DriverTestConfiguration.Client;
            
            var userName = $"sha1{Guid.NewGuid()}";
            var password = "sha1";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-1");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: null, source: null, username: userName, password: password);

            AssertAuthenticationSucceeds(settings);
        }
        
        [SkippableFact]
        public void Authentication_succeeds_when_user_has_Scram_Sha_256_mechanism_and_mechanism_is_not_specified()
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
                        
            var userName = $"sha256{Guid.NewGuid()}";
            var password = "sha256";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: null, source: null, username: userName, password: password);

            AssertAuthenticationSucceeds(settings);
        }
        
        [SkippableFact]
        public void Authentication_succeeds_when_user_has_Scram_Sha_1_mechanism_and_mechanism_is_Scram_Sha_1()
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            
            // mechanisms field in createUser command requires server >=4.0
            var client = DriverTestConfiguration.Client;
            var userName = $"sha1{Guid.NewGuid()}";
            var password = "sha1";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-1");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: "SCRAM-SHA-1", source: null, username: userName, password: password);

            AssertAuthenticationSucceeds(settings);
        }
        
        [SkippableFact]
        public void Authentication_succeeds_when_user_has_Scram_Sha_256_mechanism_and_mechanism_is_Scram_Sha_256()
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var userName = $"sha256{Guid.NewGuid()}";
            var password = "sha256";
            CreateAdminDatabaseReadWriteUser(client, userName, password, "SCRAM-SHA-256");            
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential
                .FromComponents(mechanism: "SCRAM-SHA-256", source: null, username: userName, password: password);

            AssertAuthenticationSucceeds(settings);
        }
        
        [SkippableFact]
        public void Authentication_succeeds_when_user_has_multiple_credentials_and_mechanism_is_not_specified()
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var source1 = "nyc-matrix";
            var userName1 = $"ThomasAnderson{Guid.NewGuid()}";
            var password1 = "WhatIsTheMatrix";
            var source2 = "admin";
            var userName2 = $"Neo{Guid.NewGuid()}";
            var password2 = "TrinityAndZionForever";
            CreateDatabaseUser(client, source1, userName1, password1, role: "read", mechanisms: "SCRAM-SHA-256");
            CreateDatabaseUser(client, source2, userName2, password2, role: "root", mechanisms: "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
#pragma warning disable 618
            settings.Credentials = new[]
            {
#pragma warning restore 618
                MongoCredential.FromComponents(mechanism: null, source: source1, username: userName1, password: password1),
                MongoCredential.FromComponents(mechanism: null, source: source2, username: userName2, password: password2)
            };

            AssertAuthenticationSucceeds(settings);
        }
        
        [SkippableTheory]
        [ParameterAttributeData]
        [InlineData("IX", "IX", "\u2168", "\u2163")] // "IX", "IX", Roman numeral nine, Roman numeral four
        public void Authentication_succeeds_with_Ascii_username_and_Ascii_password_when_SaslPrep_equivalent_username_exists(
            string asciiUsername, 
            string asciiPassword, 
            string unicodeUsername, 
            string unicodePassword)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var uniqueAsciiUserName = $"{asciiUsername}{Guid.NewGuid()}";
            var uniqueUnicodeUserName = $"{unicodeUsername}{Guid.NewGuid()}";
            CreateAdminDatabaseUser(client, uniqueAsciiUserName, asciiPassword, "root", "SCRAM-SHA-256");
            CreateAdminDatabaseUser(client, uniqueUnicodeUserName, unicodePassword, "root", "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(
                mechanism: "SCRAM-SHA-256", source: null, username: uniqueAsciiUserName, password: asciiPassword);
            
            AssertAuthenticationSucceeds(settings);
        }
        
        // Currently, we only support SaslPrep in .NET Framework due to a lack of a string normalization function in
        // .NET Standard
#if NET452
        [SkippableTheory]
        [ParameterAttributeData]
        [InlineData("IX", "IX", "I\u00ADX", "\u2168", "\u2163")] // "IX", "IX", "I-X", Roman numeral nine, Roman numeral four
        public void Authentication_succeeds_with_Ascii_username_and_nonSaslPrepped_password_when_SaslPrep_equivalent_username_exists(
            string asciiUsername, 
            string asciiPassword,
            string nonSaslPreppedPassword,
            string unicodeUsername, 
            string unicodePassword)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var uniqueAsciiUserName = $"{asciiUsername}{Guid.NewGuid()}";
            var uniqueUnicodeUserName = $"{unicodeUsername}{Guid.NewGuid()}";
            CreateAdminDatabaseUser(client, uniqueAsciiUserName, asciiPassword, "root", "SCRAM-SHA-256");
            CreateAdminDatabaseUser(client, uniqueUnicodeUserName, unicodePassword, "root", "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(
                mechanism: "SCRAM-SHA-256", source: null, username: uniqueAsciiUserName, password: nonSaslPreppedPassword);
            
            AssertAuthenticationSucceeds(settings);
        }
        
        [SkippableTheory]
        [ParameterAttributeData]
        [InlineData("IX", "IX", "\u2168", "\u2163", "I\u00ADV")] // "IX", "IX", Roman numeral nine, Roman numeral four, I-V
        public void Authentication_succeeds_with_Unicode_username_and_nonSaslPrepped_password_when_SaslPrep_equivalent_username_exists(
            string asciiUsername, 
            string asciiPassword,
            string unicodeUsername, 
            string unicodePassword,
            string nonSaslPreppedPassword)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var uniqueAsciiUserName = $"{asciiUsername}{Guid.NewGuid()}";
            var uniqueUnicodeUserName = $"{unicodeUsername}{Guid.NewGuid()}";
            CreateAdminDatabaseUser(client, uniqueAsciiUserName, asciiPassword, "root", "SCRAM-SHA-256");
            CreateAdminDatabaseUser(client, uniqueUnicodeUserName, unicodePassword, "root", "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(
                mechanism: "SCRAM-SHA-256", source: null, username: uniqueUnicodeUserName, password: nonSaslPreppedPassword);
            
            AssertAuthenticationSucceeds(settings);
        }
        
        [SkippableTheory]
        [ParameterAttributeData]
        [InlineData("IX", "IX", "\u2168", "\u2163")] // "IX", "IX", Roman numeral nine, Roman numeral four
        public void Authentication_succeeds_with_Unicode_username_and_Unicode_password_when_SaslPrep_equivalent_username_exists(
            string asciiUsername, 
            string asciiPassword, 
            string unicodeUsername, 
            string unicodePassword)
        {
            RequireServer.Check().Supports(Feature.ScramSha256Authentication).Authentication(true);
            var client = DriverTestConfiguration.Client;
            var uniqueAsciiUserName = $"{asciiUsername}{Guid.NewGuid()}";
            var uniqueUnicodeUserName = $"{unicodeUsername}{Guid.NewGuid()}";
            CreateAdminDatabaseUser(client, uniqueAsciiUserName, asciiPassword, "root", "SCRAM-SHA-256");
            CreateAdminDatabaseUser(client, uniqueUnicodeUserName, unicodePassword, "root", "SCRAM-SHA-256");
            var settings = client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(
                mechanism: "SCRAM-SHA-256", source: null, username: uniqueUnicodeUserName, password: unicodePassword);
            
            AssertAuthenticationSucceeds(settings);
        }
#endif
        
        private void AssertAuthenticationSucceeds(MongoClientSettings settings)
        {
             new MongoClient(settings).ListDatabaseNames().ToEnumerable().ToList();
        }
	    
        private void AssertAuthenticationFails(MongoClientSettings settings)
        {
            var exception = Record.Exception(()=>new MongoClient(settings).ListDatabaseNames().ToEnumerable().ToList());

            exception.Should().BeOfType<MongoAuthenticationException>();
        }
	    
        private BsonDocument CreateAdminDatabaseReadWriteUser(
            MongoClient client, 
            string userName, 
            string password, 
            params string[] mechanisms)
        {
            return CreateAdminDatabaseUser(client, userName, password, "readWriteAnyDatabase", mechanisms);
        }

        private BsonDocument CreateAdminDatabaseUser(
            MongoClient client,
            string userName,
            string password,
            string role,
            params string[] mechanisms)
        {
            return CreateDatabaseUser(client, "admin", userName, password, "readWriteAnyDatabase", mechanisms);
        }
        
        private BsonDocument CreateDatabaseUser(
            MongoClient client,
            string source,
            string userName, 
            string password, 
            string role, 
            params string[] mechanisms)
        {
            var createUserCommand =
                $"{{createUser: '{userName}', pwd: '{password}'," 
                + $"roles: ['{role}'], mechanisms: {mechanisms.ToJson()}}}";
            return client.GetDatabase(source).RunCommand<BsonDocument>(createUserCommand);
        }
        
    }
}
