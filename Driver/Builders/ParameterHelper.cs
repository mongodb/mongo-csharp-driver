/* Copyright 2010-2011 10gen Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A helper for converting parameters.
    /// </summary>
    internal static class ParameterHelpers
    {
        #region public methods
        public static IEnumerable<BsonValue> ConvertToBsonValues(BsonValue arg1, BsonValue arg2, BsonValue[] args)
        {
            if (arg1 != null)
            {
                yield return arg1;
            }

            if (arg2 != null)
            {
                yield return arg2;
            }

            if (args == null)
            {
                yield break;
            }

            for (int i = 0; i < args.Length; ++i)
            {
                yield return args[i];
            }
        }

        public static IEnumerable<BsonValue> ConvertToBsonValues(object arg1, object arg2, object[] args)
        {
            if (arg1 != null)
            {
                yield return BsonValue.Create(arg1);
            }

            if (arg2 != null)
            {
                yield return BsonValue.Create(arg2);
            }

            if (args == null)
            {
                yield break;
            }

            for (int i = 0; i < args.Length; ++i)
            {
                object arg = args[i];

                if (arg != null)
                {
                    yield return BsonValue.Create(arg);
                }
            }
        }
        #endregion
    }
}
