/* Copyright 2017 MongoDB Inc.
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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;

namespace MongoDB.Driver.Core.TestHelpers
{
    public static class ICoreSessionHandleExtensions
    {
        public static int ReferenceCount(this ICoreSessionHandle session)
        {
            var handle = (CoreSessionHandle)session;
            var referenceCounted = handle._wrapped();
            return referenceCounted._referenceCount();
        }

        private static ReferenceCountedCoreSession _wrapped(this CoreSessionHandle obj)
        {
            var fieldInfo = typeof(CoreSessionHandle).GetField("_wrapped", BindingFlags.NonPublic | BindingFlags.Instance);
            return (ReferenceCountedCoreSession)fieldInfo.GetValue(obj);
        }

        private static int _referenceCount(this ReferenceCountedCoreSession obj)
        {
            var fieldInfo = typeof(ReferenceCountedCoreSession).GetField("_referenceCount", BindingFlags.NonPublic | BindingFlags.Instance);
            return (int)fieldInfo.GetValue(obj);
        }
    }
}
