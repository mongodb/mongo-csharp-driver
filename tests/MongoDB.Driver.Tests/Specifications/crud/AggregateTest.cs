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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class AggregateTest : CrudOperationWithResultTestBase<List<BsonDocument>>
    {
        private List<BsonDocument> _stages;
        private AggregateOptions _options = new AggregateOptions();

        public override bool CanExecute(ClusterDescription clusterDescription, BsonDocument arguments, out string reason)
        {
            var version = clusterDescription.Servers[0].Version;
            reason = string.Format("Server must be at least 2.6.0. Current server is {0}.", version);
            return !(version < new SemanticVersion(2, 6, 0) &&
                ((BsonArray)arguments["pipeline"])
                    .Cast<BsonDocument>()
                    .Last()
                    .Contains("$out"));
        }

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "pipeline":
                    _stages = ((BsonArray)value).Cast<BsonDocument>().ToList();
                    return true;
                case "batchSize":
                    _options.BatchSize = (int)value;
                    return true;
                case "collation":
                    _options.Collation = Collation.FromBsonDocument(value.AsBsonDocument);
                    return true;
            }

            return false;
        }

        protected override List<BsonDocument> ConvertExpectedResult(BsonValue expectedResult)
        {
            return ((BsonArray)expectedResult).Select(x => (BsonDocument)x).ToList();
        }

        protected override List<BsonDocument> ExecuteAndGetResult(IMongoCollection<BsonDocument> collection, bool async)
        {
            if (async)
            {
                var cursor = collection.AggregateAsync<BsonDocument>(_stages, _options).GetAwaiter().GetResult();
                return cursor.ToListAsync().GetAwaiter().GetResult();
            }
            else
            {
                return collection.Aggregate<BsonDocument>(_stages, _options).ToList();
            }
        }

        protected override void VerifyResult(List<BsonDocument> actualResult, List<BsonDocument> expectedResult)
        {
            actualResult.Should().Equal(expectedResult);
        }
    }
}
