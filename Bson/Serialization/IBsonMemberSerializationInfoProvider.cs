using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Contract for composite serializers that contain a number of named serializers.
    /// </summary>
    public interface IBsonMemberSerializationInfoProvider
    {
        /// <summary>
        /// Gets the serialization info for a member.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <returns>The serialization info for the member.</returns>
        BsonSerializationInfo GetMemberSerializationInfo(string memberName);
    }
}
