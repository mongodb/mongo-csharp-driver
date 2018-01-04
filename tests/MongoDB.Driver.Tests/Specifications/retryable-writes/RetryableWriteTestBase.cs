/* Copyright 2017-present MongoDB Inc.
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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes
{
    public abstract class RetryableWriteTestBase : IRetryableWriteTest
    {
        // protected fields
        protected Exception _exception;

        // public methods
        public virtual void Execute(IMongoCollection<BsonDocument> collection, bool async)
        {
            try
            {
                if (async)
                {
                    ExecuteAsync(collection);
                }
                else
                {
                    ExecuteSync(collection);
                }
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        public abstract void Initialize(BsonDocument operation);

        public virtual void VerifyOutcome(IMongoCollection<BsonDocument> collection, BsonDocument outcome)
        {
            VerifyFields(outcome, "error", "result", "collection");

            if (outcome.GetValue("error", false).ToBoolean())
            {
                if (_exception == null)
                {
                    throw new Exception("Was expecting an exception but none was thrown.");
                }
            }
            else
            {
                if (_exception != null)
                {
                    throw new Exception("Was not expecting an exception but one was thrown.", _exception);
                }
            }

            BsonValue result;
            if (outcome.TryGetValue("result", out result) && _exception == null)
            {
                VerifyResult(result.AsBsonDocument);
            }

            BsonValue collectionValue;
            if (outcome.TryGetValue("collection", out collectionValue))
            {
                var collectionDocument = collectionValue.AsBsonDocument;
                VerifyFields(collectionDocument, "data");
                var data = collectionDocument["data"].AsBsonArray;

                var actualContents = ReadContents(collection);
                var expectedContents = ParseExpectedContents(data);
                VerifyCollectionContents(actualContents, expectedContents);
            }
        }

        // protected methods
        protected abstract void ExecuteAsync(IMongoCollection<BsonDocument> collection);

        protected abstract void ExecuteSync(IMongoCollection<BsonDocument> collection);

        protected virtual List<BsonDocument> ParseExpectedContents(BsonArray data)
        {
            return data.Cast<BsonDocument>().ToList();
        }

        protected virtual List<BsonDocument> ReadContents(IMongoCollection<BsonDocument> collection)
        {
            return collection.FindSync("{ }").ToList();
        }

        protected virtual void VerifyCollectionContents(List<BsonDocument> actualContents, List<BsonDocument> expectedContents)
        {
            actualContents.Should().BeEquivalentTo(expectedContents);
        }

        protected void VerifyFields(BsonDocument document, params string[] expectedNames)
        {
            foreach (var name in document.Names)
            {
                if (!expectedNames.Contains(name))
                {
                    throw new FormatException($"Unexpected field: {name}.");
                }
            }
        }

        protected virtual void VerifyResult(BsonDocument result)
        {
            throw new NotImplementedException();
        }
    }
}
