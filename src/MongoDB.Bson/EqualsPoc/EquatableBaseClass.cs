using System;

namespace MongoDB.Bson.EqualsPoc
{
    internal class EquatableBaseClass : IEquatable<EquatableBaseClass>
    {
        public object _ref1;
        public int _value1;

        public EquatableBaseClass(object ref1, int value1)
        {
            _ref1 = ref1;
            _value1 = value1;
        }

        public override bool Equals(object obj) =>
            Equals(obj as EquatableBaseClass);

        public bool Equals(EquatableBaseClass other)
        {
            if (object.ReferenceEquals(other, null)) { return false; }
            if (object.ReferenceEquals(this, other)) { return true; }
            return
                GetType().Equals(other.GetType()) &&
                object.Equals(_ref1, other._ref1) &&
                _value1.Equals(other._value1);
        }

        public override int GetHashCode() => 0; // implement as appropriate

        // optionally implement == and !=
        public static bool operator ==(EquatableBaseClass lhs, EquatableBaseClass rhs) => object.Equals(lhs, rhs);
        public static bool operator !=(EquatableBaseClass lhs, EquatableBaseClass rhs) => !(lhs == rhs);
    }
}
