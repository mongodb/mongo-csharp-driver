using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Modifies a member map.
    /// </summary>
    public interface IBsonMemberMapModifier
    {
        /// <summary>
        /// Applies a modification to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        void Apply(BsonMemberMap memberMap);
    }
}
