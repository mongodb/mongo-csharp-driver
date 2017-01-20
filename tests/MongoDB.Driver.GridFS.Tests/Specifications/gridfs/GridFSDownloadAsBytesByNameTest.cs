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
    public abstract class GridFSDownloadAsBytesByNameTestBase : GridFSTestBase
    {
        // fields
        protected string _filename;
        protected GridFSDownloadByNameOptions _options = null;

        // constructors
        public GridFSDownloadAsBytesByNameTestBase(BsonDocument data, BsonDocument testDefinition)
            : base(data, testDefinition)
        {
            var operationName = testDefinition["act"]["operation"].AsString;
            if (operationName != "download_by_name")
            {
                throw new ArgumentException(string.Format("Invalid operation name: {0}.", operationName), "testDefinition");
            }
            ParseArguments(testDefinition["act"]["arguments"].AsBsonDocument.Elements);
        }

        // protected methods
        protected byte[] InvokeMethod(GridFSBucket bucket)
        {
            return bucket.DownloadAsBytesByName(_filename, _options);
        }

        protected Task<byte[]> InvokeMethodAsync(GridFSBucket bucket)
        {
            return bucket.DownloadAsBytesByNameAsync(_filename, _options);
        }

        // private methods
        private void ParseArguments(IEnumerable<BsonElement> arguments)
        {
            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "filename":
                        _filename = argument.Value.AsString;
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
                _options = _options ?? new GridFSDownloadByNameOptions();
                switch (option.Name)
                {
                    case "checkMD5":
                        _options.CheckMD5 = option.Value.ToBoolean();
                        break;

                    case "revision":
                        _options.Revision = option.Value.ToInt32();
                        break;

                    default:
                        throw new ArgumentException(string.Format("Invalid option name: {0}.", option.Name));
                }
            }
        }
    }

    public class GridFSDownloadAsBytesByNameTest : GridFSDownloadAsBytesByNameTestBase
    {
        // fields
        private readonly byte[] _expectedResult;
        private byte[] _result;

        // constructors
        public GridFSDownloadAsBytesByNameTest(BsonDocument data, BsonDocument testDefinition)
            : base(data, testDefinition)
        {
            _expectedResult = testDefinition["assert"]["result"].AsByteArray;
        }

        // protected methods
        protected override void Act(GridFSBucket bucket, bool async)
        {
            if (async)
            {
                _result = InvokeMethodAsync(bucket).GetAwaiter().GetResult();
            }
            else
            {
                _result = InvokeMethod(bucket);
            }
        }

        protected override void Assert(GridFSBucket bucket)
        {
            _result.Should().Equal(_expectedResult);
            // don't call base.Assert
        }
    }

    public class GridFSDownloadAsBytesByNameTest<TException> : GridFSDownloadAsBytesByNameTestBase where TException : Exception
    {
        // fields
        private Action _action;

        // constructors
        public GridFSDownloadAsBytesByNameTest(BsonDocument data, BsonDocument testDefinition)
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
            // don't call base.Assert
        }
    }
}
