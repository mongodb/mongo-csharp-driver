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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public abstract class CrudOperationTestBase : ICrudOperationTest
    {
        public Exception ActualException { get; set; }
        protected ClusterDescription ClusterDescription { get; private set; }

        public virtual void SkipIfNotSupported(BsonDocument arguments)
        {
        }

        public void Execute(
            ClusterDescription clusterDescription,
            IMongoDatabase database,
            IMongoCollection<BsonDocument> collection,
            BsonDocument arguments,
            BsonDocument outcome,
            bool isErrorExpected,
            bool async)
        {
            ClusterDescription = clusterDescription;

            foreach (var argument in arguments.Elements)
            {
                if (!TrySetArgument(argument.Name, argument.Value))
                {
                    throw new NotImplementedException("The argument " + argument.Name + " has not been implemented in " + GetType());
                }
            }

            try
            {
                Execute(collection, outcome, async);
            }
            catch (Exception ex) when (isErrorExpected)
            {
                ActualException = ex;
            }

            AssertOutcome(outcome, database, collection);
        }

        protected virtual void AssertOutcome(BsonDocument outcome, IMongoDatabase database, IMongoCollection<BsonDocument> collection)
        {
            if (outcome != null && outcome.Contains("collection"))
            {
                var collectionToVerify = collection;
                if (outcome["collection"].AsBsonDocument.Contains("name"))
                {
                    collectionToVerify = database.GetCollection<BsonDocument>(outcome["collection"]["name"].ToString());
                }
                VerifyCollection(collectionToVerify, (BsonArray)outcome["collection"]["data"]);
            }
        }

        protected abstract bool TrySetArgument(string name, BsonValue value);

        protected abstract void Execute(IMongoCollection<BsonDocument> collection, BsonDocument outcome, bool async);

        protected virtual void VerifyCollection(IMongoCollection<BsonDocument> collection, BsonArray expectedData)
        {
            var data = collection.FindSync("{}").ToList();
            data.Should().BeEquivalentTo(expectedData);
        }
    }

    public abstract class CrudOperationWithResultTestBase<TResult> : CrudOperationTestBase
    {
        private TResult _result;

        protected sealed override void Execute(IMongoCollection<BsonDocument> collection, BsonDocument outcome, bool async)
        {
            _result = ExecuteAndGetResult(collection, async);
        }

        protected override void AssertOutcome(BsonDocument outcome, IMongoDatabase database, IMongoCollection<BsonDocument> collection)
        {
            if (outcome != null && outcome.Contains("result"))
            {
                var expectedResult = ConvertExpectedResult(outcome["result"]);
                VerifyResult(_result, expectedResult);
            }

            base.AssertOutcome(outcome, database, collection);
        }

        protected abstract TResult ConvertExpectedResult(BsonValue expectedResult);

        protected abstract TResult ExecuteAndGetResult(IMongoCollection<BsonDocument> collection, bool async);

        protected abstract void VerifyResult(TResult actualResult, TResult expectedResult);
    }

}
