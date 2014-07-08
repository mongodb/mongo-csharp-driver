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

namespace MongoDB.Driver.Core.Bindings
{
    public abstract class ReadBindingHandle : ReadBindingWrapper
    {
        // fields
        private readonly ReferenceCountedReadBinding _wrapped;

        // constructors
        protected ReadBindingHandle(ReferenceCountedReadBinding wrapped)
            : base(wrapped, ownsWrapped: false)
        {
            _wrapped = wrapped;
        }

        // methods
        protected abstract ReadBindingHandle CreateNewHandle(ReferenceCountedReadBinding wrapped);

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    _wrapped.DecrementReferenceCount();
                }
            }
            base.Dispose(disposing);
        }

        protected override IReadBinding ForkImplementation()
        {
            _wrapped.IncrementReferenceCount();
            return CreateNewHandle(_wrapped);
        }
    }
}
