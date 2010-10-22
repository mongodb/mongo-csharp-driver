/* Copyright 2010 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public class GuidIdGenerator : IBsonIdGenerator {
        #region private static fields
        private static GuidIdGenerator singleton = new GuidIdGenerator();
        #endregion

        #region constructors
        private GuidIdGenerator() {
        }
        #endregion

        #region public static properties
        public static GuidIdGenerator Singleton {
            get { return singleton; }
        }
        #endregion

        #region public methods
        public object GenerateId() {
            return Guid.NewGuid();
        }

        public bool IsEmpty(
            object id
        ) {
            return (Guid) id == Guid.Empty;
        }
        #endregion
    }

    public class ObjectIdIdGenerator : IBsonIdGenerator {
        #region private static fields
        private static ObjectIdIdGenerator singleton = new ObjectIdIdGenerator();
        #endregion

        #region constructors
        private ObjectIdIdGenerator() {
        }
        #endregion

        #region public static properties
        public static ObjectIdIdGenerator Singleton {
            get { return singleton; }
        }
        #endregion

        #region public methods
        public object GenerateId() {
            return ObjectId.GenerateNewId();
        }

        public bool IsEmpty(
            object id
        ) {
            return (ObjectId) id == ObjectId.Empty;
        }
        #endregion
    }
}
