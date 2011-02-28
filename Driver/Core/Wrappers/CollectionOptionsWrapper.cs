﻿/* Copyright 2010-2011 10gen Inc.
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

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver {
    public class CollectionOptionsWrapper : BaseWrapper, IMongoCollectionOptions {
        #region constructors
        public CollectionOptionsWrapper(
            object options
        )
            : base(options) {
        }
        #endregion

        #region public static methods
        public static CollectionOptionsWrapper Create(
            object options
        ) {
            if (options == null) {
                return null;
            } else {
                return new CollectionOptionsWrapper(options);
            }
        }
        #endregion
    }
}
