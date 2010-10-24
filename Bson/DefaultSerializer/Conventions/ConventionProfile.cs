using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public sealed class ConventionProfile {
        #region public static properties
        public static ConventionProfile Default { get; private set; }
        #endregion

        #region public properties
        public IBsonIdGeneratorConvention BsonIdGeneratorConvention { get; private set; }

        public IElementNameConvention ElementNameConvention { get; private set; }

        public IIdPropertyConvention IdPropertyConvention { get; private set; }

        public Func<Type, bool> TypeFilter { get; private set; }
        #endregion

        #region constructors
        static ConventionProfile() {
            Default = new ConventionProfile(t => true) //The default profile always matches...
                .SetBsonIdGeneratorConvention(new BsonSerializerBsonIdGeneratorConvention())
                .SetElementNameConvention(new MemberNameElementNameConvention())
                .SetIdPropertyConvention(new NamedIdPropertyConvention("Id"));
        }

        public ConventionProfile(
            Func<Type, bool> typeFilter
        ) {
            TypeFilter = typeFilter;
        }

        #endregion

        #region public methods
        public ConventionProfile SetBsonIdGeneratorConvention(
            IBsonIdGeneratorConvention convention
        ) {
            BsonIdGeneratorConvention = convention;
            return this;
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
        #endregion
    }
}