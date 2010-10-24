using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public sealed class ConventionProfile {
        public static ConventionProfile Default { get; private set; }

        public IElementNameConvention ElementNameConvention { get; private set; }

        public IIdPropertyConvention IdPropertyConvention { get; private set; }

        public Func<Type, bool> TypeFilter { get; private set; }

        static ConventionProfile() {
            Default = new ConventionProfile(t => true) //The default profile always matches...
                .SetElementNameConvention(new MemberNameElementNameConvention())
                .SetIdPropertyConvention(new NamedIdPropertyConvention("Id"));
        }

        public ConventionProfile(
            Func<Type, bool> typeFilter
        ) {
            TypeFilter = typeFilter;
        }

        public ConventionProfile SetElementNameConvention(
            IElementNameConvention convention
        ) {
            ElementNameConvention = convention;
            return this;
        }

        public ConventionProfile SetIdPropertyConvention(
            IIdPropertyConvention convention
        ) {
            IdPropertyConvention = convention;
            return this;
        }
    }
}