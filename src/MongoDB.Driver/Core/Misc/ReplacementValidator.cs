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
using MongoDB.Bson;

namespace MongoDB.Driver.Core.Misc
{
    internal static class ReplacementValidator
    {
        // A replacement is always sent as the value of an update field ("u" for update commands, "update" for
        // findAndModify), and unlike an inserted document it is not written at the root of a document, so the BSON
        // writer will happily emit whatever type it is given. The server reads an array there as an aggregation
        // pipeline, so a loosely typed collection (for example IMongoCollection<BsonValue>) would otherwise let a
        // BsonArray of pipeline stages be passed as a "replacement" and executed as update operators. A scalar is
        // rejected for the same reason: it is not a replacement, and it only produces a confusing server error.
        public static void EnsureIsValidReplacement(object replacement, string paramName)
        {
            if (replacement is BsonValue bsonValue && bsonValue.BsonType != BsonType.Document)
            {
                var message = string.Format("A replacement must be a document, not a {0}.", bsonValue.BsonType);
                throw new ArgumentException(message, paramName);
            }
        }
    }
}
