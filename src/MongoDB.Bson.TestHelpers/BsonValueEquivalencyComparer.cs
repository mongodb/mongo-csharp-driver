/* Copyright 2015 MongoDB Inc.
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

namespace MongoDB.Bson.TestHelpers
{
    public class BsonValueEquivalencyComparer
    {
        public static bool Compare(BsonValue a, BsonValue b)
        {
            if (a.BsonType == BsonType.Document && b.BsonType == BsonType.Document)
            {
                return CompareDocuments((BsonDocument)a, (BsonDocument)b);
            }
            else if (a.BsonType == BsonType.Array && b.BsonType == BsonType.Array)
            {
                return CompareArrays((BsonArray)a, (BsonArray)b);
            }
            else if (a.BsonType == b.BsonType)
            {
                return a.Equals(b);
            }
            else if (IsNumber(a) && IsNumber(b))
            {
                return a.ToDouble() == b.ToDouble();
            }
            else if (CouldBeBoolean(a) && CouldBeBoolean(b))
            {
                return a.ToBoolean() == b.ToBoolean();
            }
            else
            {
                return false;
            }
        }

        private static bool CompareArrays(BsonArray a, BsonArray b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                if (!Compare(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompareDocuments(BsonDocument a, BsonDocument b)
        {
            if (a.ElementCount != b.ElementCount)
            {
                return false;
            }

            foreach (var aElement in a)
            {
                BsonElement bElement;
                if (!b.TryGetElement(aElement.Name, out bElement))
                {
                    return false;
                }

                if (!Compare(aElement.Value, bElement.Value))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CouldBeBoolean(BsonValue value)
        {
            switch (value.BsonType)
            {
                case BsonType.Boolean:
                    return true;
                case BsonType.Double:
                case BsonType.Int32:
                case BsonType.Int64:
                    var numericValue = value.ToDouble();
                    return numericValue == 0.0 || numericValue == 1.0;
                default:
                    return false;
            }
        }

        private static bool IsNumber(BsonValue value)
        {
            switch (value.BsonType)
            {
                case BsonType.Double:
                case BsonType.Int32:
                case BsonType.Int64:
                    return true;
                default:
                    return false;
            }
        }
    }
}
