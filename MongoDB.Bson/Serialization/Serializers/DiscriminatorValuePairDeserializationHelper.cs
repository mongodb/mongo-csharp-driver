/* Copyright 2015 MongoDB Inc.
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
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    internal class DiscriminatorValuePairDeserializationHelper
    {
        private readonly string _discriminatorElementName;
        private readonly string _valueElementName;

        public DiscriminatorValuePairDeserializationHelper(string discriminatorElementName, string valueElementName)
        {
            _discriminatorElementName = discriminatorElementName;
            _valueElementName = valueElementName;
        }

        public object Deserialize(
            BsonReader bsonReader,
            Type actualType,
            IBsonSerializer valueSerializer,
            IBsonSerializationOptions options)
        {
            object value = null;
            var wasValuePresent = false;

            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != 0)
            {
                var name = bsonReader.ReadName();
                if (name == _discriminatorElementName)
                {
                    bsonReader.SkipValue();
                }
                else if (name == _valueElementName)
                {
                    value = valueSerializer.Deserialize(bsonReader, actualType, actualType, options);
                    wasValuePresent = true;
                }
                else
                {
                    var message = string.Format("Unexpected element name: '{0}'", name);
                    throw new FormatException(message);
                }
            }
            bsonReader.ReadEndDocument();

            if (!wasValuePresent)
            {
                throw new FormatException("_v element missing");
            }

            return value;
        }
    }
}
