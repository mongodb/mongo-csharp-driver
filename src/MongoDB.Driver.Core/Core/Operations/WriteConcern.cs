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
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Operations
{
    public class WriteConcern : IEquatable<WriteConcern>
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

        public static WriteConcern Unacknowledged
        {
            get { return __unacknowledged; }
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
            _w = w;
            _wTimeout = wTimeout;
            _fsync = fsync;
            _journal = journal;
        }

        // properties
        public bool? FSync
        {
            get { return _fsync; }
        }

        public bool IsAcknowledged
        {
            get
            {
                return
                    _w != null && !_w.Equals((WValue)0) ||
                    _wTimeout.HasValue ||
                    _fsync.HasValue ||
                    _journal.HasValue;
            }
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
        public override bool Equals(object obj)
        {
            return Equals(obj as WriteConcern);
        }

        public bool Equals(WriteConcern rhs)
        {
            if (object.ReferenceEquals(rhs, null) || rhs.GetType() != typeof(WriteConcern))
            {
                return false;
            }

            return
                _fsync == rhs._fsync &&
                _journal == rhs._journal &&
                object.Equals(_w, rhs._w) &&
                _wTimeout == rhs._wTimeout;
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_fsync)
                .Hash(_journal)
                .Hash(_w)
                .Hash(_wTimeout)
                .GetHashCode();
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (_w != null)
            {
                parts.Add(string.Format("w : {0}", _w));
            }
            if (_wTimeout != null)
            {
                parts.Add(string.Format("wtimeout : {0}", TimeSpanParser.ToString(_wTimeout.Value)));
            }
            if (_fsync != null)
            {
                parts.Add(string.Format("fsync : {0}", _fsync.Value ? "true" : "false" ));
            }
            if (_journal != null)
            {
                parts.Add(string.Format("journal : {0}", _journal.Value ? "true" : "false"));
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

        public abstract class WValue : IEquatable<WValue>
        {
            #region static
            // static methods
            public static WValue Parse(string value)
            {
                int n;
                if (int.TryParse(value, out n))
                {
                    return new WCount(n);
                }
                else
                {
                    return new WMode(value);
                }
            }

            // static operators
            public static implicit operator WValue(int value)
            {
                return new WCount(value);
            }

            public static implicit operator WValue(int? value)
            {
                return value.HasValue ? new WCount(value.Value) : null;
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
            public bool Equals(WValue rhs)
            {
                return Equals((object)rhs);
            }

            public abstract BsonValue ToBsonValue();
        }

        public sealed class WCount : WValue, IEquatable<WCount>
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
                return Equals(obj as WCount);
            }

            public bool Equals(WCount rhs)
            {
                if (object.ReferenceEquals(rhs, null) || rhs.GetType() != typeof(WCount))
                {
                    return false;
                }
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

            public override string ToString()
            {
                return _value.ToString();
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
                return Equals(obj as WMode);
            }

            public bool Equals(WMode rhs)
            {
                if (object.ReferenceEquals(rhs, null) || rhs.GetType() != typeof(WMode))
                {
                    return false;
                }
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

            public override string ToString()
            {
                return "\"" + _value + "\"";
            }
        }
    }
}
