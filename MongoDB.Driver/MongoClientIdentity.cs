using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the identity to be used when talking with mongodb.
    /// </summary>
    public class MongoClientIdentity
    {
        // private static fields
        public readonly static MongoClientIdentity _system = SystemMongoClientIdentity.Instance;

        // private fields
        private readonly MongoAuthenticationType _authenticationType;
        private SecureString _password;
        private readonly string _username;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoClientIdentity" /> class.
        /// </summary>
        /// <remarks>
        /// This is here to support SystemMongoClientIdentity.
        /// </remarks>
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

        // public static properties
        public static MongoClientIdentity System
        {
            get { return _system; }
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

    /// <summary>
    /// Represents the process' identity.
    /// </summary>
    internal class SystemMongoClientIdentity : MongoClientIdentity
    {
        public readonly static SystemMongoClientIdentity Instance = new SystemMongoClientIdentity();

        private SystemMongoClientIdentity()
        { }
    }
}