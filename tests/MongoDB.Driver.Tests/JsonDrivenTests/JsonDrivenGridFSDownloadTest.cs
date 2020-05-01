/* Copyright 2019-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.GridFS;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenGridFSDownloadTest : JsonDrivenGridFSTest
    {
        // private fields
        private GridFSDownloadOptions _downloadOptions = new GridFSDownloadOptions();
        private FilterDefinition<BsonDocument> _filter = new BsonDocument();
        private ObjectId _id;
        private IClientSessionHandle _session;

        // public constructors
        public JsonDrivenGridFSDownloadTest(IMongoDatabase database, string bucketName, Dictionary<string, object> objectMap)
            : base(database, bucketName, objectMap)
        {
        }

        // public methods
        public override void Arrange(BsonDocument document)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(document, "name", "object", "databaseOptions", "arguments", "result", "error");
            base.Arrange(document);
        }

        // protected methods
        protected override void AssertResult()
        {
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            var bucket = new GridFSBucket(_database, _bucketOptions);
            bucket.DownloadAsBytes(_id, _downloadOptions, cancellationToken);
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            var bucket = new GridFSBucket(_database, _bucketOptions);
            await bucket.DownloadAsBytesAsync(_id, _downloadOptions, cancellationToken).ConfigureAwait(false);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "session":
                    _session = (IClientSessionHandle)_objectMap[value.AsString];
                    return;

                case "id":
                    _id = value.AsObjectId;
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
