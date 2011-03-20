/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.Bson.Serialization.Conventions {
    /// <summary>
    /// Represents a set of conventions.
    /// </summary>
    public sealed class ConventionProfile {
        #region public properties
        /// <summary>
        /// Gets the Id generator convention.
        /// </summary>
        public IIdGeneratorConvention IdGeneratorConvention { get; private set; }

        /// <summary>
        /// Gets the default value convention.
        /// </summary>
        public IDefaultValueConvention DefaultValueConvention { get; private set; }

        /// <summary>
        /// Gets the element name convention.
        /// </summary>
        public IElementNameConvention ElementNameConvention { get; private set; }

        /// <summary>
        /// Gets the extra elements member convention.
        /// </summary>
        public IExtraElementsMemberConvention ExtraElementsMemberConvention { get; private set; }

        /// <summary>
        /// Gets the Id member convention.
        /// </summary>
        public IIdMemberConvention IdMemberConvention { get; private set; }

        /// <summary>
        /// Gets the ignore extra elements convention.
        /// </summary>
        public IIgnoreExtraElementsConvention IgnoreExtraElementsConvention { get; private set; }

        /// <summary>
        /// Gets the ignore if null convention.
        /// </summary>
        public IIgnoreIfNullConvention IgnoreIfNullConvention { get; private set; }

        /// <summary>
        /// Gets the member finder convention.
        /// </summary>
        public IMemberFinderConvention MemberFinderConvention { get; private set; }

        /// <summary>
        /// Gets the default value convention.
        /// </summary>
        public ISerializeDefaultValueConvention SerializeDefaultValueConvention { get; private set; }
        #endregion

        #region public static methods
        /// <summary>
        /// Gets the default convention profile.
        /// </summary>
        /// <returns>The default convention profile.</returns>
        public static ConventionProfile GetDefault() {
            return new ConventionProfile() // The default profile always matches...
                .SetIdGeneratorConvention(new LookupIdGeneratorConvention())
                .SetDefaultValueConvention(new NullDefaultValueConvention())
                .SetElementNameConvention(new MemberNameElementNameConvention())
                .SetExtraElementsMemberConvention(new NamedExtraElementsMemberConvention("ExtraElements"))
                .SetIdMemberConvention(new NamedIdMemberConvention("Id", "id", "_id"))
                .SetIgnoreExtraElementsConvention(new NeverIgnoreExtraElementsConvention())
                .SetIgnoreIfNullConvention(new NeverIgnoreIfNullConvention())
                .SetMemberFinderConvention(new PublicMemberFinderConvention())
                .SetSerializeDefaultValueConvention(new AlwaysSerializeDefaultValueConvention());
        }
        #endregion

        #region public methods
        /// <summary>
        /// Merges another convention profile into this one (only missing conventions are merged).
        /// </summary>
        /// <param name="other">The other convention profile.</param>
        public void Merge(
            ConventionProfile other
        ) {
            if (IdGeneratorConvention == null) {
                IdGeneratorConvention = other.IdGeneratorConvention;
            }
            if (DefaultValueConvention == null) {
                DefaultValueConvention = other.DefaultValueConvention;
            }
            if (ElementNameConvention == null) {
                ElementNameConvention = other.ElementNameConvention;
            }
            if (ExtraElementsMemberConvention == null) {
                ExtraElementsMemberConvention = other.ExtraElementsMemberConvention;
            }
            if (IdMemberConvention == null) {
                IdMemberConvention = other.IdMemberConvention;
            }
            if (IgnoreExtraElementsConvention == null) {
                IgnoreExtraElementsConvention = other.IgnoreExtraElementsConvention;
            }
            if (IgnoreIfNullConvention == null) {
                IgnoreIfNullConvention = other.IgnoreIfNullConvention;
            }
            if(MemberFinderConvention == null) {
                MemberFinderConvention = other.MemberFinderConvention;
            }
            if (SerializeDefaultValueConvention == null) {
                SerializeDefaultValueConvention = other.SerializeDefaultValueConvention;
            }
        }

        /// <summary>
        /// Sets the Id generator convention.
        /// </summary>
        /// <param name="convention">An Id generator convention.</param>
        /// <returns>The convention profile.</returns>
        public ConventionProfile SetIdGeneratorConvention(
            IIdGeneratorConvention convention
        ) {
            IdGeneratorConvention = convention;
            return this;
        }

        /// <summary>
        /// Sets the default value convention.
        /// </summary>
        /// <param name="convention">A default value convention.</param>
        /// <returns>The convention profile.</returns>
        public ConventionProfile SetDefaultValueConvention(
            IDefaultValueConvention convention
        ) {
            DefaultValueConvention = convention;
            return this;
        }

        /// <summary>
        /// Sets the element name convention.
        /// </summary>
        /// <param name="convention">An element name convention.</param>
        /// <returns>The convention profile.</returns>
        public ConventionProfile SetElementNameConvention(
            IElementNameConvention convention
        ) {
            ElementNameConvention = convention;
            return this;
        }

        /// <summary>
        /// Sets the extra elements member convention.
        /// </summary>
        /// <param name="convention">An extra elements member convention.</param>
        /// <returns>The convention profile.</returns>
        public ConventionProfile SetExtraElementsMemberConvention(
            IExtraElementsMemberConvention convention
        ) {
            ExtraElementsMemberConvention = convention;
            return this;
        }

        /// <summary>
        /// Sets the Id member convention.
        /// </summary>
        /// <param name="convention">An Id member convention.</param>
        /// <returns>The convention profile.</returns>
        public ConventionProfile SetIdMemberConvention(
            IIdMemberConvention convention
        ) {
            IdMemberConvention = convention;
            return this;
        }

        /// <summary>
        /// Sets the ignore extra elements convention.
        /// </summary>
        /// <param name="convention">An ignore extra elements convention.</param>
        /// <returns>The convention profile.</returns>
        public ConventionProfile SetIgnoreExtraElementsConvention(
            IIgnoreExtraElementsConvention convention
        ) {
            IgnoreExtraElementsConvention = convention;
            return this;
        }

        /// <summary>
        /// Sets the ignore if null convention.
        /// </summary>
        /// <param name="convention">An ignore if null convention.</param>
        /// <returns>The convention profile.</returns>
        public ConventionProfile SetIgnoreIfNullConvention(
            IIgnoreIfNullConvention convention
        ) {
            IgnoreIfNullConvention = convention;
            return this;
        }

        /// <summary>
        /// Sets the member finder convention.
        /// </summary>
        /// <param name="convention">A member finder convention.</param>
        /// <returns>The convention profile.</returns>
        public ConventionProfile SetMemberFinderConvention(
            IMemberFinderConvention convention
        ) {
            MemberFinderConvention = convention;
            return this;
        }

        /// <summary>
        /// Sets the serialize default value convention.
        /// </summary>
        /// <param name="convention">A serialize default value convention.</param>
        /// <returns>The convention profile.</returns>
        public ConventionProfile SetSerializeDefaultValueConvention(
            ISerializeDefaultValueConvention convention
        ) {
            SerializeDefaultValueConvention = convention;
            return this;
        }
        #endregion
    }
}
