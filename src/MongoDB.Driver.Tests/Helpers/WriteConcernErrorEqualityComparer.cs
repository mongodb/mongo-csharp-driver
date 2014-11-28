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
    public class WriteConcernErrorEqualityComparer : IEqualityComparer<WriteConcernError>
    {
        public bool Equals(WriteConcernError x, WriteConcernError y)
        {
            if ((object)x == (object)y)
            {
                return true;
            }

            if ((object)x == null || (object)y == null)
            {
                return false;
            }

            return
                x.Code == y.Code &&
                object.Equals(x.Details, y.Details) &&
                object.Equals(x.Message, y.Message);
        }

        public int GetHashCode(WriteConcernError x)
        {
            return 1;
        }
    }
}
