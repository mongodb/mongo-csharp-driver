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

using System;
using System.Collections;
using System.Reflection;

namespace MongoDB.Bson.TestHelpers.EqualityComparers
{
    public class FieldsEqualityComparer : IEqualityComparer
    {
        // fields
        private readonly IEqualityComparerSource _source;

        // constructors
        public FieldsEqualityComparer(IEqualityComparerSource source)
        {
            _source = source;
        }

        // methods
        public new bool Equals(object x, object y)
        {
            if (x == null) { return y == null; }
            if (x.GetType() != y.GetType()) { return false; }

            foreach (var field in x.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!FieldEquals(field.FieldType, field.GetValue(x), field.GetValue(y)))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(object x)
        {
            return 1;
        }

        private bool FieldEquals(Type fieldType, object xFieldValue, object yFieldValue)
        {
            if (xFieldValue == null) { return yFieldValue == null; }
            var fieldComparer = _source.GetComparer(fieldType);
            return fieldComparer.Equals(xFieldValue, yFieldValue);
        }
    }
}
