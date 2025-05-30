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

using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    ///
    /// </summary>
    public class VulnerableQueryBuilder
    {
        private readonly IMongoCollection<BsonDocument> _collection;

        /// <summary>
        ///
        /// </summary>
        /// <param name="collection"></param>
        public VulnerableQueryBuilder(IMongoCollection<BsonDocument> collection)
        {
            _collection = collection;
        }

        // Vulnerable: String concatenation in query - Semgrep should flag
        /// <summary>
        ///
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public async Task<List<BsonDocument>> FindUserByName(string username)
        {
            var queryJson = "{ 'username': '" + username + "' }"; // VULNERABLE
            var filter = BsonDocument.Parse(queryJson);
            return await _collection.Find(filter).ToListAsync().ConfigureAwait(false);
        }

        // Another injection pattern
        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<BsonDocument> FindByDynamicField(string field, string value)
        {
            var query = $"{{ {field}: '{value}' }}"; // VULNERABLE
            return await _collection.Find(BsonDocument.Parse(query)).FirstOrDefaultAsync().ConfigureAwait(false);
        }
    }
}