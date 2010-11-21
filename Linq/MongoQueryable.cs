using MongoDB.Bson;

namespace MongoDB.Linq
{
    /// <summary>
    /// 
    /// </summary>
    internal static class MongoQueryable
    {
        /// <summary>
        /// Keys the specified document.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="document">The document.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static DocumentQuery Key<T>(this T document, string key) where T : BsonDocument
        {
            return new DocumentQuery(document, key);
        }
    }
}