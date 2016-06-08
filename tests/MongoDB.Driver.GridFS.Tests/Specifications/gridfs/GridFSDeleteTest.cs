/* Copyright 2015 MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.GridFS.Tests.Specifications.gridfs
{
    public abstract class GridFSDeleteTestBase : GridFSTestBase
    {
        // fields
        protected ObjectId _id;

        // constructors
        public GridFSDeleteTestBase(BsonDocument data, BsonDocument testDefinition)
            : base(data, testDefinition)
        {
            var operationName = testDefinition["act"]["operation"].AsString;
            if (operationName != "delete")
            {
                throw new ArgumentException(string.Format("Invalid operation name: {0}.", operationName), "testDefinition");
            }
            ParseArguments(testDefinition["act"]["arguments"].AsBsonDocument.Elements);
        }

        // protected methods
        protected void InvokeMethod(GridFSBucket bucket)
        {
            bucket.Delete(_id);
        }

        protected Task InvokeMethodAsync(GridFSBucket bucket)
        {
            return bucket.DeleteAsync(_id);
        }

        // private methods
        private void ParseArguments(IEnumerable<BsonElement> arguments)
        {
            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "id":
                        _id = argument.Value.AsObjectId;
                        break;

                    default:
                        throw new ArgumentException(string.Format("Invalid argument name: {0}.", argument.Name));
                }
            }
        }
    }

    public class GridFSDeleteTest : GridFSDeleteTestBase
    {
        // constructors
        public GridFSDeleteTest(BsonDocument data, BsonDocument testDefinition)
            : base(data, testDefinition)
        {
        }

        // protected methods
        protected override void Act(GridFSBucket bucket, bool async)
        {
            if (async)
            {
                InvokeMethodAsync(bucket).GetAwaiter().GetResult();
            }
            else
            {
                InvokeMethod(bucket);
            }
        }
    }

    public class GridFSDeleteTest<TException> : GridFSDeleteTestBase where TException : Exception
    {
        // fields
        private Action _action;

        // constructors
        public GridFSDeleteTest(BsonDocument data, BsonDocument testDefinition)
            : base(data, testDefinition)
        {
        }

        // protected methods
        protected override void Act(GridFSBucket bucket, bool async)
        {
            if (async)
            {
                _action = () => InvokeMethodAsync(bucket).GetAwaiter().GetResult();
            }
            else
            {
                _action = () => InvokeMethod(bucket);
            }
        }

        protected override void Assert(GridFSBucket bucket)
        {
            _action.ShouldThrow<TException>();
            base.Assert(bucket);
        }
    }
}
