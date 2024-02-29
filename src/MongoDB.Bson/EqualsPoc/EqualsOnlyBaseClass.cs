namespace MongoDB.Bson.EqualsPoc
{
    internal class EqualsOnlyBaseClass
    {
        public object _ref1;
        public int _value1;

        public EqualsOnlyBaseClass(object ref1, int value1)
        {
            _ref1 = ref1;
            _value1 = value1;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                GetType().Equals(obj.GetType()) &&
                obj is EqualsOnlyBaseClass other &&
                object.Equals(_ref1, other._ref1) &&
                _value1.Equals(other._value1);
        }

        public override int GetHashCode() => 0; // implement as appropriate

        // optionally implement == and !=
        public static bool operator ==(EqualsOnlyBaseClass lhs, EqualsOnlyBaseClass rhs) => object.Equals(lhs, rhs);
        public static bool operator !=(EqualsOnlyBaseClass lhs, EqualsOnlyBaseClass rhs) => !(lhs == rhs);
    }
}
