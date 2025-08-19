/* Copyright 2010-present MongoDB Inc.
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

using System.Collections.Generic;
using System.Dynamic;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson
{
    internal class BsonDefaultsDomain : IBsonDefaults
    {
        private IBsonSerializationDomain _serializationDomain;
        private bool _dynamicArraySerializerWasSet;
        private IBsonSerializer _dynamicArraySerializer;
        private bool _dynamicDocumentSerializerWasSet;
        private IBsonSerializer _dynamicDocumentSerializer;

        public BsonDefaultsDomain(IBsonSerializationDomain serializationDomain)
        {
            _serializationDomain = serializationDomain;
        }

        public IBsonSerializer DynamicArraySerializer
        {
            get
            {
                if (!_dynamicArraySerializerWasSet)
                {
                    _dynamicArraySerializer = _serializationDomain.LookupSerializer<List<object>>();
                }
                return _dynamicArraySerializer;
            }
            set
            {
                _dynamicArraySerializerWasSet = true;
                _dynamicArraySerializer = value;
            }
        }

        public IBsonSerializer DynamicDocumentSerializer
        {
            get
            {
                if (!_dynamicDocumentSerializerWasSet)
                {
                    _dynamicDocumentSerializer = _serializationDomain.LookupSerializer<ExpandoObject>();
                }
                return _dynamicDocumentSerializer;
            }
            set
            {
                _dynamicDocumentSerializerWasSet = true;
                _dynamicDocumentSerializer = value;
            }
        }

        public int MaxDocumentSize { get; set; } = int.MaxValue;

        public int MaxSerializationDepth { get; set; } = 100;
    }
}