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

using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Options {
    /// <summary>
    /// Represents serialization options for a DateTime value.
    /// </summary>
    public class DateTimeSerializationOptions : IBsonSerializationOptions {
        #region private static fields
        private static DateTimeSerializationOptions dateOnlyInstance = new DateTimeSerializationOptions(true);
        private static DateTimeSerializationOptions defaults = new DateTimeSerializationOptions();
        private static DateTimeSerializationOptions localInstance = new DateTimeSerializationOptions(DateTimeKind.Local);
        private static DateTimeSerializationOptions utcInstance = new DateTimeSerializationOptions(DateTimeKind.Utc);
        #endregion

        #region private fields
        private bool dateOnly = false;
        private DateTimeKind kind = DateTimeKind.Utc;
        private BsonType representation = BsonType.DateTime;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the DateTimeSerializationOptions class.
        /// </summary>
        public DateTimeSerializationOptions() {
        }

        /// <summary>
        /// Initializes a new instance of the DateTimeSerializationOptions class.
        /// </summary>
        /// <param name="dateOnly">Whether this DateTime consists of a Date only.</param>
        public DateTimeSerializationOptions(
            bool dateOnly
        ) {
            this.dateOnly = dateOnly;
        }

        /// <summary>
        /// Initializes a new instance of the DateTimeSerializationOptions class.
        /// </summary>
        /// <param name="dateOnly">Whether this DateTime consists of a Date only.</param>
        /// <param name="representation">The external representation.</param>
        public DateTimeSerializationOptions(
            bool dateOnly,
            BsonType representation
        ) {
            this.dateOnly = dateOnly;
            this.representation = representation;
        }

        /// <summary>
        /// Initializes a new instance of theDateTimeSerializationOptions  class.
        /// </summary>
        /// <param name="kind">The DateTimeKind (Local, Unspecified or Utc).</param>
        public DateTimeSerializationOptions(
            DateTimeKind kind
        ) {
            this.kind = kind;
        }

        /// <summary>
        /// Initializes a new instance of the DateTimeSerializationOptions class.
        /// </summary>
        /// <param name="kind">The DateTimeKind (Local, Unspecified or Utc).</param>
        /// <param name="representation">The external representation.</param>
        public DateTimeSerializationOptions(
            DateTimeKind kind,
            BsonType representation
        ) {
            this.kind = kind;
            this.representation = representation;
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of DateTimeSerializationOptions with DateOnly=true.
        /// </summary>
        public static DateTimeSerializationOptions DateOnlyInstance {
            get { return dateOnlyInstance; }
        }

        /// <summary>
        /// Gets or sets the default DateTime serialization options.
        /// </summary>
        public static DateTimeSerializationOptions Defaults {
            get { return defaults; }
            set { defaults = value; }
        }

        /// <summary>
        /// Gets an instance of DateTimeSerializationOptions with Kind=Local.
        /// </summary>
        public static DateTimeSerializationOptions LocalInstance {
            get { return localInstance; }
        }

        /// <summary>
        /// Gets an instance of DateTimeSerializationOptions with Kind=Utc.
        /// </summary>
        public static DateTimeSerializationOptions UtcInstance {
            get { return utcInstance; }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets whether this DateTime consists of a Date only.
        /// </summary>
        public bool DateOnly {
            get { return dateOnly; }
        }

        /// <summary>
        /// Gets the DateTimeKind (Local, Unspecified or Utc).
        /// </summary>
        public DateTimeKind Kind {
            get { return kind; }
        }

        /// <summary>
        /// Gets the external representation.
        /// </summary>
        public BsonType Representation {
            get { return representation; }
        }
        #endregion
    }
}
