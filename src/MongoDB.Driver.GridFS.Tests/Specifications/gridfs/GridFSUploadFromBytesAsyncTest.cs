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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.GridFS.Tests.Specifications.gridfs
{
    public abstract class GridFSPutAsyncTestBase : GridFSTestBase
    {
        // fields
        protected string _filename;
        protected GridFSUploadOptions _options = null;
        protected byte[] _source;

        // constructors
        public GridFSPutAsyncTestBase(BsonDocument data, BsonDocument testDefinition)
            : base(data, testDefinition)
        {
            var operationName = testDefinition["act"]["operation"].AsString;
            if (operationName != "upload")
            {
                throw new ArgumentException(string.Format("Invalid operation name: {0}.", operationName), "testDefinition");
            }
            ParseArguments(testDefinition["act"]["arguments"].AsBsonDocument.Elements);
        }

        // protected methods
        protected Task<ObjectId> InvokeMethodAsync(GridFSBucket bucket)
        {
            return bucket.UploadFromBytesAsync(_filename, _source, _options);
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

                    case "source":
                        _source = argument.Value.AsByteArray;
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
                _options = _options ?? new GridFSUploadOptions();
                switch (option.Name)
                {
                    case "aliases":
#pragma warning disable 618
                        _options.Aliases = option.Value.AsBsonArray.Select(v => v.AsString);
#pragma warning restore
                        break;

                    case "chunkSizeBytes":
                        _options.ChunkSizeBytes = option.Value.ToInt32();
                        break;

                    case "contentType":
#pragma warning disable 618
                        _options.ContentType = option.Value.AsString;
#pragma warning restore
                        break;

                    case "metadata":
                        _options.Metadata = option.Value.AsBsonDocument;
                        break;

                    default:
                        throw new ArgumentException(string.Format("Invalid option name: {0}.", option.Name));
                }
            }
        }
    }

    public class GridFSUploadFromBytesAsyncTest : GridFSPutAsyncTestBase
    {
        // fields
        private ObjectId _referenceObjectId;
        private ObjectId _result;
        private DateTime _startTime;

        // constructors
        public GridFSUploadFromBytesAsyncTest(BsonDocument data, BsonDocument testDefinition)
            : base(data, testDefinition)
        {
        }

        // protected methods
        protected override async Task ActAsync(GridFSBucket bucket)
        {
            _referenceObjectId = ObjectId.GenerateNewId();
            _startTime = DateTime.UtcNow;
            _result = await InvokeMethodAsync(bucket);
        }

        protected override void Assert(List<BsonDocument> filesCollectionDocuments, List<BsonDocument> chunks, List<BsonDocument> expectedFilesCollectionDocuments, List<BsonDocument> expectedChunks)
        {
            var filesCollectionDocument = filesCollectionDocuments.Single(e => e["_id"] == _result);
            var uploadDate = filesCollectionDocument["uploadDate"].ToUniversalTime();
            uploadDate.Should().BeCloseTo(_startTime, precision: 1000);

            base.Assert(filesCollectionDocuments, chunks, expectedFilesCollectionDocuments, expectedChunks);
        }

        protected override Task AssertAsync(GridFSBucket bucket)
        {
            _result.Timestamp.Should().BeInRange(_referenceObjectId.Timestamp, _referenceObjectId.Timestamp + 1);
            _result.Machine.Should().Be(_referenceObjectId.Machine);
            _result.Pid.Should().Be(_referenceObjectId.Pid);

            return base.AssertAsync(bucket);
        }

        protected override void PreprocessAssertCommands(List<BsonDocument> commands, List<BsonDocument> actualFilesCollectionDocuments, List<BsonDocument> actualChunks)
        {
            PreprocessAssertCommands(commands, actualFilesCollectionDocuments, actualChunks, _result);
        }
    }

    public class GridFSPutAsyncTest<TException> : GridFSPutAsyncTestBase where TException : Exception
    {
        // fields
        private Func<Task> _action;

        // constructors
        public GridFSPutAsyncTest(BsonDocument data, BsonDocument testDefinition)
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
            return base.AssertAsync(bucket);
        }
    }
}
