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

using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp542
{
    public class CSharp542
    {
        public class Test
        {
            public ObjectId Id;
            public Nullable<int> MyNullableInt;
        }

        [Fact]
        public void TestNullableComparison()
        {
            var server = LegacyTestConfiguration.Server;
            var database = server.GetDatabase("test");
            var collection = database.GetCollection<Test>("foos");

            var query = collection.AsQueryable().Where(p => p.MyNullableInt == 3);

            query.ToList();
        }
    }

}