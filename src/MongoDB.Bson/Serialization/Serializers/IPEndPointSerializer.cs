/* Copyright 2010-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for IPEndPoints.
    /// </summary>
    public class IPEndPointSerializer : BsonBaseSerializer<IPEndPoint>
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the IPEndPointSerializer class.
        /// </summary>
        public IPEndPointSerializer()
        {
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override IPEndPoint Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;
            string message;

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;

                case BsonType.String:
                    var stringValue = bsonReader.ReadString();
                    var match = Regex.Match(stringValue, @"^(?<address>(.+|\[.*\]))\:(?<port>\d+)$");
                    if (match.Success)
                    {
                        IPAddress address;
                        if (IPAddress.TryParse(match.Groups["address"].Value, out address))
                        {
                            int port;
                            if (int.TryParse(match.Groups["port"].Value, out port))
                            {
                                return new IPEndPoint(address, port);
                            }
                        }
                    }
                    message = string.Format("Invalid IPEndPoint value '{0}'.", stringValue);
                    throw new FileFormatException(message);

                default:
                    message = string.Format("Cannot deserialize IPEndPoint from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, IPEndPoint value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                string stringValue;
                if (value.AddressFamily == AddressFamily.InterNetwork)
                {
                    stringValue = string.Format("{0}:{1}", value.Address, value.Port); // IPv4
                }
                else
                {
                    stringValue = string.Format("[{0}]:{1}", value.Address, value.Port); // IPv6
                }
                bsonWriter.WriteString(stringValue);
            }
        }
    }
}
