using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.DefaultSerializer.Conventions 
{
    public interface IConventionExtension 
    {
        void Apply(BsonClassMap classMap);
    }
}
