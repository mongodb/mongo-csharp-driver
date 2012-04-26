using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Modifies a member map.
    /// </summary>
    public interface IBsonClassMapModifier
    {
        /// <summary>
        /// Applies a modification to the class map.
        /// </summary>
        /// <param name="classMap">The class map.</param>
        void Apply(BsonClassMap classMap);
    }
}