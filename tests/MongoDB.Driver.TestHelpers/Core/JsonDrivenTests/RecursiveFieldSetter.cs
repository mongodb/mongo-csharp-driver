/* Copyright 2018-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.TestHelpers.JsonDrivenTests
{
    public static class RecursiveFieldSetter
    {
        public static void SetAll(BsonArray array, string name, BsonValue value)
        {
            SetAll(array, name, x => value);
        }

        public static void SetAll(BsonArray array, string name, Func<BsonValue, BsonValue> map)
        {
            for (var i = 0; i < array.Count; i++)
            {
                var item = array[i];
                if (item.IsBsonArray)
                {
                    SetAll(item.AsBsonArray, name, map);
                }
                else if (item.IsBsonDocument)
                {
                    SetAll(item.AsBsonDocument, name, map);
                }
            }
        }

        public static void SetAll(BsonDocument document, string name, BsonValue value)
        {
            SetAll(document, name, x => value);
        }

        public static void SetAll(BsonDocument document, string name, Func<BsonValue, BsonValue> map)
        {
            foreach (var element in document.ToList())
            {
                if (element.Name == name)
                {
                    document[name] = map(element.Value);
                }
                else if (element.Value.IsBsonArray)
                {
                    SetAll(element.Value.AsBsonArray, name, map);
                }
                else if (element.Value.IsBsonDocument)
                {
                    SetAll(element.Value.AsBsonDocument, name, map);
                }
            }
        }
    }
}
