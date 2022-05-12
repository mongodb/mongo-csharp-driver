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

using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for dropping a collection.
    /// </summary>
    public class DropCollectionOptions
    {
        private BsonDocument _encryptedFields;

        /// <summary>
        /// Gets or sets encrypted fields.
        /// </summary>
        public BsonDocument EncryptedFields
        {
            get { return _encryptedFields; }
            set { _encryptedFields = value; }
        }
    }
}
