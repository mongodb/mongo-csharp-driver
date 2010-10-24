using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public sealed class ConventionProfile {
        public static ConventionProfile Default { get; set; }

        public IElementNameConvention ElementNameConvention { get; private set; }

        static ConventionProfile()
        {
            Default = new ConventionProfile()
                .SetElementNameConvention(new MemberNameElementNameConvention());
        }

        public ConventionProfile SetElementNameConvention(IElementNameConvention convention)
        {
            ElementNameConvention = convention;
            return this;
        }
    }
}