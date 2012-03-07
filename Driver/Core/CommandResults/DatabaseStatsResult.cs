﻿/* Copyright 2010-2012 10gen Inc.
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
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the result of the database stats command.
    /// </summary>
    [Serializable]
    public class DatabaseStatsResult : CommandResult
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the DatabaseStatsResult class.
        /// </summary>
        public DatabaseStatsResult()
        {
        }

        // public properties
        /// <summary>
        /// Gets the average object size.
        /// </summary>
        public double AverageObjectSize
        {
            get { return Response["avgObjSize"].ToDouble(); }
        }

        /// <summary>
        /// Gets the collection count.
        /// </summary>
        public int CollectionCount
        {
            get { return Response["collections"].ToInt32(); }
        }

        /// <summary>
        /// Gets the data size.
        /// </summary>
        public long DataSize
        {
            get { return Response["dataSize"].ToInt64(); }
        }

        /// <summary>
        /// Gets the extent count.
        /// </summary>
        public int ExtentCount
        {
            get { return Response["numExtents"].ToInt32(); }
        }

        /// <summary>
        /// Gets the file size.
        /// </summary>
        public long FileSize
        {
            get { return Response["fileSize"].ToInt64(); }
        }

        /// <summary>
        /// Gets the index count.
        /// </summary>
        public int IndexCount
        {
            get { return Response["indexes"].ToInt32(); }
        }

        /// <summary>
        /// Gets the index size.
        /// </summary>
        public long IndexSize
        {
            get { return Response["indexSize"].ToInt64(); }
        }

        /// <summary>
        /// Gets the object count.
        /// </summary>
        public long ObjectCount
        {
            get { return Response["objects"].ToInt64(); }
        }

        /// <summary>
        /// Gets the storage size.
        /// </summary>
        public long StorageSize
        {
            get { return Response["storageSize"].ToInt64(); }
        }
    }
}
