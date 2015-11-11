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
using System.IO;
using MongoDB.Bson;

namespace MongoDB.Driver.GridFS.Tests.Specifications.gridfs
{
    public static class GridFSDeleteTestFactory
    {
        // static public methods
        public static IGridFSTest CreateTest(BsonDocument data, BsonDocument testDefinition)
        {
            if (testDefinition["assert"].AsBsonDocument.Contains("result"))
            {
                return new GridFSDeleteTest(data, testDefinition);
            }

            var error = testDefinition["assert"]["error"].AsString;
            switch (error)
            {
                case "FileNotFound":
                    return new GridFSDeleteTest<GridFSFileNotFoundException>(data, testDefinition);
                default:
                    throw new NotSupportedException(string.Format("Invalid error: {0}.", error));
            }
        }
    }
}
