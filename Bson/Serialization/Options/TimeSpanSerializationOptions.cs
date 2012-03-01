/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Bson.Serialization.Options
{
    /// <summary>
    /// Represents serialization options for a TimeSpan value.
    /// </summary>
    public class TimeSpanSerializationOptions : IBsonSerializationOptions
    {
        // private fields
        private BsonType _representation;
        private TimeSpanUnits _units;

        // constructors
        /// <summary>
        /// Initializes a new instance of the TimeSpanSerializationOptions class.
        /// </summary>
        /// <param name="representation">The representation for serialized TimeSpans.</param>
        public TimeSpanSerializationOptions(BsonType representation)
            : this(representation, TimeSpanUnits.Ticks)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TimeSpanSerializationOptions class.
        /// </summary>
        /// <param name="representation">The representation for serialized TimeSpans.</param>
        /// <param name="units">The units for serialized TimeSpans.</param>
        public TimeSpanSerializationOptions(BsonType representation, TimeSpanUnits units)
        {
            _representation = representation;
            _units = units;
        }

        // public properties
        /// <summary>
        /// Gets the representation for serialized TimeSpans.
        /// </summary>
        public BsonType Representation
        {
            get { return _representation; }
        }

        /// <summary>
        /// Gets the units for serialized TimeSpans.
        /// </summary>
        public TimeSpanUnits Units
        {
            get { return _units; }
        }
    }
}
