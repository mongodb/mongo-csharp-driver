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

namespace MongoDB.Bson.DefaultSerializer.Conventions {
    public sealed class ConventionProfile {
        #region public properties
        public IIdGeneratorConvention IdGeneratorConvention { get; private set; }

        public IDefaultValueConvention DefaultValueConvention { get; private set; }

        public IElementNameConvention ElementNameConvention { get; private set; }

        public IExtraElementsMemberConvention ExtraElementsMemberConvention { get; private set; }

        public IIdMemberConvention IdMemberConvention { get; private set; }

        public IIgnoreExtraElementsConvention IgnoreExtraElementsConvention { get; private set; }

        public IIgnoreIfNullConvention IgnoreIfNullConvention { get; private set; }

        public IMemberFinderConvention MemberFinderConvention { get; private set; }

        public ISerializeDefaultValueConvention SerializeDefaultValueConvention { get; private set; }
        #endregion

        #region public static methods
        public static ConventionProfile GetDefault() {
            return new ConventionProfile() // The default profile always matches...
                .SetIdGeneratorConvention(new LookupIdGeneratorConvention())
                .SetDefaultValueConvention(new NullDefaultValueConvention())
                .SetElementNameConvention(new MemberNameElementNameConvention())
                .SetExtraElementsMemberConvention(new NamedExtraElementsMemberConvention("ExtraElements"))
                .SetIdMemberConvention(new NamedIdMemberConvention("Id"))
                .SetIgnoreExtraElementsConvention(new NeverIgnoreExtraElementsConvention())
                .SetIgnoreIfNullConvention(new NeverIgnoreIfNullConvention())
                .SetMemberFinderConvention(new PublicMemberFinderConvention())
                .SetSerializeDefaultValueConvention(new AlwaysSerializeDefaultValueConvention());
        }
        #endregion

        #region public methods
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

        public ConventionProfile SetIdGeneratorConvention(
            IIdGeneratorConvention convention
        ) {
            IdGeneratorConvention = convention;
            return this;
        }

        public ConventionProfile SetDefaultValueConvention(
            IDefaultValueConvention convention
        ) {
            DefaultValueConvention = convention;
            return this;
        }

        public ConventionProfile SetElementNameConvention(
            IElementNameConvention convention
        ) {
            ElementNameConvention = convention;
            return this;
        }

        public ConventionProfile SetExtraElementsMemberConvention(
            IExtraElementsMemberConvention convention
        ) {
            ExtraElementsMemberConvention = convention;
            return this;
        }

        public ConventionProfile SetIdMemberConvention(
            IIdMemberConvention convention
        ) {
            IdMemberConvention = convention;
            return this;
        }

        public ConventionProfile SetIgnoreExtraElementsConvention(
            IIgnoreExtraElementsConvention convention
        ) {
            IgnoreExtraElementsConvention = convention;
            return this;
        }

        public ConventionProfile SetIgnoreIfNullConvention(
            IIgnoreIfNullConvention convention
        ) {
            IgnoreIfNullConvention = convention;
            return this;
        }

        public ConventionProfile SetMemberFinderConvention(
            IMemberFinderConvention convention
        ) {
            MemberFinderConvention = convention;
            return this;
        }

        public ConventionProfile SetSerializeDefaultValueConvention(
            ISerializeDefaultValueConvention convention
        ) {
            SerializeDefaultValueConvention = convention;
            return this;
        }
        #endregion
    }
}
