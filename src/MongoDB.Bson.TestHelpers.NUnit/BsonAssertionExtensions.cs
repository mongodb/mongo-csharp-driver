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

using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;

namespace FluentAssertions // use FluentAssertions namespace so that these extension methods are automatically available
{
    public static class BsonAssertionExtensions
    {
        // static methods
        public static BsonArrayAssertions Should(this BsonArray actualValue)
        {
            return new BsonArrayAssertions(actualValue);
        }

        public static BsonDocumentAssertions Should(this BsonDocument actualValue)
        {
            return new BsonDocumentAssertions(actualValue);
        }

        public static BsonValueAssertions Should(this BsonValue actualValue)
        {
            return new BsonValueAssertions(actualValue);
        }
    }
}
