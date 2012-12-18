using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace MongoDB.Driver
{
    /// <summary>
    /// The authentication type used to communicate with MongoDB.
    /// </summary>
    public enum MongoAuthenticationType
    {
        /// <summary>
        /// Authenticate to the server using GSSAPI.
        /// </summary>
        GSSAPI
    }

    public class MongoClientIdentity
    {
        // private fields
        private readonly MongoAuthenticationType _authenticationType;
        private SecureString _password;
        private readonly string _username;

        // constructors
        internal MongoClientIdentity()
            : this("", "", MongoAuthenticationType.GSSAPI)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoClientIdentity" /> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public MongoClientIdentity(string username, string password, MongoAuthenticationType authenticationType)  
        {
            _username = username;
            _password = CreateSecureString(password);
            _authenticationType = authenticationType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoClientIdentity" /> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public MongoClientIdentity(string username, SecureString password, MongoAuthenticationType authenticationType)
        {
            _username = username;
            _password = password.Copy();
            _authenticationType = authenticationType;
        }

        // public properties
        public MongoAuthenticationType AuthenticationType
        {
            get { return _authenticationType; }
        }

        /// <summary>
        /// Gets the password.
        /// </summary>
        public string Password
        {
            get { return CreateString(_password); }
        }

        /// <summary>
        /// Gets the secure password.
        /// </summary>
        public SecureString SecurePassword
        {
            get { return _password.Copy(); }
        }

        /// <summary>
        /// Gets the username.
        /// </summary>
        public string Username
        {
            get { return _username; }
        }

        // internal properties
        /// <summary>
        /// Gets the realm.
        /// </summary>
        /// <remarks>This is here currently as a placeholder. It will become public with server 2.6.</remarks>
        internal string Realm
        {
            get { return "$sasl"; }
        }

        // private static methods
        private static SecureString CreateSecureString(string str)
        {
            var secureStr = new SecureString();
            foreach (var c in str)
            {
                secureStr.AppendChar(c);
            }

            return secureStr;
        }

        private static string CreateString(SecureString secureStr)
        {
            IntPtr strPtr = IntPtr.Zero;
            if (secureStr == null || secureStr.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                strPtr = Marshal.SecureStringToBSTR(secureStr);
                return Marshal.PtrToStringBSTR(strPtr);
            }
            finally
            {
                if (strPtr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(strPtr);
                }
            }
        }
    }

    public class SystemMongoClientIdentity : MongoClientIdentity
    {
        public readonly static SystemMongoClientIdentity Instance = new SystemMongoClientIdentity();
    }
}