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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Linq3.Methods
{
    public static class MongoQueryableMethod
    {
        // private static fields
        private static readonly MethodInfo __anyAsync;
        private static readonly MethodInfo __anyWithPredicateAsync;

        // static constructor
        static MongoQueryableMethod()
        {
            __anyAsync = new Func<IQueryable<object>, CancellationToken, Task<bool>>(MongoQueryable.AnyAsync).Method.GetGenericMethodDefinition();
            __anyWithPredicateAsync = new Func<IQueryable<object>, Expression<Func<object, bool>>, CancellationToken, Task<bool>>(MongoQueryable.AnyAsync).Method.GetGenericMethodDefinition();
        }

        // public properties
        public static MethodInfo AnyAsync => __anyAsync;
        public static MethodInfo AnyWithPredicateAsync => __anyWithPredicateAsync;
    }
}
