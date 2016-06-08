/* Copyright 2016 MongoDB Inc.
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

namespace MongoDB.Bson.TestHelpers
{
    public static class UnicodeHelper
    {
        public static string Unescape(string value)
        {
            var index = 0;
            while ((index = value.IndexOf("\\u", index)) != -1)
            {
                var hex = value.Substring(index + 2, 4);
                var bytes = BsonUtils.ParseHexString(hex);
                var c = (char)((bytes[0] << 8) | bytes[1]);
                var s = new string(c, 1);
                value = value.Substring(0, index) + s + value.Substring(index + 6);
                index = index + 1;
            }
            return value;
        }
    }
}
