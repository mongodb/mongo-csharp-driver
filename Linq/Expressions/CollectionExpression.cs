using System;
using MongoDB.Driver;

namespace MongoDB.Linq.Expressions
{
    internal class CollectionExpression : AliasedExpression
    {
        public string CollectionName { get; private set; }

        public MongoDatabase Database { get; private set; }

        public Type DocumentType { get; private set; }

        public CollectionExpression(Alias alias, MongoDatabase database, string collectionName, Type documentType)
            : base(MongoExpressionType.Collection, typeof(void), alias)
        {
            CollectionName = collectionName;
            Database = database;
            DocumentType = documentType;
        }
    }
}