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
        private readonly static MongoClientIdentity _system = SystemMongoClientIdentity.Instance;

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
        /// <param name="authenticationType">Type of the authentication.</param>
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
        /// <param name="authenticationType">Type of the authentication.</param>
        public MongoClientIdentity(string username, SecureString password, MongoAuthenticationType authenticationType)
        {
            _username = username;
            if (password != null)
            {
                _password = password.Copy();
            }
            _authenticationType = authenticationType;
        }

        // public static properties
        /// <summary>
        /// Gets the system identity used to execute the current process.
        /// </summary>
        public static MongoClientIdentity System
        {
            get { return _system; }
        }

        // public properties
        /// <summary>
        /// Gets the type of authentication used to confirm this identity.
        /// </summary>
        public MongoAuthenticationType AuthenticationType
        {
            get { return _authenticationType; }
        }

        /// <summary>
        /// Indicates whether this instance has a password.
        /// </summary>
        public bool HasPassword
        {
            get { return _password != null; }
        }

        /// <summary>
        /// Gets the password.
        /// </summary>
        public string Password
        {
            get 
            {
                if (HasPassword)
                {
                    return CreateString(_password);
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the secure password.
        /// </summary>
        public SecureString SecurePassword
        {
            get 
            {
                if (HasPassword)
                {
                    return _password.Copy();
                }

                return null;
            }
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
            if (str != null)
            {
                var secureStr = new SecureString();
                foreach (var c in str)
                {
                    secureStr.AppendChar(c);
                }
                return secureStr;
            }

            return null;
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