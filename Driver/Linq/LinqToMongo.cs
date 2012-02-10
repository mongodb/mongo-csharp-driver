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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// This static class holds methods that can be used to express MongoDB specific query operations in LINQ queries.
    /// </summary>
    public static class LinqToMongo
    {
        /// <summary>
        /// Test whether an array in the document contains all of the supplied values (see $all).
        /// </summary>
        /// <typeparam name="TItem">The type of the array items.</typeparam>
        /// <param name="field">The field or property.</param>
        /// <param name="values">The set of values.</param>
        /// <returns></returns>
        public static bool All<TItem>(IEnumerable<TItem> field, IEnumerable<TItem> values)
        {
            throw new InvalidOperationException("The LinqToMongo.All method is only intended to be used in LINQ Where clauses.");
        }

        /// <summary>
        /// Test whether a field exists in the document.
        /// </summary>
        /// <typeparam name="TField">The type of the field or property.</typeparam>
        /// <param name="field">The field or property.</param>
        /// <param name="exists">Whether to test for the presence or absence of the field.</param>
        /// <returns></returns>
        public static bool Exists<TField>(TField field, bool exists)
        {
            throw new InvalidOperationException("The LinqToMongo.Exists method is only intended to be used in LINQ Where clauses.");
        }

        /// <summary>
        /// Test whether an array in the document contains at least one of the provided set of values.
        /// </summary>
        /// <typeparam name="TItem">The type of the array items.</typeparam>
        /// <param name="field">The field or property.</param>
        /// <param name="values">The set of values.</param>
        /// <returns></returns>
        public static bool In<TItem>(IEnumerable<TItem> field, IEnumerable<TItem> values)
        {
            throw new InvalidOperationException("The LinqToMongo.In method is only intended to be used in LINQ Where clauses.");
        }

        /// <summary>
        /// Test whether a field in the document is equal to one of the provided set of values.
        /// </summary>
        /// <typeparam name="TField">The type of the field or property.</typeparam>
        /// <param name="field">The field or property.</param>
        /// <param name="values">The set of values.</param>
        /// <returns></returns>
        public static bool In<TField>(TField field, IEnumerable<TField> values)
        {
            throw new InvalidOperationException("The LinqToMongo.In method is only intended to be used in LINQ Where clauses.");
        }
    }
}
