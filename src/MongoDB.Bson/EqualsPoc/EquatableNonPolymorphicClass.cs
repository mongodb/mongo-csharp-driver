using System;

namespace MongoDB.Bson.EqualsPoc
{
    internal sealed class EquatableNonPolymorphicClass : IEquatable<EquatableNonPolymorphicClass>
    {
        private object _ref;
        private int _value;

        public EquatableNonPolymorphicClass(object @ref, int value)
        {
            _ref = @ref;
            _value = value;
        }

        public override bool Equals(object obj) =>
            Equals(obj as EquatableNonPolymorphicClass);

        public bool Equals(EquatableNonPolymorphicClass other) =>
            object.ReferenceEquals(this, other) ||
            !object.ReferenceEquals(other, null) &&
            object.Equals(_ref, other._ref) &&
            _value.Equals(other._value);

        public override int GetHashCode() => 0; // implement as appropriate

        // optionally implement == and !=
        public static bool operator ==(EquatableNonPolymorphicClass lhs, EquatableNonPolymorphicClass rhs) => object.Equals(lhs, rhs);
        public static bool operator !=(EquatableNonPolymorphicClass lhs, EquatableNonPolymorphicClass rhs) => !(lhs == rhs);
    }
}
