using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MongoDB.MongoDBClient {
    public static class Mongo {
        #region public static fields
        private static int maxMessageLength = 16 * 1024 * 1204; // 16MB
        private static int tcpReceiveBufferSize = 4 * 1024 * 1204; // 4MB
        private static int tcpSendBufferSize = 4 * 1024 * 1204; // 4MB
        #endregion

        #region public static properties
        public static int MaxMessageLength {
            get { return maxMessageLength; }
            set { maxMessageLength = value; }
        }

        public static int TcpReceiveBufferSize {
            get { return tcpReceiveBufferSize; }
            set { tcpReceiveBufferSize = value; }
        }

        public static int TcpSendBufferSize {
            get { return tcpSendBufferSize; }
            set { tcpSendBufferSize = value; }
        }
        #endregion

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
