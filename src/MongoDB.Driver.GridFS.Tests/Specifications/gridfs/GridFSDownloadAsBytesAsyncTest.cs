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
    public abstract class GridFSDownloadAsyncTestBase : GridFSTestBase
    {
        // fields
        protected ObjectId _id;
        protected GridFSDownloadOptions _options = null;

        // constructors
        public GridFSDownloadAsyncTestBase(BsonDocument data, BsonDocument testDefinition)
            : base(data, testDefinition)
        {
            var operationName = testDefinition["act"]["operation"].AsString;
            if (operationName != "download")
            {
                throw new ArgumentException(string.Format("Invalid operation name: {0}.", operationName), "testDefinition");
            }
            ParseArguments(testDefinition["act"]["arguments"].AsBsonDocument.Elements);
        }

        // protected methods
        protected Task<byte[]> InvokeMethodAsync(GridFSBucket bucket)
        {
            return bucket.DownloadAsBytesAsync(_id, _options);
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

                    case "options":
                        ParseOptions((BsonDocument)argument.Value);
                        break;

                    default:
                        throw new ArgumentException(string.Format("Invalid argument name: {0}.", argument.Name));
                }
            }
        }

        private void ParseOptions(BsonDocument options)
        {
            foreach (var option in options.Elements)
            {
                _options = _options ?? new GridFSDownloadOptions();
                switch (option.Name)
                {
                    case "checkMD5":
                        _options.CheckMD5 = option.Value.ToBoolean();
                        break;

                    default:
                        throw new ArgumentException(string.Format("Invalid option name: {0}.", option.Name));
                }
            }
        }
    }

    public class GridFSDownloadAsBytesAsyncTest : GridFSDownloadAsyncTestBase
    {
        // fields
        private readonly byte[] _expectedResult;
        private byte[] _result;

        // constructors
        public GridFSDownloadAsBytesAsyncTest(BsonDocument data, BsonDocument testDefinition)
            : base(data, testDefinition)
        {
            _expectedResult = testDefinition["assert"]["result"].AsByteArray;
        }

        // protected methods
        protected override async Task ActAsync(GridFSBucket bucket)
        {
            _result = await InvokeMethodAsync(bucket);
        }

        protected override Task AssertAsync(GridFSBucket bucket)
        {
            _result.Should().Equal(_expectedResult);
            return Task.FromResult(true); // don't call base.AssertAsync
        }
    }

    public class GridFSDownloadAsyncTest<TException> : GridFSDownloadAsyncTestBase where TException : Exception
    {
        // fields
        private Func<Task> _action;

        // constructors
        public GridFSDownloadAsyncTest(BsonDocument data, BsonDocument testDefinition)
            : base(data, testDefinition)
        {
        }

        // protected methods
        protected override Task ActAsync(GridFSBucket bucket)
        {
            _action = () => InvokeMethodAsync(bucket);
            return Task.FromResult(true);
        }

        protected override Task AssertAsync(GridFSBucket bucket)
        {
            _action.ShouldThrow<TException>();
            return Task.FromResult(true); // don't call base.AssertAsync
        }
    }
}
