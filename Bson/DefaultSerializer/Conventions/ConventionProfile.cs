using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public sealed class ConventionProfile {
        #region public properties
        public IBsonIdGeneratorConvention BsonIdGeneratorConvention { get; private set; }

        public IElementNameConvention ElementNameConvention { get; private set; }

        public IIdPropertyConvention IdPropertyConvention { get; private set; }

        public IIgnoreExtraElementsConvention IgnoreExtraElementsConvention { get; private set; }

        public IIgnoreIfNullConvention IgnoreIfNullConvention { get; private set; }

        public IUseCompactRepresentationConvention UseCompactRepresentationConvention { get; private set; }
        #endregion

        #region public static methods
        public static ConventionProfile GetDefault() {
            return new ConventionProfile() //The default profile always matches...
                .SetBsonIdGeneratorConvention(new BsonSerializerBsonIdGeneratorConvention())
                .SetElementNameConvention(new MemberNameElementNameConvention())
                .SetIdPropertyConvention(new NamedIdPropertyConvention("Id"))
                .SetIgnoreExtraElementsConvention(new NeverIgnoreExtraElementsConvention())
                .SetIgnoreIfNullConvention(new NeverIgnoreIfNullConvention())
                .SetUseCompactRepresentationConvention(new NeverUseCompactRepresentationConvention());
        }
        #endregion

        #region public methods
        public void Merge(
            ConventionProfile other
        ) {
            if (BsonIdGeneratorConvention == null)
                BsonIdGeneratorConvention = other.BsonIdGeneratorConvention;
            if (ElementNameConvention == null)
                ElementNameConvention = other.ElementNameConvention;
            if (IdPropertyConvention == null)
                IdPropertyConvention = other.IdPropertyConvention;
            if (IgnoreExtraElementsConvention == null)
                IgnoreExtraElementsConvention = other.IgnoreExtraElementsConvention;
            if (IgnoreIfNullConvention == null)
                IgnoreIfNullConvention = other.IgnoreIfNullConvention;
            if (UseCompactRepresentationConvention == null)
                UseCompactRepresentationConvention = other.UseCompactRepresentationConvention;
        }

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

        public ConventionProfile SetUseCompactRepresentationConvention(
            IUseCompactRepresentationConvention convention
        ) {
            UseCompactRepresentationConvention = convention;
            return this;
        }
        #endregion
    }
}