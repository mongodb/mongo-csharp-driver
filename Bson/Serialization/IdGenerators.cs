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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization {
    public class CombGuidGenerator : IIdGenerator {
        #region private static fields
        private static CombGuidGenerator instance = new CombGuidGenerator();
        #endregion

        #region constructors
        public CombGuidGenerator() {
        }
        #endregion

        #region public static properties
        public static CombGuidGenerator Instance {
            get { return instance; }
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
            var msecsArray = BitConverter.GetBytes((long) (msecs.TotalMilliseconds));

            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);

            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new Guid(guidArray);
        }

        public bool IsEmpty(
            object id
        ) {
            return id == null || (Guid) id == Guid.Empty;
        }
        #endregion
    }

    public class GuidGenerator : IIdGenerator {
        #region private static fields
        private static GuidGenerator instance = new GuidGenerator();
        #endregion

        #region constructors
        public GuidGenerator() {
        }
        #endregion

        #region public static properties
        public static GuidGenerator Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        public object GenerateId() {
            return Guid.NewGuid();
        }

        public bool IsEmpty(
            object id
        ) {
            return id == null || (Guid) id == Guid.Empty;
        }
        #endregion
    }

    public class NullIdChecker : IIdGenerator {
        #region private static fields
        private static NullIdChecker instance = new NullIdChecker();
        #endregion

        #region constructors
        public NullIdChecker() {
        }
        #endregion

        #region public static properties
        public static NullIdChecker Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        public object GenerateId() {
            throw new InvalidOperationException("Id cannot be null");
        }

        public bool IsEmpty(
            object id
        ) {
            return id == null;
        }
        #endregion
    }

    public class ObjectIdGenerator : IIdGenerator {
        #region private static fields
        private static ObjectIdGenerator instance = new ObjectIdGenerator();
        #endregion

        #region constructors
        public ObjectIdGenerator() {
        }
        #endregion

        #region public static properties
        public static ObjectIdGenerator Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        public object GenerateId() {
            return ObjectId.GenerateNewId();
        }

        public bool IsEmpty(
            object id
        ) {
            return id == null || (ObjectId) id == ObjectId.Empty;
        }
        #endregion
    }

    // TODO: is it worth trying to remove the dependency on IEquatable<T>?
    public class ZeroIdChecker<T> : IIdGenerator where T : struct, IEquatable<T> {
        #region constructors
        public ZeroIdChecker() {
        }
        #endregion

        #region public methods
        public object GenerateId() {
            throw new InvalidOperationException("Id cannot be default value (all zeros)");
        }

        public bool IsEmpty(
            object id
        ) {
            return id == null || ((T) id).Equals(default(T));
        }
        #endregion
    }
}
