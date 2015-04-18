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

using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Processors
{
    internal class ProjectionBindingContext
    {
        private readonly CorrelatedGroupMap _groupMap;
        private readonly IMethodCallBinder _methodCallBinder;
        private readonly IBsonSerializerRegistry _serializerRegistry;

        public ProjectionBindingContext(IBsonSerializerRegistry serializerRegistry, IMethodCallBinder methodCallBinder)
        {
            _serializerRegistry = Ensure.IsNotNull(serializerRegistry, "serializerRegistry");
            _methodCallBinder = Ensure.IsNotNull(methodCallBinder, "methodCallBinder");
            _groupMap = new CorrelatedGroupMap();
        }

        public CorrelatedGroupMap GroupMap
        {
            get { return _groupMap; }
        }

        public IMethodCallBinder MethodCallBinder
        {
            get { return _methodCallBinder; }
        }

        public IBsonSerializerRegistry SerializerRegistry
        {
            get { return _serializerRegistry; }
        }
    }
}
