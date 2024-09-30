﻿/* Copyright 2010-present MongoDB Inc.
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

using System.Collections.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp170
{
    public class CSharp170Tests
    {
        public class C
        {
            public Collection<int> Collection;
            public ObservableCollection<int> Observable;
        }

        [Fact]
        public void TestDeserializeDouble()
        {
            var obj = new C
            {
                Collection = new Collection<int> { 1, 2, 3 },
                Observable = new ObservableCollection<int> { 1, 2, 3 }
            };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'Collection' : [1, 2, 3], 'Observable' : [1, 2, 3] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.Equal(3, rehydrated.Collection.Count);
            Assert.Equal(1, rehydrated.Collection[0]);
            Assert.Equal(2, rehydrated.Collection[1]);
            Assert.Equal(3, rehydrated.Collection[2]);
            Assert.Equal(3, rehydrated.Observable.Count);
            Assert.Equal(1, rehydrated.Observable[0]);
            Assert.Equal(2, rehydrated.Observable[1]);
            Assert.Equal(3, rehydrated.Observable[2]);
        }
    }
}
