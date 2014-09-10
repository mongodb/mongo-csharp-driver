/* Copyright 2013-2014 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a deserializer that deserializes the selected element and skips any others.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class ElementDeserializer<TValue> : SerializerBase<TValue>
    {
        // private fields
        private readonly string _elementName;
        private readonly IBsonSerializer<TValue> _valueSerializer;

        // constructors
        public ElementDeserializer(string elementName, IBsonSerializer<TValue> valueSerializer)
        {
            _elementName = elementName;
            _valueSerializer = valueSerializer;
        }

        // methods
        public override TValue Deserialize(BsonDeserializationContext context)
        {
            var reader = context.Reader;
            TValue value = default(TValue);
            reader.ReadStartDocument();
            while (reader.ReadBsonType() != 0)
            {
                var elementName = reader.ReadName();
                if (elementName == _elementName)
                {
                    value = context.DeserializeWithChildContext<TValue>(_valueSerializer);
                }
                else
                {
                    reader.SkipValue();
                }
            }
            reader.ReadEndDocument();
            return value;
        }
    }
}
