/* Copyright 2010-2014 MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public interface ICrudOperationTest
    {
        bool CanExecute(ClusterDescription clusterDescription, BsonDocument arguments, out string reason);

        Task ExecuteAsync(ClusterDescription clusterDescription, IMongoDatabase database, IMongoCollection<BsonDocument> collection, BsonDocument arguments, BsonDocument outcome);
    }

    public abstract class CrudOperationTestBase : ICrudOperationTest
    {
        protected ClusterDescription ClusterDescription { get; private set; }

        public virtual bool CanExecute(ClusterDescription clusterDescription, BsonDocument arguments, out string reason)
        {
            reason = null;
            return true;
        }

        public async Task ExecuteAsync(ClusterDescription clusterDescription, IMongoDatabase database, IMongoCollection<BsonDocument> collection, BsonDocument arguments, BsonDocument outcome)
        {
            ClusterDescription = clusterDescription;

            foreach (var argument in arguments.Elements)
            {
                if (!TrySetArgument(argument.Name, argument.Value))
                {
                    throw new NotImplementedException("The argument " + argument.Name + " has not been implemented in " + GetType());
                }
            }

            await ExecuteAsync(collection, outcome);

            if (outcome.Contains("collection"))
            {
                var collectionToVerify = collection;
                if (outcome["collection"].AsBsonDocument.Contains("name"))
                {
                    collectionToVerify = database.GetCollection<BsonDocument>(outcome["collection"]["name"].ToString());
                }
                await VerifyCollectionAsync(collectionToVerify, (BsonArray)outcome["collection"]["data"]);
            }
        }

        protected abstract bool TrySetArgument(string name, BsonValue value);

        protected abstract Task ExecuteAsync(IMongoCollection<BsonDocument> collection, BsonDocument outcome);

        protected virtual async Task VerifyCollectionAsync(IMongoCollection<BsonDocument> collection, BsonArray expectedData)
        {
            var data = await collection.Find("{}").ToListAsync();
            data.Should().BeEquivalentTo(expectedData);
        }
    }

    public abstract class CrudOperationWithResultTestBase<TResult> : CrudOperationTestBase
    {
        protected async sealed override Task ExecuteAsync(IMongoCollection<BsonDocument> collection, BsonDocument outcome)
        {
            var actualResult = await ExecuteAndGetResultAsync(collection);
            var expectedResult = ConvertExpectedResult(outcome["result"]);
            VerifyResult(actualResult, expectedResult);
        }

        protected abstract TResult ConvertExpectedResult(BsonValue expectedResult);

        protected abstract Task<TResult> ExecuteAndGetResultAsync(IMongoCollection<BsonDocument> collection);

        protected abstract void VerifyResult(TResult actualResult, TResult expectedResult);
    }

}
