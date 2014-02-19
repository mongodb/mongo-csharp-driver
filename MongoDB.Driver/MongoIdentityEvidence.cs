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
using System.Text;

namespace MongoDB.Driver
{
    /// <summary>
    /// Evidence used as proof of a MongoIdentity.
    /// </summary>
    public abstract class MongoIdentityEvidence
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoIdentityEvidence" /> class.
        /// </summary>
        internal MongoIdentityEvidence()
        { }

        // public operators
        /// <summary>
        /// Compares two MongoIdentityEvidences.
        /// </summary>
        /// <param name="lhs">The first MongoIdentityEvidence.</param>
        /// <param name="rhs">The other MongoIdentityEvidence.</param>
        /// <returns>True if the two MongoIdentityEvidences are equal (or both null).</returns>
        public static bool operator ==(MongoIdentityEvidence lhs, MongoIdentityEvidence rhs)
        {
            return object.Equals(lhs, rhs);
        }

        /// <summary>
        /// Compares two MongoIdentityEvidences.
        /// </summary>
        /// <param name="lhs">The first MongoIdentityEvidence.</param>
        /// <param name="rhs">The other MongoIdentityEvidence.</param>
        /// <returns>True if the two MongoIdentityEvidences are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(MongoIdentityEvidence lhs, MongoIdentityEvidence rhs)
        {
            return !(lhs == rhs);
        }

        // public methods
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.NotImplementedException">Subclasses of MongoIdentityEvidence must override Equals.</exception>
        public override bool Equals(object obj)
        {
            throw new NotImplementedException("Subclasses of MongoIdentityEvidence must override Equals.");
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        /// <exception cref="System.NotImplementedException">Subclasses of MongoIdentityEvidence must override Equals.</exception>
        public override int GetHashCode()
        {
            throw new NotImplementedException("Subclasses of MongoIdentityEvidence must override GetHashCode.");
        }
    }
}
