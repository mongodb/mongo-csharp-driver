namespace MongoDB.Bson.Serialization.Conventions
{
    using System;
    using Options;

    /// <summary>
    /// A convention that allows you to set the Decimal serialization representation
    /// </summary>
    public class DecimalRepresentationConvention : ConventionBase, IMemberMapConvention
    {
        // private fields
        private readonly BsonType _representation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalRepresentationConvention" /> class.
        /// </summary>
        /// <param name="representation">The serialization representation.</param>
        public DecimalRepresentationConvention(BsonType representation)
        {
            if (!((representation == BsonType.Array) ||
                  (representation == BsonType.Double) ||
                  (representation == BsonType.Int32) ||
                  (representation == BsonType.Int64) ||
                  (representation == BsonType.String)))
            {
                throw new ArgumentException("Decimals can only be represented as Array, Double, Int32, Int64 or String");
            }
            _representation = representation;
        }

        /// <summary>
        /// Changes the decimal representation if the member is a decimal
        /// </summary>
        /// <param name="memberMap"></param>
        public void Apply(BsonMemberMap memberMap)
        {
            if (memberMap.MemberType != typeof (decimal))
            {
                return;
            }
            memberMap.SetSerializationOptions(new RepresentationSerializationOptions(_representation));
        }
    }
}