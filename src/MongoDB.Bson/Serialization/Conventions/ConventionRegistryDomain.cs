using System;
using System.Collections.Generic;

namespace MongoDB.Bson.Serialization.Conventions
{
    internal class ConventionRegistryDomain : IConventionRegistryDomain
    {
        private readonly List<ConventionPackContainer> _conventionPacks = [];
        private readonly object _lock = new();

        //  constructors
        internal ConventionRegistryDomain()
        {
            Register("__defaults__", DefaultConventionPack.Instance, t => true);
            Register("__attributes__", AttributeConventionPack.Instance, t => true);
        }

        // public static methods
        /// <summary>
        /// Looks up the effective set of conventions that apply to a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The conventions for that type.</returns>
        public IConventionPack Lookup(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            lock (_lock)
            {
                var pack = new ConventionPack();

                // append any attribute packs (usually just one) at the end so attributes are processed last
                var attributePacks = new List<IConventionPack>();
                foreach (var container in _conventionPacks)
                {
                    if (container.Filter(type))
                    {

                        if (container.Name == "__attributes__")
                        {
                            attributePacks.Add(container.Pack);
                        }
                        else
                        {
                            pack.Append(container.Pack);
                        }
                    }
                }

                foreach (var attributePack in attributePacks)
                {
                    pack.Append(attributePack);
                }

                return pack;
            }
        }

        /// <summary>
        /// Registers the conventions.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="conventions">The conventions.</param>
        /// <param name="filter">The filter.</param>
        public void Register(string name, IConventionPack conventions, Func<Type, bool> filter)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (conventions == null)
            {
                throw new ArgumentNullException("conventions");
            }

            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }

            lock (_lock)
            {
                var container = new ConventionPackContainer
                {
                    Filter = filter,
                    Name = name,
                    Pack = conventions
                };

                _conventionPacks.Add(container);
            }
        }

        /// <summary>
        /// Removes the conventions specified by the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <remarks>Removing a convention allows the removal of the special __defaults__ conventions
        /// and the __attributes__ conventions for those who want to completely customize the
        /// experience.</remarks>
        public void Remove(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            lock (_lock)
            {
                _conventionPacks.RemoveAll(x => x.Name == name);
            }
        }

        // private class
        private class ConventionPackContainer
        {
            public Func<Type, bool> Filter;
            public string Name;
            public IConventionPack Pack;
        }
    }
}