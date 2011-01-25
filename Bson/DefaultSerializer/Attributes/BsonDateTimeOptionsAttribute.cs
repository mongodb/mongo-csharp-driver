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
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    // [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class BsonDateTimeOptionsAttribute : BsonSerializationOptionsAttribute {
        #region private fields
        private bool dateOnly = false;
        private DateTimeKind kind = DateTimeKind.Utc;
        private BsonType representation = BsonType.DateTime;
        #endregion

        #region constructors
        public BsonDateTimeOptionsAttribute() {
        }
        #endregion

        #region public properties
        public bool DateOnly {
            get { return dateOnly; }
            set { dateOnly = value; }
        }

        public DateTimeKind Kind {
            get { return kind; }
            set { kind = value; }
        }

        public BsonType Representation {
            get { return representation; }
            set { representation = value; }
        }
        #endregion

        #region public methods
        public override IBsonSerializationOptions GetOptions() {
            return new DateTimeSerializationOptions { DateOnly = dateOnly, Kind = kind, Representation = representation };
        }
        #endregion
    }
}
