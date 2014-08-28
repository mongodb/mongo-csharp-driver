/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents the base class for an operation that will be sending a query message to the server.
    /// </summary>
    public abstract class QueryOperationBase
    {
        // methods
        protected BsonDocument CreateReadPreferenceDocument(ReadPreference readPreference)
        {
            if (readPreference == null)
            {
                return null;
            }

            BsonArray tagSets = null;
            if (readPreference.TagSets != null)
            {
                tagSets = new BsonArray(readPreference.TagSets.Select(ts => new BsonDocument(ts.Tags.Select(t => new BsonElement(t.Name, t.Value)))));
            }

            return new BsonDocument
            {
                { "mode", BsonUtils.ToCamelCase(readPreference.ReadPreferenceMode.ToString()) },
                { "tags", tagSets, tagSets != null }
            };
        }
    }
}
