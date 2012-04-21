/* Copyright 2010-2012 10gen Inc.
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
using System.Reflection;
using System.Threading;

namespace MongoDB.Bson
{
    /// <summary>
    /// Fast singleton abstract base class. Enables runtime, static, and lazy binding.
    /// </summary>
    /// <typeparam name="TValue">The singleton value type.</typeparam>
    internal abstract class FastSingleton<TValue> where TValue : class
    {
        // private static fields
        private static ReaderWriterLockSlim __readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static Dictionary<Type, FastSingleton<TValue>> __dictionary = new Dictionary<Type, FastSingleton<TValue>>();

        // public properties
        /// <summary>
        /// Gets the singleton value.
        /// </summary>
        public abstract TValue Value
        {
            get;
        }

        /// <summary>
        /// Gets An optional user-defined object that contains information about the singleton value.
        /// </summary>
        public abstract object State
        {
            get;
        }

        // public static methods
        /// <summary>
        /// Resolves a nominal type to a singleton instance.
        /// </summary>
        /// <param name="nominalType">The nominal type.</param>
        /// <returns>A singleton instance.</returns>
        public static FastSingleton<TValue> Lookup(Type nominalType)
        {
            FastSingleton<TValue> value;

            __readerWriterLock.EnterUpgradeableReadLock();
            try
            {
                if (__dictionary.TryGetValue(nominalType, out value))
                {
                    return value;
                }

                __readerWriterLock.EnterWriteLock();
                try
                {
                    if (__dictionary.TryGetValue(nominalType, out value))
                    {
                        return value;
                    }

                    var genericType = typeof(FastSingleton<,>).MakeGenericType(typeof(TValue), nominalType);

                    var instancePropertyInfo = genericType.GetProperty(
                        "Instance",
                        BindingFlags.Static | BindingFlags.Public);

                    value = (FastSingleton<TValue>)instancePropertyInfo.GetValue(null, null);

                    __dictionary.Add(nominalType, value);

                    return value;
                }
                finally
                {
                    __readerWriterLock.ExitWriteLock();
                }
            }
            finally
            {
                __readerWriterLock.ExitUpgradeableReadLock();
            }
        }

        // public methods
        /// <summary>
        /// Sets the singleton value.
        /// </summary>
        /// <param name="value">The singleton value</param>
        /// <param name="state">An optional user-defined object that contains information about the singleton value.</param>
        /// <remarks>Function is atomic.</remarks>
        /// <returns>true if the singleton value was set; otherwise false.</returns>
        public abstract bool TrySetValue(TValue value, object state);

        // protected classes
        /// <summary>
        /// Groups a singleton value and user-defined object so they can be set atomically.
        /// </summary>
        protected class ValueStateTuple
        {
            // private fields
            private readonly TValue value;
            private readonly object state;

            // constructors
            /// <summary>
            /// Initializes a new instance of the Tuple class.
            /// </summary>
            /// <param name="value">The singleton value</param>
            /// <param name="state">An optional user-defined object that contains information about the singleton value.</param>
            public ValueStateTuple(TValue value, object state)
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this.value = value;
                this.state = state;
            }

            // public properties
            /// <summary>
            /// Gets the singleton value.
            /// </summary>
            public TValue Value
            {
                get
                {
                    return this.value;
                }
            }

            /// <summary>
            /// Gets an optional user-defined object that contains information about the singleton value.
            /// </summary>
            public object State
            {
                get
                {
                    return this.state;
                }
            }
        }
    }

    /// <summary>
    /// Fast singleton implementation class
    /// </summary>
    /// <typeparam name="TValue">The singleton value type.</typeparam>
    /// <typeparam name="TNominal">The nominal type associated with the singleton value.</typeparam>
    internal sealed class FastSingleton<TValue, TNominal> : FastSingleton<TValue> where TValue : class
    {
        // private static fields
        private static readonly FastSingleton<TValue, TNominal> __instance = new FastSingleton<TValue, TNominal>();
        private static ValueStateTuple __tuple; // volatile accesses

        // constructors
        /// <summary>
        /// Initializes a new instance of the FastSingleton&lt;TValue, TNominal&gt; class.
        /// </summary>
        /// <remarks>Private because FastSingleton&lt;TValue, TNominal&gt; is a singleton.</remarks>
        private FastSingleton()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the FastSingleton&lt;TValue, TNominal&gt; type.
        /// </summary>
        /// <remarks>Accessed by FastSingleton.Create</remarks>
        public static FastSingleton<TValue, TNominal> Instance
        {
            get
            {
                return __instance;
            }
        }

        /// <summary>
        /// Gets the singleton value.
        /// </summary>
        public override TValue Value
        {
            get
            {
                // see implementation of Thread.VolatileRead();
                var tuple = __tuple;

                Thread.MemoryBarrier();

                if (tuple == null)
                {
                    return null;
                }

                return tuple.Value;
            }
        }

        /// <summary>
        /// Gets An optional user-defined object that contains information about the singleton value.
        /// </summary>
        public override object State
        {
            get
            {
                // see implementation of Thread.VolatileRead();
                var tuple = __tuple;

                Thread.MemoryBarrier();

                if (tuple == null)
                {
                    return null;
                }

                return tuple.State;
            }
        }

        /// <summary>
        /// Sets the singleton value.
        /// </summary>
        /// <param name="value">The singleton value</param>
        /// <param name="State">An optional user-defined object that contains information about the singleton value.</param>
        /// <remarks>Function is atomic.</remarks>
        /// <returns>true if the singleton value was set; otherwise false.</returns>
        public override bool TrySetValue(TValue value, object State)
        {
            var tuple = new ValueStateTuple(value, State);

            var originalTuple = Interlocked.CompareExchange(ref __tuple, tuple, null);

            return originalTuple == null;
        }
    }
}
