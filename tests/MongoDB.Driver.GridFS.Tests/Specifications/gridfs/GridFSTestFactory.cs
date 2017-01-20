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
using MongoDB.Bson;

namespace MongoDB.Driver.GridFS.Tests.Specifications.gridfs
{
    public static class GridFSTestFactory
    {
        public static IGridFSTest CreateTest(BsonDocument data, BsonDocument testDefinition)
        {
            var operationName = testDefinition["act"]["operation"].AsString;
            switch (operationName)
            {
                case "delete":
                    return GridFSDeleteTestFactory.CreateTest(data, testDefinition);
                case "download":
                    return GridFSDownloadAsBytesTestFactory.CreateTest(data, testDefinition);
                case "download_by_name":
                    return GridFSDownloadAsBytesByNameTestFactory.CreateTest(data, testDefinition);
                case "upload":
                    return GridFSUploadFromBytesTestFactory.CreateTest(data, testDefinition);
                default:
                    throw new NotSupportedException(string.Format("Invalid operation name: {0}.", operationName));
            }
        }
    }
}
