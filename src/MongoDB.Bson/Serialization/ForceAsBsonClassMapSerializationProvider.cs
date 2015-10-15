namespace MongoDB.Bson.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a BSON serialization provider which defines that some serializable types should be treated
    /// as BSON class maps.
    /// </summary>
    /// <remarks>
    /// Argumented types to be forced as BSON class maps can be either concrete or also base class and interface ones.
    /// 
    /// This serialization provider is useful when a class may implement a collection interface (for example, <see cref="System.Collections.Generic.IList{T}"/>)
    /// because the domain requires the class to act as a collection, but in terms of serialization, it must be serialized as a regular
    /// POCO class.
    /// </remarks>
    /// <example>
    /// For example, given the following class:
    /// 
    /// <code language="c#">
    /// public interface ISomeInterface { }
    /// public class SomeImpl : ISomeInterface { }
    /// </code>
    /// 
    /// This provider can be configured both to force any <codeInline>SomeImpl</codeInline> to be treated as 
    /// BSON class map and also any implementation of <codeInline>ISomeInterface</codeInline> can be configured as a 
    /// forced type to let any implementation be serialized as a BSON class map:
    /// 
    /// <code language="c#">
    /// ForceAsBsonClassMapSerializationProvider provider = new ForceAsBsonClassMapSerializationProvider(typeof(SomeImpl));
    /// 
    /// // or
    /// 
    /// ForceAsBsonClassMapSerializationProvider provider = new ForceAsBsonClassMapSerializationProvider(typeof(ISomeInterface));
    /// 
    /// // or even both
    /// 
    /// ForceAsBsonClassMapSerializationProvider provider = new ForceAsBsonClassMapSerializationProvider(typeof(SomeImpl), typeof(ISomeInterface));
    /// </code>
    /// </example>
    public sealed class ForceAsBsonClassMapSerializationProvider : BsonSerializationProviderBase
    {
        private readonly HashSet<Type> _forcedTypes;

        /// <summary>
        /// Constructor to give forced types as a type array.
        /// </summary>
        /// <param name="forcedTypes">The whole types to be forced as BSON class maps</param>
        public ForceAsBsonClassMapSerializationProvider(params Type[] forcedTypes)
            : this((IEnumerable<Type>)forcedTypes)
        {
        }

        /// <summary>
        /// Constructor to give forced types as a sequence of types.
        /// </summary>
        /// <param name="forcedTypes">The whole types to be forced as BSON class maps</param>
        public ForceAsBsonClassMapSerializationProvider(IEnumerable<Type> forcedTypes)
        {
            if (forcedTypes == null || forcedTypes.Count() == 0)
                throw new ArgumentException("Cannot configure a forced BSON class map serialization provider which contains no types to be forced as BSON class maps", "forcedTypes");
            if (forcedTypes.All(type => type.IsClass || type.IsInterface))
                throw new ArgumentException("Forced types must be classes or interfaces");

            _forcedTypes = new HashSet<Type>(forcedTypes);
        }

        /// <summary>
        /// Gets a set of types to be forced as BSON class maps during their serialization.
        /// </summary>
        public HashSet<Type> ForcedTypes { get { return _forcedTypes; } }

        /// <inheritdoc/>
        public override IBsonSerializer GetSerializer(Type type, IBsonSerializerRegistry serializerRegistry)
        {
            // Forcing can happen either if type to be serialized is within forced type set, or if one of forced types
            // is implemented or inherited by the given type.
            if (ForcedTypes.Contains(type) || ForcedTypes.Any(forcedType => forcedType.IsAssignableFrom(type)))
            {
                BsonClassMapSerializationProvider bsonClassMapProvider = new BsonClassMapSerializationProvider();

                return bsonClassMapProvider.GetSerializer(type);
            }

            return null;
        }
    }
}