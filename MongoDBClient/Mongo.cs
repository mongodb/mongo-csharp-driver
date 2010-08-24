using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MongoDB.MongoDBClient {
    public static class Mongo {
        #region public static methods
        public static string Hash(
            string text
        ) {
            var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
            var hash = BitConverter.ToString(bytes).Replace("-", "").ToLower();
            return hash;
        }
        #endregion
    }
}
