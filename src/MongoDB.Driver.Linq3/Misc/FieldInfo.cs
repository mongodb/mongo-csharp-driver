﻿/* Copyright 2010-present MongoDB Inc.
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

using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq3.Misc
{
    public class FieldInfo
    {
        // private fields
        private readonly string _elementName;
        private readonly IBsonSerializer _serializer;

        // constructors
        public FieldInfo(string elementName, IBsonSerializer serializer)
        {
            _elementName = Throw.IfNullOrEmpty(elementName, nameof(elementName));
            _serializer = Throw.IfNull(serializer, nameof(serializer));
        }

        // public properties
        public string ElementName => _elementName;
        public IBsonSerializer Serializer => _serializer;
    }
}
