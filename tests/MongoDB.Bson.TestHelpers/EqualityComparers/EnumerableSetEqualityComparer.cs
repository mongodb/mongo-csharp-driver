/* Copyright 2016 MongoDB Inc.
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

namespace MongoDB.Bson.TestHelpers.EqualityComparers
{
    public class EnumerableSetEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        IEqualityComparer<T> _comparer;

        public EnumerableSetEqualityComparer(IEqualityComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
            {
                return false;
            }

            var materializedX = x.ToList();
            var materializedY = y.ToList();

            while (materializedX.Count > 0 && materializedY.Count > 0)
            {
                var xValue = materializedX[0];
                var index = materializedY.FindIndex(yValue => _comparer.Equals(xValue, yValue));
                if (index != -1)
                {
                    materializedX.RemoveAt(0);
                    materializedY.RemoveAt(index);
                }
                else
                {
                    return false;
                }
            }

            return materializedX.Count == 0 && materializedY.Count == 0;
        }

        public int GetHashCode(IEnumerable<T> obj)
        {
            throw new NotImplementedException();
        }
    }
}
