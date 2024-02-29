namespace MongoDB.Bson.EqualsPoc
{
    internal class EqualsOnlyDerivedClass : EquatableBaseClass
    {
        private object _ref2;
        private int _value2;

        public EqualsOnlyDerivedClass(object ref1, int value1, object ref2, int value2)
            : base(ref1, value1)
        {
            _ref2 = ref2;
            _value2 = value2;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is EqualsOnlyDerivedClass other &&
                object.Equals(_ref2, other._ref2) &&
                _value2.Equals(other._value2);
        }

        public override int GetHashCode() => 0; // implement as appropriate
    }
}
