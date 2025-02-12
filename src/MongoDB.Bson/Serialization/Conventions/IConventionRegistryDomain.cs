using System;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// //TODO
    /// </summary>
    public interface IConventionRegistryDomain
    {
        /// <summary>
        /// //TODO
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IConventionPack Lookup(Type type);

        /// <summary>
        /// //TODO
        /// </summary>
        /// <param name="name"></param>
        /// <param name="conventions"></param>
        /// <param name="filter"></param>
        void Register(string name, IConventionPack conventions, Func<Type, bool> filter);

        /// <summary>
        /// //TODO
        /// </summary>
        /// <param name="name"></param>
        void Remove(string name);
    }
}