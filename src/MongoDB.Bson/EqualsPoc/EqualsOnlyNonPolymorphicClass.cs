namespace MongoDB.Bson.EqualsPoc
{
    internal sealed class EqualsOnlyNonPolymorphicClass
    {
        private object _ref;
        private int _value;

        public EqualsOnlyNonPolymorphicClass(object @ref, int value)
        {
            _ref = @ref;
            _value = value;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                obj is EqualsOnlyNonPolymorphicClass other &&
                object.Equals(_ref, other._ref) &&
                _value.Equals(other._value);
        }

        public override int GetHashCode() => 0; // implement as appropriate

        // optionally implement == and !=
        public static bool operator ==(EqualsOnlyNonPolymorphicClass lhs, EqualsOnlyNonPolymorphicClass rhs) => object.Equals(lhs, rhs);
        public static bool operator !=(EqualsOnlyNonPolymorphicClass lhs, EqualsOnlyNonPolymorphicClass rhs) => !(lhs == rhs);
    }
}
