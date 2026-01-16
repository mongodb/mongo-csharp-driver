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

internal abstract class MethodInfoSet : IReadOnlyMethodInfoSet
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
        return hashSet.Count <= 4 ? new ArrayBasedMethodInfoSet(hashSet.ToArray()) : new HashSetBasedMethodInfoSet(hashSet);
    }

    public abstract bool Contains(MethodInfo method);

    public abstract IEnumerator<MethodInfo> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class ArrayBasedMethodInfoSet : MethodInfoSet
{
    private readonly MethodInfo[] _methods;

    public ArrayBasedMethodInfoSet(MethodInfo[] methods)
    {
        _methods = methods;
    }

    public override bool Contains(MethodInfo method)
    {
        return method.IsGenericMethod && !method.ContainsGenericParameters ?
            _methods.Contains(method.GetGenericMethodDefinition()) :
            _methods.Contains(method);
    }

    public override IEnumerator<MethodInfo> GetEnumerator() => ((IEnumerable<MethodInfo>)_methods).GetEnumerator();
}

internal sealed class HashSetBasedMethodInfoSet : MethodInfoSet
{
    private readonly HashSet<MethodInfo> _methods;

    public HashSetBasedMethodInfoSet(HashSet<MethodInfo> methods)
    {
        _methods = new HashSet<MethodInfo>(methods);
    }

    public override bool Contains(MethodInfo method)
    {
        return method.IsGenericMethod && !method.ContainsGenericParameters ?
            _methods.Contains(method.GetGenericMethodDefinition()) :
            _methods.Contains(method);
    }

    public override IEnumerator<MethodInfo> GetEnumerator() => _methods.GetEnumerator();
}
