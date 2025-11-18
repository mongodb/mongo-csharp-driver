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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection;

internal static class MemoryExtensionsMethod
{
    // private static fields
    private static readonly MethodInfo __containsWithReadOnlySpanAndValue;
    private static readonly MethodInfo __containsWithReadOnlySpanAndValueAndComparer;
    private static readonly MethodInfo __containsWithSpanAndValue;
    private static readonly MethodInfo __sequenceEqualWithReadOnlySpanAndReadOnlySpan;
    private static readonly MethodInfo __sequenceEqualWithReadOnlySpanAndReadOnlySpanAndComparer;
    private static readonly MethodInfo __sequenceEqualWithSpanAndReadOnlySpan;
    private static readonly MethodInfo __sequenceEqualWithSpanAndReadOnlySpanAndComparer;

    // static constructor
    static MemoryExtensionsMethod()
    {
        __containsWithReadOnlySpanAndValue = GetContainsWithReadOnlySpanAndValueMethod();
        __containsWithReadOnlySpanAndValueAndComparer = GetContainsWithReadOnlySpanAndValueAndComparerMethod();
        __containsWithSpanAndValue = GetContainsWithSpanAndValueMethod();
        __sequenceEqualWithReadOnlySpanAndReadOnlySpan = GetSequenceEqualWithReadOnlySpanAndReadOnlySpan();
        __sequenceEqualWithReadOnlySpanAndReadOnlySpanAndComparer = GetSequenceEqualWithReadOnlySpanAndReadOnlySpanAndComparer();
        __sequenceEqualWithSpanAndReadOnlySpan = GetSequenceEqualWithSpanAndReadOnlySpan();
        __sequenceEqualWithSpanAndReadOnlySpanAndComparer = GetSequenceEqualWithSpanAndReadOnlySpanAndComparer();
    }

    // public static properties
    public static MethodInfo ContainsWithReadOnlySpanAndValue => __containsWithReadOnlySpanAndValue;
    public static MethodInfo ContainsWithReadOnlySpanAndValueAndComparer => __containsWithReadOnlySpanAndValueAndComparer;
    public static MethodInfo ContainsWithSpanAndValue => __containsWithSpanAndValue;
    public static MethodInfo SequenceEqualWithReadOnlySpanAndReadOnlySpan => __sequenceEqualWithReadOnlySpanAndReadOnlySpan;
    public static MethodInfo SequenceEqualWithReadOnlySpanAndReadOnlySpanAndComparer => __sequenceEqualWithReadOnlySpanAndReadOnlySpanAndComparer;
    public static MethodInfo SequenceEqualWithSpanAndReadOnlySpan => __sequenceEqualWithSpanAndReadOnlySpan;
    public static MethodInfo SequenceEqualWithSpanAndReadOnlySpanAndComparer => __sequenceEqualWithSpanAndReadOnlySpanAndComparer;

    // private static methods
    private static MethodInfo GetContainsWithReadOnlySpanAndValueMethod()
    {
        return
            typeof(MemoryExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "Contains" &&
                    m.Has1GenericArgument(out var itemType) &&
                    m.Has2Parameters(out var spanParameter, out var valueParameter) &&
                    spanParameter.ParameterType.IsReadOnlySpanOf(itemType) &&
                    valueParameter.ParameterType == itemType);
    }

    private static MethodInfo GetContainsWithReadOnlySpanAndValueAndComparerMethod()
    {
        return
            typeof(MemoryExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "Contains" &&
                    m.Has1GenericArgument(out var itemType) &&
                    m.Has3Parameters(out var spanParameter, out var valueParameter, out var comparerParameter) &&
                    spanParameter.ParameterType.IsReadOnlySpanOf(itemType) &&
                    valueParameter.ParameterType == itemType &&
                    comparerParameter.ParameterType == typeof(IEqualityComparer<>).MakeGenericType(itemType));
    }

    private static MethodInfo GetContainsWithSpanAndValueMethod()
    {
        return
            typeof(MemoryExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "Contains" &&
                    m.Has1GenericArgument(out var itemType) &&
                    m.Has2Parameters(out var spanParameter, out var valueParameter) &&
                    spanParameter.ParameterType.IsSpanOf(itemType) &&
                    valueParameter.ParameterType == itemType);
    }

    private static MethodInfo GetSequenceEqualWithReadOnlySpanAndReadOnlySpan()
    {
        return
            typeof(MemoryExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "SequenceEqual" &&
                    m.Has1GenericArgument(out var itemType) &&
                    m.Has2Parameters(out var spanParameter, out var otherParameter) &&
                    spanParameter.ParameterType.IsReadOnlySpanOf(itemType) &&
                    otherParameter.ParameterType.IsReadOnlySpanOf(itemType));
    }

    private static MethodInfo GetSequenceEqualWithReadOnlySpanAndReadOnlySpanAndComparer()
    {
        return
            typeof(MemoryExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "SequenceEqual" &&
                    m.Has1GenericArgument(out var itemType) &&
                    m.Has3Parameters(out var spanParameter, out var otherParameter, out var comparerParameter) &&
                    spanParameter.ParameterType.IsReadOnlySpanOf(itemType) &&
                    otherParameter.ParameterType.IsReadOnlySpanOf(itemType) &&
                    comparerParameter.ParameterType == typeof(IEqualityComparer<>).MakeGenericType(itemType));
    }

    private static MethodInfo GetSequenceEqualWithSpanAndReadOnlySpan()
    {
        return
            typeof(MemoryExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "SequenceEqual" &&
                    m.Has1GenericArgument(out var itemType) &&
                    m.Has2Parameters(out var spanParameter, out var otherParameter) &&
                    spanParameter.ParameterType.IsSpanOf(itemType) &&
                    otherParameter.ParameterType.IsReadOnlySpanOf(itemType));
    }

    private static MethodInfo GetSequenceEqualWithSpanAndReadOnlySpanAndComparer()
    {
        return
            typeof(MemoryExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "SequenceEqual" &&
                    m.Has1GenericArgument(out var itemType) &&
                    m.Has3Parameters(out var spanParameter, out var otherParameter, out var comparerParameter) &&
                    spanParameter.ParameterType.IsSpanOf(itemType) &&
                    otherParameter.ParameterType.IsReadOnlySpanOf(itemType) &&
                    comparerParameter.ParameterType == typeof(IEqualityComparer<>).MakeGenericType(itemType));
    }
}
