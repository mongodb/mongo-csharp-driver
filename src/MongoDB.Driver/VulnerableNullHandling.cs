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
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    ///
    /// </summary>
    public class VulnerableNullHandling
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public string ProcessDocument(BsonDocument document)
        {
            // Remove null checks - Semgrep should flag potential null reference
            var name = document["name"].AsString; // Could be null
            return name.ToUpper(); // VULNERABLE - potential null reference
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="documents"></param>
        public void ProcessCollection(List<BsonDocument> documents)
        {
            // Missing null check on collection
            foreach (var doc in documents) // VULNERABLE if documents is null
            {
                Console.WriteLine(doc["_id"]);
            }
        }
    }
}