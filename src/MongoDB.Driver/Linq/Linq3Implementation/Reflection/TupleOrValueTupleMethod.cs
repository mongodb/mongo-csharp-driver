/* Copyright 2010-present MongoDB Inc.
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
using System.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection;

internal static class TupleOrValueTupleMethod
{
    private static HashSet<MethodInfo> __createOverloads;

    static TupleOrValueTupleMethod()
    {
        __createOverloads =
        [
            TupleMethod.Create1,
            TupleMethod.Create2,
            TupleMethod.Create3,
            TupleMethod.Create4,
            TupleMethod.Create5,
            TupleMethod.Create6,
            TupleMethod.Create7,
            TupleMethod.Create8,
            ValueTupleMethod.Create1,
            ValueTupleMethod.Create2,
            ValueTupleMethod.Create3,
            ValueTupleMethod.Create4,
            ValueTupleMethod.Create5,
            ValueTupleMethod.Create6,
            ValueTupleMethod.Create7,
            ValueTupleMethod.Create8
        ];
    }

    public static HashSet<MethodInfo> CreateOverloads => __createOverloads;
}
