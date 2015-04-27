/* Copyright 2010-2014 MongoDB Inc.
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
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Provides functionality to evaluate queries against MongoDB.
    /// </summary>
    public interface IMongoQueryable : IQueryable
    {
        /// <summary>
        /// Builds an execution model for the query.
        /// </summary>
        /// <returns>An execution model.</returns>
        QueryableExecutionModel BuildExecutionModel();
    }

    /// <summary>
    /// Provides functionality to evaluate queries against MongoDB
    /// wherein the type of the data is known.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the data in the data source.
    /// This type parameter is covariant.
    /// That is, you can use either the type you specified or any type that is more
    /// derived. For more information about covariance and contravariance, see Covariance
    /// and Contravariance in Generics.
    /// </typeparam>
    public interface IMongoQueryable<T> : IMongoQueryable, IQueryable<T>, IAsyncCursorSource<T>
    {

    }

    /// <summary>
    /// Represents the result of a sorting operation.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the data in the data source.
    /// This type parameter is covariant.
    /// That is, you can use either the type you specified or any type that is more
    /// derived. For more information about covariance and contravariance, see Covariance
    /// and Contravariance in Generics.
    /// </typeparam>
    public interface IOrderedMongoQueryable<T> : IMongoQueryable<T>, IOrderedQueryable<T>
    {

    }
}
