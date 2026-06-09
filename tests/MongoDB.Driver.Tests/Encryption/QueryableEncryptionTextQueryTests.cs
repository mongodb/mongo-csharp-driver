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
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using MongoDB.Driver.Tests.Specifications.client_side_encryption;
using MongoDB.Driver.Tests.Specifications.client_side_encryption.prose_tests;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Encryption;

[Trait("Category", "CSFLE")]
[Trait("Category", "Integration")]
public class QueryableEncryptionTextQueryTests : LoggableTestClass
{
    private static readonly CollectionNamespace __keyVaultCollectionNamespace = CollectionNamespace.FromFullName("keyvault.datakeys");
    private static readonly CollectionNamespace __prefixSuffixCollectionNamespace = CollectionNamespace.FromFullName("db.prefix-suffix");
    private static readonly CollectionNamespace __substringCollectionNamespace = CollectionNamespace.FromFullName("db.substring");

    public QueryableEncryptionTextQueryTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Theory]
    [ParameterAttributeData]
    public void EncStr_filter_builder_should_match_encrypted_text([Values(false, true)] bool async)
    {
        RequireTextPreviewSupport();

        using var encryptedClient = SetupEncryptedCollections();
        var prefixSuffix = GetCollection(encryptedClient, __prefixSuffixCollectionNamespace);
        var substring = GetCollection(encryptedClient, __substringCollectionNamespace);

        Find(prefixSuffix, Builders<TextDocument>.Filter.EncStrStartsWith(x => x.EncryptedText, "foo"), async)
            .Single().EncryptedText.Should().Be("foobarbaz");
        Find(prefixSuffix, Builders<TextDocument>.Filter.EncStrEndsWith(x => x.EncryptedText, "baz"), async)
            .Single().EncryptedText.Should().Be("foobarbaz");
        Find(substring, Builders<TextDocument>.Filter.EncStrContains(x => x.EncryptedText, "bar"), async)
            .Single().EncryptedText.Should().Be("foobarbaz");

        Find(prefixSuffix, Builders<TextDocument>.Filter.EncStrStartsWith(x => x.EncryptedText, "baz"), async)
            .Should().BeEmpty();
    }

    [Theory]
    [ParameterAttributeData]
    public void Mql_EncStr_in_LINQ_should_match_encrypted_text([Values(false, true)] bool async)
    {
        RequireTextPreviewSupport();

        using var encryptedClient = SetupEncryptedCollections();
        var prefixSuffix = GetCollection(encryptedClient, __prefixSuffixCollectionNamespace);
        var substring = GetCollection(encryptedClient, __substringCollectionNamespace);

        Where(prefixSuffix, x => Mql.EncStrStartsWith(x.EncryptedText, "foo"), async)
            .Single().EncryptedText.Should().Be("foobarbaz");
        Where(prefixSuffix, x => Mql.EncStrEndsWith(x.EncryptedText, "baz"), async)
            .Single().EncryptedText.Should().Be("foobarbaz");
        Where(substring, x => Mql.EncStrContains(x.EncryptedText, "bar"), async)
            .Single().EncryptedText.Should().Be("foobarbaz");

        Where(prefixSuffix, x => Mql.EncStrStartsWith(x.EncryptedText, "baz"), async)
            .Should().BeEmpty();
    }

    private static void RequireTextPreviewSupport()
    {
        RequireServer.Check()
            .Supports(Feature.Csfle2QEv2TextPreviewAlgorithm)
            .ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded, ClusterType.LoadBalanced)
            .VersionLessThanOrEqualTo("8.99.99"); // the encryptedFields fixtures use the deprecated prefixPreview/suffixPreview/substringPreview query types, which 9.0 rejects (SERVER-123416)

        // QE text queries require crypt_shared; skip when only mongocryptd is available (see SERVER-106469).
        CoreTestConfiguration.SkipMongocryptdTests_SERVER_106469(checkForSharedLib: true);
    }

    // Creates the prefix/suffix and substring QE collections, populates the key vault, inserts a document into
    // each via automatic encryption, and returns the auto-encrypting client (which the caller disposes).
    private IMongoClient SetupEncryptedCollections()
    {
        // Reuse the encryptedFields schemas and data key embedded for the spec prose tests, for simplicity.
        var jsonReader = ClientEncryptionProseTests.JsonFileReader.Instance;
        var prefixSuffixFields = jsonReader.Documents["etc.data.encryptedFields-prefix-suffix.json"];
        var substringFields = jsonReader.Documents["etc.data.encryptedFields-substring.json"];
        var key1Document = jsonReader.Documents["etc.data.keys.key1-document.json"];

        using (var keyVaultClient = CreateClient())
        {
            CreateEncryptedCollection(keyVaultClient, __prefixSuffixCollectionNamespace, prefixSuffixFields);
            CreateEncryptedCollection(keyVaultClient, __substringCollectionNamespace, substringFields);

            // Majority write concern so the data key is committed before the encrypted client reads it (read concern majority).
            var keyVaultDatabase = keyVaultClient.GetDatabase(
                __keyVaultCollectionNamespace.DatabaseNamespace.DatabaseName,
                new MongoDatabaseSettings { WriteConcern = WriteConcern.WMajority });
            keyVaultDatabase.DropCollection(__keyVaultCollectionNamespace.CollectionName);
            keyVaultDatabase
                .GetCollection<BsonDocument>(__keyVaultCollectionNamespace.CollectionName)
                .InsertOne(key1Document);
        }

        var encryptedClient = CreateClient(withAutoEncryption: true);
        GetCollection(encryptedClient, __prefixSuffixCollectionNamespace).InsertOne(new TextDocument { Id = 0, EncryptedText = "foobarbaz" });
        GetCollection(encryptedClient, __substringCollectionNamespace).InsertOne(new TextDocument { Id = 0, EncryptedText = "foobarbaz" });
        return encryptedClient;
    }

    private static void CreateEncryptedCollection(IMongoClient client, CollectionNamespace collectionNamespace, BsonDocument encryptedFields)
    {
        var database = client.GetDatabase(
            collectionNamespace.DatabaseNamespace.DatabaseName,
            new MongoDatabaseSettings { WriteConcern = WriteConcern.WMajority });
        database.DropCollection(collectionNamespace.CollectionName, new DropCollectionOptions { EncryptedFields = encryptedFields });
        database.CreateCollection(collectionNamespace.CollectionName, new CreateCollectionOptions { EncryptedFields = encryptedFields });
    }

    private IMongoClient CreateClient(bool withAutoEncryption = false)
    {
        var settings = DriverTestConfiguration.GetClientSettings();
        var configurator = settings.ClusterConfigurator;
        settings.ClusterConfigurator = b => configurator?.Invoke(b);
        settings.ClusterSource = DisposingClusterSource.Instance;

        if (withAutoEncryption)
        {
            var extraOptions = new Dictionary<string, object>();
            EncryptionTestHelper.ConfigureDefaultExtraOptions(extraOptions);

            // No schemaMap/encryptedFieldsMap: the QE collection's encryptedFields metadata is fetched from the server.
            settings.AutoEncryptionOptions = new AutoEncryptionOptions(
                keyVaultNamespace: __keyVaultCollectionNamespace,
                kmsProviders: EncryptionTestHelper.GetKmsProviders("local"),
                extraOptions: extraOptions);
        }

        return new MongoClient(settings);
    }

    private static IMongoCollection<TextDocument> GetCollection(IMongoClient client, CollectionNamespace collectionNamespace) =>
        client
            .GetDatabase(collectionNamespace.DatabaseNamespace.DatabaseName)
            .GetCollection<TextDocument>(collectionNamespace.CollectionName);

    private static List<TextDocument> Find(IMongoCollection<TextDocument> collection, FilterDefinition<TextDocument> filter, bool async) =>
        async
            ? collection.FindAsync(filter).GetAwaiter().GetResult().ToList()
            : collection.Find(filter).ToList();

    private static List<TextDocument> Where(IMongoCollection<TextDocument> collection, Expression<Func<TextDocument, bool>> predicate, bool async)
    {
        var queryable = collection.AsQueryable().Where(predicate);
        return async
            ? queryable.ToListAsync().GetAwaiter().GetResult()
            : queryable.ToList();
    }

    [BsonIgnoreExtraElements]
    public class TextDocument
    {
        [BsonId]
        public int Id { get; set; }

        [BsonElement("encryptedText")]
        public string EncryptedText { get; set; }
    }
}
