/*
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

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// A class map convention that wraps a delegate.
    /// </summary>
    public class CreatorClassMapConvention : ConventionBase, IClassMapConvention
    {
        // private fields
        private readonly Func<Type, Func<object>> _creator;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatorClassMapConvention" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="creator">The creator.</param>
        public CreatorClassMapConvention(string name, Func<Type, Func<object>> creator)
            : base(name)
        {
            if (creator == null)
            {
                throw new ArgumentNullException("creator");
            }
            _creator = creator;
        }

        // public methods
        /// <summary>
        /// Applies a modification to the class map.
        /// </summary>
        /// <param name="classMap">The class map.</param>
        public void Apply(BsonClassMap classMap)
        {
            classMap.SetCreator(_creator(classMap.ClassType));
        }
    }
}
