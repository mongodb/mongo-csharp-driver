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

using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal class MemberSerializationInfo
    {
        // private fields
        private readonly string _elementName;
        private readonly IReadOnlyList<string> _elementPath;
        private readonly IBsonSerializer _serializer;

        // constructors
        public MemberSerializationInfo(string elementName, IBsonSerializer serializer)
        {
            _elementName = Ensure.IsNotNullOrEmpty(elementName, nameof(elementName));
            _serializer = Ensure.IsNotNull(serializer, nameof(serializer));
        }

        public MemberSerializationInfo(IReadOnlyList<string> elementPath, IBsonSerializer serializer)
        {
            _elementPath = Ensure.IsNotNull(elementPath, nameof(elementPath));
            _serializer = Ensure.IsNotNull(serializer, nameof(serializer));
        }

        // public properties
        public string ElementName
        {
            get
            {
                if (_elementPath != null)
                {
                    throw new InvalidOperationException("When ElementPath is not null you must use it instead.");
                }
                return _elementName;
            }
        }

        public IReadOnlyList<string> ElementPath => _elementPath;

        public IBsonSerializer Serializer => _serializer;
    }
}
