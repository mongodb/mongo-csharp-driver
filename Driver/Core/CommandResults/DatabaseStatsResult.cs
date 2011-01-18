/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.Driver {
    [Serializable]
    public class DatabaseStatsResult : CommandResult {
        #region constructors
        public DatabaseStatsResult() {
        }
        #endregion

        #region public properties
        public double AverageObjectSize {
            get { return response["avgObjSize"].ToDouble(); }
        }

        public int CollectionCount {
            get { return response["collections"].ToInt32(); }
        }

        public long DataSize {
            get { return response["dataSize"].ToInt64(); }
        }

        public int ExtentCount {
            get { return response["numExtents"].ToInt32(); }
        }

        public long FileSize {
            get { return response["fileSize"].ToInt64(); }
        }

        public int IndexCount {
            get { return response["indexes"].ToInt32(); }
        }

        public long IndexSize {
            get { return response["indexSize"].ToInt64(); }
        }

        public long ObjectCount {
            get { return response["objects"].ToInt64(); }
        }

        public long StorageSize {
            get { return response["storageSize"].ToInt64(); }
        }
        #endregion
    }
}
