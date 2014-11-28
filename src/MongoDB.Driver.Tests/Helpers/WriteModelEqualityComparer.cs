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

using System.Collections.Generic;

namespace MongoDB.Driver.Tests.Helpers
{
    public class WriteModelEqualityComparer<T> : IEqualityComparer<WriteModel<T>>
    {
        public bool Equals(WriteModel<T> x, WriteModel<T> y)
        {
            if ((object)x == (object)y)
            {
                return true;
            }

            if ((object)x == null || (object)y == null)
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            // since this is just for testing purposes we're going to assume that if they are the same type they are equal
            return true;
        }

        public int GetHashCode(WriteModel<T> x)
        {
            return 1;
        }
    }
}
