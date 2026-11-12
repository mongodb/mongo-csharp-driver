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
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers;

// Regression tests for CSHARP-6169: the default allow-list treated any compiler-generated generic type
// whose name contains "Anon" as an anonymous type.
public class ObjectSerializerAnonymousTypeAllowListTests
{
    [Fact]
    public void IsAnonymousType_should_be_true_for_real_anonymous_types()
    {
        var one = new { X = 1 };
        var many = new { A = 1, B = "b", C = new { D = 2.0 } };

        one.GetType().IsAnonymousType().Should().BeTrue();
        many.GetType().IsAnonymousType().Should().BeTrue();
        new BsonClassMap(many.GetType()).IsAnonymous.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_should_create_real_anonymous_type_named_by_discriminator()
    {
        var type = new { A = 1, B = "b" }.GetType();
        var json = $"{{ _t : '{type.AssemblyQualifiedName}', A : 7, B : 'x' }}";

        var result = DeserializeWithDefaultAllowedTypes(json);

        result.Should().Be(new { A = 7, B = "x" });
    }

    [Fact]
    public void DefaultAllowedTypes_should_not_allow_open_anonymous_type_definition()
    {
        var definition = new { X = 1 }.GetType().GetGenericTypeDefinition();

        ObjectSerializer.DefaultAllowedTypes(definition).Should().BeFalse();
    }

    [Fact]
    public void Deserialize_should_not_reach_declaring_class_through_async_state_machine()
    {
        // the state machine's <>4__this field would otherwise deserialize a DeclaringClass, and through it a NotAllowListed
        ObjectSerializer.DefaultAllowedTypes(typeof(NotAllowListed)).Should().BeFalse();
        var type = GetStateMachineType(typeof(DeclaringClass), nameof(DeclaringClass.AnonWorkAsync)).MakeGenericType(typeof(int));
        var json = $"{{ _t : '{type.AssemblyQualifiedName}', '<>4__this' : {{ Settings : {{ Secret : 'attacker' }} }} }}";

        var exception = Record.Exception(() => DeserializeWithDefaultAllowedTypes(json));

        exception.Should().BeOfType<BsonSerializationException>();
        exception.Message.Should().Contain("is not configured as a type that is allowed to be deserialized");
    }

    [Theory]
    [InlineData("iterator")]
    [InlineData("async")]
    [InlineData("anonymous delegate")]
    public void IsAnonymousType_should_be_false_for_other_compiler_generated_generic_types_with_Anon_in_name(string kind)
    {
        var type = kind switch
        {
            "iterator" => GetStateMachineType(typeof(ObjectSerializerAnonymousTypeAllowListTests), nameof(AnonIterator)).MakeGenericType(typeof(int)),
            "async" => GetStateMachineType(typeof(DeclaringClass), nameof(DeclaringClass.AnonWorkAsync)).MakeGenericType(typeof(int)),
            "anonymous delegate" => GetAnonymousDelegateType(),
            _ => throw new ArgumentException(kind)
        };

        type.IsAnonymousType().Should().BeFalse();
        new BsonClassMap(type).IsAnonymous.Should().BeFalse();
        ObjectSerializer.DefaultAllowedTypes(type).Should().BeFalse();
    }

    // Real types from these compilers were verified separately; these imitate the parts of their shape that the check looks at.
    [Theory]
    [InlineData("<>f__AnonymousType0", Variant.None)] // C#
    [InlineData("<mymodule>f__AnonymousType0", Variant.None)] // C# compiled as a netmodule
    [InlineData("<>f__AnonymousType3361913667", Variant.ImplementsInterface)] // F# anonymous record
    [InlineData("<>f__AnonymousType1000371791", Variant.ValueType)] // F# struct anonymous record
    [InlineData("VB$AnonymousType_0", Variant.PropertySetter)] // VB non-key property
    [InlineData("<>__AnonType0", Variant.None)] // Mono mcs
    public void IsAnonymousType_should_be_true_for_anonymous_type_shapes(string name, Variant variant)
    {
        var type = CreateLookalike(name, variant).MakeGenericType(typeof(int));

        type.IsAnonymousType().Should().BeTrue();
        ObjectSerializer.DefaultAllowedTypes(type).Should().BeTrue();
    }

    [Theory]
    [InlineData("<>f__AnonymousType0", Variant.NotCompilerGenerated)]
    [InlineData("<>f__AnonymousType0", Variant.FieldNotTypedByGenericParameter)]
    [InlineData("<>f__AnonymousType0", Variant.PropertyNotTypedByGenericParameter)]
    [InlineData("<>f__AnonymousType0", Variant.BaseClass)]
    [InlineData("<>f__AnonymousDelegate0", Variant.None)]
    [InlineData("<Anon>d__0", Variant.None)]
    [InlineData("VB$StateMachine_1_Anon", Variant.None)]
    [InlineData("Anon>f__AnonymousType0", Variant.None)]
    public void IsAnonymousType_should_be_false_for_types_that_are_not_anonymous_type_shapes(string name, Variant variant)
    {
        var type = CreateLookalike(name, variant).MakeGenericType(typeof(int));

        type.IsAnonymousType().Should().BeFalse();
        ObjectSerializer.DefaultAllowedTypes(type).Should().BeFalse();
    }

    public enum Variant
    {
        None,
        PropertySetter,
        ImplementsInterface,
        ValueType,
        NotCompilerGenerated,
        FieldNotTypedByGenericParameter,
        PropertyNotTypedByGenericParameter,
        BaseClass
    }

    public class NotAllowListed
    {
        public string Secret { get; set; }
    }

    public class DeclaringClass
    {
        public NotAllowListed Settings { get; set; }

        public async Task<T> AnonWorkAsync<T>(T value)
        {
            await Task.Yield();
            return value;
        }
    }

    public class LookalikeBase
    {
        public NotAllowListed Data;
    }

    // the registered object serializer can't be used because RegisterObjectSerializerFixture replaces it with one
    // that also allows every type in a MongoDB namespace, which includes the types declared in this test
    private static object DeserializeWithDefaultAllowedTypes(string json)
    {
        var subject = new ObjectSerializer(ObjectSerializer.DefaultAllowedTypes);
        using var reader = new JsonReader(json);
        return subject.Deserialize(BsonDeserializationContext.CreateRoot(reader));
    }

    private static IEnumerable<T> AnonIterator<T>(T value)
    {
        yield return value;
    }

    private static Type GetStateMachineType(Type declaringType, string methodName)
    {
        return declaringType
            .GetNestedTypes(BindingFlags.NonPublic)
            .Single(t => t.Name.StartsWith("<" + methodName + ">", StringComparison.Ordinal) && t.IsGenericTypeDefinition);
    }

    // a lambda with a params parameter has no Func/Action equivalent, so the compiler synthesizes <>f__AnonymousDelegateN
    private static Type GetAnonymousDelegateType()
    {
        var lambda = (int first, params int[] rest) => first;
        var type = lambda.GetType();
        type.Name.Should().StartWith("<>f__AnonymousDelegate");
        return type;
    }

    // Emits a generic type with one generic parameter T, a backing field and a get-only property of type T, then applies the variant.
    private static Type CreateLookalike(string name, Variant variant)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("Lookalike" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("module");

        var attributes = TypeAttributes.Public | TypeAttributes.Sealed;
        var baseType = variant switch
        {
            Variant.ValueType => typeof(ValueType),
            Variant.BaseClass => typeof(LookalikeBase),
            _ => typeof(object)
        };
        if (variant == Variant.ValueType)
        {
            attributes |= TypeAttributes.SequentialLayout;
        }
        var type = module.DefineType(name + "`1", attributes, baseType);
        var t = type.DefineGenericParameters("T")[0];
        if (variant != Variant.NotCompilerGenerated)
        {
            type.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), new object[0]));
        }

        var fieldType = variant == Variant.FieldNotTypedByGenericParameter ? typeof(NotAllowListed) : t;
        var field = type.DefineField("<X>i__Field", fieldType, FieldAttributes.Private);

        var propertyType = variant == Variant.PropertyNotTypedByGenericParameter ? typeof(NotAllowListed) : t;
        var property = type.DefineProperty("X", PropertyAttributes.None, propertyType, null);
        var getter = type.DefineMethod("get_X", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
        var il = getter.GetILGenerator();
        if (propertyType == fieldType)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
        }
        else
        {
            il.Emit(OpCodes.Ldnull);
        }
        il.Emit(OpCodes.Ret);
        property.SetGetMethod(getter);

        switch (variant)
        {
            case Variant.PropertySetter:
                var setter = type.DefineMethod("set_X", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), new Type[] { t });
                il = setter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ret);
                property.SetSetMethod(setter);
                break;
            case Variant.ImplementsInterface:
                type.AddInterfaceImplementation(typeof(IComparable));
                var compareTo = type.DefineMethod(
                    "CompareTo",
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                    typeof(int),
                    new Type[] { typeof(object) });
                il = compareTo.GetILGenerator();
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ret);
                break;
        }

        return type.CreateType();
    }
}
