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
using System.Linq;
using System.Text;

namespace MongoDB.Bson.DefaultSerializer {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple=true)]
    public class BsonKnownTypeAttribute : Attribute {
        #region private fields
        private Type knownType;
        #endregion

        #region constructors
        public BsonKnownTypeAttribute(
            Type knownType
        ) {
            this.knownType = knownType;
        }
        #endregion

        #region public properties
        public Type KnownType {
            get { return knownType; }
        }
        #endregion
    }
}
