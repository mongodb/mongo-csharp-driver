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

using System.Collections.Generic;

namespace MongoDB.Driver.Core.Helpers
{
    public class WriteConcernResultEqualityComparer : IEqualityComparer<WriteConcernResult>
    {
        public bool Equals(WriteConcernResult x, WriteConcernResult y)
        {
            if ((object)x == (object)y)
            {
                return true;
            }

            if ((object)x == null || (object)y == null)
            {
                return false;
            }

            return object.Equals(x.Response, y.Response);
        }

        public int GetHashCode(WriteConcernResult x)
        {
            return 1;
        }
    }
}
