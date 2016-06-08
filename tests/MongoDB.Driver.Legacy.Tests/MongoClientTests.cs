/* Copyright 2010-2015 MongoDB Inc.
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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoClientTests
    {
        [Fact]
        public void UsesSameMongoServerForIdenticalSettings()
        {
            var client1 = new MongoClient("mongodb://localhost");
#pragma warning disable 618
            var server1 = client1.GetServer();
#pragma warning restore

            var client2 = new MongoClient("mongodb://localhost");
#pragma warning disable 618
            var server2 = client2.GetServer();
#pragma warning restore

            Assert.Same(server1, server2);
        }

        [Fact]
        public void UsesSameMongoServerWhenReadPreferenceTagsAreTheSame()
        {
            var client1 = new MongoClient("mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny");
#pragma warning disable 618
            var server1 = client1.GetServer();
#pragma warning restore

            var client2 = new MongoClient("mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny");
#pragma warning disable 618
            var server2 = client2.GetServer();
#pragma warning restore

            Assert.Same(server1, server2);
        }
    }
}
