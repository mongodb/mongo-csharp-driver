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
using MongoDB.Driver.Core.Misc;
using MongoDB.TestHelpers.XunitExtensions;

namespace MongoDB.Driver.Tests.Search;

public static class AtlasSearchTestsUtils
{
    public static IMongoClient CreateAtlasSearchMongoClient()
    {
        RequireEnvironment.Check().EnvironmentVariable("ATLAS_SEARCH_TESTS_ENABLED");

        var atlasSearchUri = Environment.GetEnvironmentVariable("ATLAS_SEARCH_URI");
        Ensure.IsNotNullOrEmpty(atlasSearchUri, nameof(atlasSearchUri));

        var mongoClientSettings = MongoClientSettings.FromConnectionString(atlasSearchUri);
        mongoClientSettings.ClusterSource = DisposingClusterSource.Instance;

        return new MongoClient(mongoClientSettings);
    }
}
