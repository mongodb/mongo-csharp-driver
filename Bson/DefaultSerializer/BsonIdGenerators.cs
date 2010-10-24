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
    public class GuidGenerator : IBsonIdGenerator {
        #region constructors
        public GuidGenerator() {
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

    public class ObjectIdGenerator : IBsonIdGenerator {
        #region constructors
        public ObjectIdGenerator() {
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

    public class CombGuidGenerator : IBsonIdGenerator
    {
        #region constructors
        public CombGuidGenerator() {
        }
        #endregion

        #region public methods
        public object GenerateId() {
            var guidArray = Guid.NewGuid().ToByteArray();

            var baseDate = new DateTime(1900, 1, 1);
            var now = DateTime.Now;

            var days = new TimeSpan(now.Ticks - baseDate.Ticks);
            var msecs = now.TimeOfDay;

            var daysArray = BitConverter.GetBytes(days.Days);
            var msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds));

            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);

            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new Guid(guidArray);
        }

        public bool IsEmpty(
            object id
        ) {
            return (Guid) id == Guid.Empty;
        }
        #endregion
    }
}
