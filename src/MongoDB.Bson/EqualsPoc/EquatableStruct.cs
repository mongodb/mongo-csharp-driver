using System;

namespace MongoDB.Bson.EqualsPoc
{
    internal struct EquatableStruct : IEquatable<EquatableStruct>
    {
        private object _ref;
        private int _value;

        public EquatableStruct(object @ref, int value)
        {
            _ref = @ref;
            _value = value;
        }

        public override bool Equals(object obj) =>
            obj is EquatableStruct other && Equals(other);

        public bool Equals(EquatableStruct other)
        {
            return
                object.Equals(_ref, other._ref) &&
                _value.Equals(other._value);
        }

        public override int GetHashCode() => 0; // implement as appropriate

        // optionally implement == and !=
        public static bool operator ==(EquatableStruct lhs, EquatableStruct rhs) => lhs.Equals(rhs);
        public static bool operator !=(EquatableStruct lhs, EquatableStruct rhs) => !(lhs == rhs);
    }
}
