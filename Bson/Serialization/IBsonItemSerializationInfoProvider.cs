using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Contract for serializers to implement if they serialize an array of items.
    /// </summary>
    public interface IBsonItemSerializationInfoProvider
    {
        /// <summary>
        /// Gets the serialization info for individual items of an enumerable type.
        /// </summary>
        /// <returns>The serialization info for the items.</returns>
        BsonSerializationInfo GetItemSerializationInfo();
    }
}
