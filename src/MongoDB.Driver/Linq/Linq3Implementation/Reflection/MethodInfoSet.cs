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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection;

internal interface IReadOnlyMethodInfoSet : IEnumerable<MethodInfo>
{
    bool Contains(MethodInfo method);
}

internal class MethodInfoSet : IReadOnlyMethodInfoSet
{
    public static IReadOnlyMethodInfoSet Create(IEnumerable<MethodInfo> methods)
    {
        var hashSet = new HashSet<MethodInfo>();
        hashSet.UnionWith(methods.Where(m => m != null));
        return Create(hashSet);
    }

    public static IReadOnlyMethodInfoSet Create(IEnumerable<IEnumerable<MethodInfo>> methodSets)
    {
        var hashSet = new HashSet<MethodInfo>();

        foreach (var methodSet in methodSets)
        {
           hashSet.UnionWith(methodSet.Where(m => m != null));
        }

        return Create(hashSet);
    }

    private static IReadOnlyMethodInfoSet Create(HashSet<MethodInfo> hashSet)
    {
        return hashSet.Count <= 4 ? new MethodInfoSet(hashSet.ToArray()) : new MethodInfoSet(hashSet);
    }

    private readonly ICollection<MethodInfo> _methods;

    public MethodInfoSet(ICollection<MethodInfo> methods)
    {
        _methods = methods;
    }

    public bool Contains(MethodInfo method)
    {
        return method.IsGenericMethod && !method.ContainsGenericParameters ?
            _methods.Contains(method.GetGenericMethodDefinition()) :
            _methods.Contains(method);
    }

    public IEnumerator<MethodInfo> GetEnumerator() => _methods.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
