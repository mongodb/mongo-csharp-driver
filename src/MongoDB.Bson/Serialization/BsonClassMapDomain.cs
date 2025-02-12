using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MongoDB.Bson.Serialization;

internal class BsonClassMapDomain : IBsonClassMapDomain
{
    // private fields
    private readonly IBsonSerializationDomain _serializationDomain;
    private readonly Dictionary<Type, BsonClassMap> _classMaps = new();

    public BsonClassMapDomain(BsonSerializationDomain serializationDomain)
    {
        _serializationDomain = serializationDomain;
    }

    /// <summary>
    /// Gets all registered class maps.
    /// </summary>
    /// <returns>All registered class maps.</returns>
    public IEnumerable<BsonClassMap> GetRegisteredClassMaps()
    {
        BsonSerializer.ConfigLock.EnterReadLock();  //TODO It would make sense to look at this after the PR by Robert is merged
        try
        {
            return _classMaps.Values.ToList(); // return a copy for thread safety
        }
        finally
        {
            BsonSerializer.ConfigLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Checks whether a class map is registered for a type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if there is a class map registered for the type.</returns>
    public bool IsClassMapRegistered(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        BsonSerializer.ConfigLock.EnterReadLock();
        try
        {
            return _classMaps.ContainsKey(type);
        }
        finally
        {
            BsonSerializer.ConfigLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Looks up a class map (will AutoMap the class if no class map is registered).
    /// </summary>
    /// <param name="classType">The class type.</param>
    /// <returns>The class map.</returns>
    public BsonClassMap LookupClassMap(Type classType)
    {
        if (classType == null)
        {
            throw new ArgumentNullException("classType");
        }

        BsonSerializer.ConfigLock.EnterReadLock();
        try
        {
            if (_classMaps.TryGetValue(classType, out var classMap))
            {
                if (classMap.IsFrozen)
                {
                    return classMap;
                }
            }
        }
        finally
        {
            BsonSerializer.ConfigLock.ExitReadLock();
        }

        // automatically create a new classMap for classType and register it (unless another thread does first)
        // do the work of speculatively creating a new class map outside of holding any lock
        var classMapDefinition = typeof(BsonClassMap<>);
        var classMapType = classMapDefinition.MakeGenericType(classType);
        var newClassMap = (BsonClassMap)Activator.CreateInstance(classMapType);
        newClassMap.AutoMap(_serializationDomain);

        BsonSerializer.ConfigLock.EnterWriteLock();
        try
        {
            if (!_classMaps.TryGetValue(classType, out var classMap))
            {
                RegisterClassMap(newClassMap);
                classMap = newClassMap;
            }

            return classMap.Freeze();
        }
        finally
        {
            BsonSerializer.ConfigLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Creates and registers a class map.
    /// </summary>
    /// <typeparam name="TClass">The class.</typeparam>
    /// <returns>The class map.</returns>
    public BsonClassMap<TClass> RegisterClassMap<TClass>()
    {
        return RegisterClassMap<TClass>(cm => { cm.AutoMap(_serializationDomain); });
    }

    /// <summary>
    /// Creates and registers a class map.
    /// </summary>
    /// <typeparam name="TClass">The class.</typeparam>
    /// <param name="classMapInitializer">The class map initializer.</param>
    /// <returns>The class map.</returns>
    public BsonClassMap<TClass> RegisterClassMap<TClass>(Action<BsonClassMap<TClass>> classMapInitializer)
    {
        var classMap = new BsonClassMap<TClass>(classMapInitializer);
        RegisterClassMap(classMap);
        return classMap;
    }

    /// <summary>
    /// Registers a class map.
    /// </summary>
    /// <param name="classMap">The class map.</param>
    public void RegisterClassMap(BsonClassMap classMap)
    {
        if (classMap == null)
        {
            throw new ArgumentNullException("classMap");
        }

        BsonSerializer.ConfigLock.EnterWriteLock();
        try
        {
            // note: class maps can NOT be replaced (because derived classes refer to existing instance)
            _classMaps.Add(classMap.ClassType, classMap);
            BsonSerializer.RegisterDiscriminator(classMap.ClassType, classMap.Discriminator);
        }
        finally
        {
            BsonSerializer.ConfigLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Registers a class map if it is not already registered.
    /// </summary>
    /// <typeparam name="TClass">The class.</typeparam>
    /// <returns>True if this call registered the class map, false if the class map was already registered.</returns>
    public bool TryRegisterClassMap<TClass>()
    {
        return TryRegisterClassMap(() => ClassMapFactory(_serializationDomain));

        static BsonClassMap<TClass> ClassMapFactory(IBsonSerializationDomain serializationDomain)
        {
            var classMap = new BsonClassMap<TClass>();
            classMap.AutoMap(serializationDomain);
            return classMap;
        }
    }

    /// <summary>
    /// Registers a class map if it is not already registered.
    /// </summary>
    /// <typeparam name="TClass">The class.</typeparam>
    /// <param name="classMap">The class map.</param>
    /// <returns>True if this call registered the class map, false if the class map was already registered.</returns>
    public bool TryRegisterClassMap<TClass>(BsonClassMap<TClass> classMap)
    {
        if (classMap == null)
        {
            throw new ArgumentNullException(nameof(classMap));
        }

        return TryRegisterClassMap(ClassMapFactory);

        BsonClassMap<TClass> ClassMapFactory()
        {
            return classMap;
        }
    }

    /// <summary>
    /// Registers a class map if it is not already registered.
    /// </summary>
    /// <typeparam name="TClass">The class.</typeparam>
    /// <param name="classMapInitializer">The class map initializer (only called if the class map is not already registered).</param>
    /// <returns>True if this call registered the class map, false if the class map was already registered.</returns>
    public bool TryRegisterClassMap<TClass>(Action<BsonClassMap<TClass>> classMapInitializer)
    {
        if (classMapInitializer == null)
        {
            throw new ArgumentNullException(nameof(classMapInitializer));
        }

        return TryRegisterClassMap(ClassMapFactory);

        BsonClassMap<TClass> ClassMapFactory()
        {
            return new BsonClassMap<TClass>(classMapInitializer);
        }
    }

    /// <summary>
    /// Registers a class map if it is not already registered.
    /// </summary>
    /// <typeparam name="TClass">The class.</typeparam>
    /// <param name="classMapFactory">The class map factory (only called if the class map is not already registered).</param>
    /// <returns>True if this call registered the class map, false if the class map was already registered.</returns>
    public bool TryRegisterClassMap<TClass>(Func<BsonClassMap<TClass>> classMapFactory)
    {
        if (classMapFactory == null)
        {
            throw new ArgumentNullException(nameof(classMapFactory));
        }

        BsonSerializer.ConfigLock.EnterReadLock();
        try
        {
            if (_classMaps.ContainsKey(typeof(TClass)))
            {
                return false;
            }
        }
        finally
        {
            BsonSerializer.ConfigLock.ExitReadLock();
        }

        BsonSerializer.ConfigLock.EnterWriteLock();
        try
        {
            if (_classMaps.ContainsKey(typeof(TClass)))
            {
                return false;
            }
            else
            {
                // create a classMap for TClass and register it
                var classMap = classMapFactory();
                RegisterClassMap(classMap);
                return true;
            }
        }
        finally
        {
            BsonSerializer.ConfigLock.ExitWriteLock();
        }
    }
}