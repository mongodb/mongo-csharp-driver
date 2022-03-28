using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MongoDriverRepo
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            BsonClassMap.RegisterClassMap<GeoCoordinate>(c =>
            {
                c.AutoMap();
                c.UnmapConstructor(typeof(GeoCoordinate).GetConstructor(new[] { typeof(string), typeof(string) }));
            });

            foreach (var aggregateType in TestExtensions.GetTypes())
            {
                BsonClassMap.LookupClassMap(aggregateType);
            }

            BsonClassMap.LookupClassMap(typeof(Aggregate));
            BsonClassMap.LookupClassMap(typeof(GeoCoordinate));
        }
    }

    public static class TestExtensions
    {
        public static bool CanSet(this PropertyInfo propertyInfo, object dsu, object value, bool canSetEverything)
        {
            try
            {
                propertyInfo.SetValue(dsu, value);
                return canSetEverything;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error with property " + propertyInfo.Name + " " + e.Message);
                return false;
            }
        }

        public static List<Type> GetTypes()
        {
            var domainAssembly = Assembly.GetExecutingAssembly();
            var allAggregateRoots = domainAssembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(AggregateBase))).ToList();

            return allAggregateRoots
                .Where(x => x != typeof(OurAggregateBase))
                .Where(x => !x.IsAbstract).ToList();
        }
    }
    public interface IAggregate
    {
        Guid Id { get; }
    }

    public abstract class AggregateBase : IAggregate, IEquatable<IAggregate>
    {
        public Guid Id { get; protected set; }

        public virtual bool Equals(IAggregate other) => other != null && other.Id == Id;

    }
    public class OurAggregateBase : AggregateBase { }
    public class Aggregate : OurAggregateBase
    {
        public GeoCoordinate GeoCoordinate { get; private set; }
    }

    public class GeoCoordinate : System.IEquatable<GeoCoordinate>
    {
        public GeoCoordinate(string latitude, string longitude)
        {
            double lat, lon;
            double.TryParse(latitude, out lat);
            double.TryParse(longitude, out lon);

            Longitude = lon;
            Latitude = lat;
        }

        public GeoCoordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public double Latitude { get; private set; }

        public double Longitude { get; private set; }

        public override bool Equals(Object o)
        {
            return o is GeoCoordinate && Equals((GeoCoordinate)o);
        }

        public override int GetHashCode()
        {
            return Latitude.GetHashCode() ^ Longitude.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Latitude},{Longitude}";
        }

        public bool Equals(GeoCoordinate other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);
        }
    }
}
