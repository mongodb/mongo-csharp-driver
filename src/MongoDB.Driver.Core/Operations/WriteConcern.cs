/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class WriteConcern
    {
        #region static
        // static fields
        private static readonly WriteConcern __acknowledged = new WriteConcern();
        private static readonly WriteConcern __unacknowledged = new WriteConcern(0);
        private static readonly WriteConcern __w1 = new WriteConcern(1);
        private static readonly WriteConcern __w2 = new WriteConcern(2);
        private static readonly WriteConcern __w3 = new WriteConcern(3);
        private static readonly WriteConcern __wMajority = new WriteConcern("majority");

        // static properties
        public static WriteConcern Acknowledged
        {
            get { return __acknowledged; }
        }

        public static WriteConcern None
        {
            get { return null; }
        }

        public static WriteConcern W1
        {
            get { return __w1; }
        }

        public static WriteConcern W2
        {
            get { return __w2; }
        }

        public static WriteConcern W3
        {
            get { return __w3; }
        }

        public static WriteConcern Unacknowledged
        {
            get { return __unacknowledged; }
        }

        public static WriteConcern WMajority
        {
            get { return __wMajority; }
        }
        #endregion

        // fields
        private readonly bool? _fsync;
        private readonly bool? _journal;
        private readonly WValue _w;
        private readonly TimeSpan? _wTimeout;

        // constructors
        public WriteConcern()
        {
        }

        public WriteConcern(int w)
            : this(w, null, null, null)
        {
        }

        public WriteConcern(string mode)
            : this(mode, null, null, null)
        {
        }

        public WriteConcern(
            WValue w,
            TimeSpan? wTimeout,
            bool? fsync,
            bool? journal)
        {
            _w = Ensure.IsNotNull(w, "w");
            _wTimeout = wTimeout;
            _fsync = fsync;
            _journal = journal;
        }

        // properties
        public bool? FSync
        {
            get { return _fsync; }
        }

        public bool? Journal
        {
            get { return _journal; }
        }

        public WValue W
        {
            get { return _w; }
        }

        public TimeSpan? WTimeout
        {
            get { return _wTimeout; }
        }

        // methods
        public override string ToString()
        {
            var parts = new List<string>();
            if (_w != null)
            {
                parts.Add(string.Format("w : {0}", _w));
            }
            if (_wTimeout != null)
            {
                parts.Add(string.Format("wtimeout : {0}", _wTimeout));
            }
            if (_fsync != null)
            {
                parts.Add(string.Format("fsync : {0}", _fsync.Value));
            }
            if (_journal != null)
            {
                parts.Add(string.Format("w : {0}", _journal.Value));
            }

            if (parts.Count == 0)
            {
                return "{ }";
            }
            else
            {
                return string.Format("{{ {0} }}", string.Join(", ", parts.ToArray()));
            }
        }

        public WriteConcern WithFSync(bool? value)
        {
            return (_fsync == value) ? this : new Builder(this) { _fsync = value }.Build();
        }

        public WriteConcern WithJournal(bool? value)
        {
            return (_journal == value) ? this : new Builder(this) { _journal = value }.Build();
        }

        public WriteConcern WithW(WValue value)
        {
            return (object.Equals(_w, value)) ? this : new Builder(this) { _w = value }.Build();
        }

        public WriteConcern WithWTimeout(TimeSpan? value)
        {
            return (_wTimeout == value) ? this : new Builder(this) { _wTimeout = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public bool? _fsync;
            public bool? _journal;
            public WValue _w;
            public TimeSpan? _wTimeout;

            // constructors
            public Builder(WriteConcern other)
            {
                _fsync = other.FSync;
                _journal = other.Journal;
                _w = other.W;
                _wTimeout = other.WTimeout;
            }

            // methods
            public WriteConcern Build()
            {
                return new WriteConcern(_w, _wTimeout, _fsync, _journal);
            }
        }

        public abstract class WValue
        {
            #region static
            // static operators
            public static implicit operator WValue(int value)
            {
                return new WCount(value);
            }

            public static implicit operator WValue(string value)
            {
                return (value == null) ? null : new WMode(value);
            }
            #endregion

            // constructors
            internal WValue()
            {
            }

            // methods
            public abstract BsonValue ToBsonValue();
        }

        public sealed class WCount : WValue
        {
            // fields
            private readonly int _value;

            // constructors
            public WCount(int w)
            {
                _value = Ensure.IsGreaterThanOrEqualToZero(w, "w");
            }

            // properties
            public int Value
            {
                get { return _value; }
            }

            // methods
            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != typeof(WCount))
                {
                    return false;
                }
                var rhs = (WCount)obj;
                return _value == rhs._value;
            }

            public override int GetHashCode()
            {
                return _value.GetHashCode();
            }

            public override BsonValue ToBsonValue()
            {
                return new BsonInt32(_value);
            }
        }

        public sealed class WMode : WValue
        {
            #region static
            // static properties
            public static WMode Majority
            {
                get { return new WMode("majority"); }
            }
            #endregion

            // fields
            private readonly string _value;

            // constructors
            public WMode(string mode)
            {
                _value = Ensure.IsNotNullOrEmpty(mode, "mode");
            }

            // properties
            public string Value
            {
                get { return _value; }
            }

            // methods
            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != typeof(WMode))
                {
                    return false;
                }
                var rhs = (WMode)obj;
                return _value == rhs._value;
            }

            public override int GetHashCode()
            {
                return _value.GetHashCode();
            }

            public override BsonValue ToBsonValue()
            {
                return new BsonString(_value);
            }
        }
    }
}
