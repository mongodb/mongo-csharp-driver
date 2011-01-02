﻿/* Copyright 2010 10gen Inc.
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

namespace MongoDB.Bson {
    public static class BsonDefaults {
        #region private static fields
        private static int initialBsonBufferSize = 4 * 1024; // 4KiB
        private static int maxDocumentSize = 4 * 1024 * 1024; // 4MiB
        #endregion

        #region public static properties
        public static int InitialBsonBufferSize {
            get { return initialBsonBufferSize; }
            set { initialBsonBufferSize = value; }
        }

        public static int MaxDocumentSize {
            get { return maxDocumentSize; }
            set { maxDocumentSize = value; }
        }
        #endregion
    }
}
