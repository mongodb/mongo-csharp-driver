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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for a class that implements IEnumerable.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class EnumerableInterfaceImplementerSerializer<TValue> :
        EnumerableInterfaceImplementerSerializerBase<TValue>,
        IChildSerializerConfigurable
            where TValue : class, IList, new()
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableInterfaceImplementerSerializer{TValue}"/> class.
        /// </summary>
        public EnumerableInterfaceImplementerSerializer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableInterfaceImplementerSerializer{TValue}"/> class.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        public EnumerableInterfaceImplementerSerializer(IBsonSerializer itemSerializer)
            : base(itemSerializer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableInterfaceImplementerSerializer{TValue}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public EnumerableInterfaceImplementerSerializer(IBsonSerializerRegistry serializerRegistry)
            : base(serializerRegistry)
        {
        }

        // public methods
        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified item serializer.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        public EnumerableInterfaceImplementerSerializer<TValue> WithItemSerializer(IBsonSerializer itemSerializer)
        {
            return new EnumerableInterfaceImplementerSerializer<TValue>(itemSerializer);
        }

        // protected methods
        /// <summary>
        /// Creates the accumulator.
        /// </summary>
        /// <returns>The accumulator.</returns>
        protected override object CreateAccumulator()
        {
            return new TValue();
        }

        // explicit interface implementations
        IBsonSerializer IChildSerializerConfigurable.ChildSerializer
        {
            get { return ItemSerializer; }
        }

        IBsonSerializer IChildSerializerConfigurable.WithChildSerializer(IBsonSerializer childSerializer)
        {
            return WithItemSerializer(childSerializer);
        }
    }

    /// <summary>
    /// Represents a serializer for a class that implementes <see cref="IEnumerable{TItem}"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public class EnumerableInterfaceImplementerSerializer<TValue, TItem> :
        EnumerableInterfaceImplementerSerializerBase<TValue, TItem>,
        IChildSerializerConfigurable
            where TValue : class, IEnumerable<TItem>
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableInterfaceImplementerSerializer{TValue, TItem}"/> class.
        /// </summary>
        public EnumerableInterfaceImplementerSerializer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableInterfaceImplementerSerializer{TValue, TItem}"/> class.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        public EnumerableInterfaceImplementerSerializer(IBsonSerializer<TItem> itemSerializer)
            : base(itemSerializer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableInterfaceImplementerSerializer{TValue, TItem}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public EnumerableInterfaceImplementerSerializer(IBsonSerializerRegistry serializerRegistry)
            : base(serializerRegistry)
        {
        }

        // public methods
        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified item serializer.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        public EnumerableInterfaceImplementerSerializer<TValue, TItem> WithItemSerializer(IBsonSerializer<TItem> itemSerializer)
        {
            return new EnumerableInterfaceImplementerSerializer<TValue, TItem>(itemSerializer);
        }

        // protected methods
        /// <summary>
        /// Creates the accumulator.
        /// </summary>
        /// <returns>The accumulator.</returns>
        protected override object CreateAccumulator()
        {
            return new List<TItem>();
        }

        Func<object, TValue> _finalizeResult = null;
        /// <summary>
        /// Finalizes the result.
        /// </summary>
        /// <param name="accumulator">The accumulator.</param>
        /// <returns>The final result.</returns>
        protected override TValue FinalizeResult(object accumulator)
        {
            if (_finalizeResult == null)
                _finalizeResult = BuildFinalizeResultAction(accumulator);
            return _finalizeResult(accumulator);
        }
        Func<object, TValue> BuildFinalizeResultAction(object accumulator)
        {
            
            // if it the same type we can just convert and return
            var accumulatorType = accumulator.GetType();
            if (typeof(TValue).IsAssignableFrom(accumulatorType))
                return (accumulator) => (TValue)accumulator;

            // find and call a constructor that we can pass the accumulator to
            var constructors = typeof(TValue).GetTypeInfo().GetConstructors();
            var enumerableConstructor = constructors.FirstOrDefault(constructorInfo =>
            {
                var parameterInfos = constructorInfo.GetParameters();
                return parameterInfos.Length == 1 && parameterInfos[0].ParameterType.GetTypeInfo().IsAssignableFrom(accumulatorType);
            });
            if (enumerableConstructor != null)
            {
                //func parameter
                var accumulatorParameter = Expression.Parameter(typeof(object), "accumulator");
                var convertedAccumulator = Expression.ConvertChecked(accumulatorParameter, accumulatorType);
                return Expression.Lambda<Func<object, TValue>>(
                    Expression.New(enumerableConstructor, convertedAccumulator),
                    accumulatorParameter
                ).Compile();
            }

            // otherwise try to find a no-argument constructor and an Add method
            var valueTypeInfo = typeof(TValue).GetTypeInfo();
            var noArgumentConstructorInfo = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
            var addMethodInfo = valueTypeInfo.GetMethod("Add", new Type[] { typeof(TItem) });

            if (noArgumentConstructorInfo != null && addMethodInfo != null)
            {
                //slow, reflection- it is very rare case
                return (accumulator) =>
                {
                    var value = (TValue)noArgumentConstructorInfo.Invoke(new Type[] { });
                    foreach (var item in (IEnumerable<TItem>)accumulator)
                    {
                        addMethodInfo.Invoke(value, new object[] { item! });
                    }
                    return value;
                };
            }

            var message = string.Format("Type '{0}' does not have a suitable constructor or Add method.", typeof(TValue).FullName);
            throw new BsonSerializationException(message);

        }


        // explicit interface implementations
        IBsonSerializer IChildSerializerConfigurable.ChildSerializer
        {
            get { return ItemSerializer; }
        }

        IBsonSerializer IChildSerializerConfigurable.WithChildSerializer(IBsonSerializer childSerializer)
        {
            return WithItemSerializer((IBsonSerializer<TItem>)childSerializer);
        }
    }
}
