using MongoDB.Driver.Security.Gsasl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MongoDB.Driver.Security.Mechanisms
{
    /// <summary>
    /// A mechanism implementing the CRAM-MD5 sasl specification.
    /// </summary>
    internal class GsaslCramMD5Mechanism : AbstractGsaslMechanism
    {
        // private fields
        private readonly NetworkCredential _credential;
        private readonly string _databaseName;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GsaslCramMD5Mechanism" /> class.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="credential">The credential.</param>
        public GsaslCramMD5Mechanism(string databaseName, NetworkCredential credential)
            : base("CRAM-MD5")
        {
            _databaseName = databaseName;
            _credential = credential;
        }

        // protected methods
        /// <summary>
        /// Gets the properties that should be used in the specified mechanism.
        /// </summary>
        /// <returns>The properties.</returns>
        protected override IEnumerable<KeyValuePair<string, string>> GetProperties()
        {
            yield return new KeyValuePair<string, string>("AUTHID", CreateUsername());
            yield return new KeyValuePair<string, string>("PASSWORD", CreatePassword());
        }
        
        // private methods
        private string CreatePassword()
        {
            var mongoPassword = _credential.UserName + ":mongo:" + _credential.Password;
            byte[] password;
            using (var md5 = MD5.Create())
            {
                password = md5.ComputeHash(Encoding.UTF8.GetBytes(mongoPassword));
            }

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < password.Length; i++)
            {
                builder.Append(password[i].ToString("x2"));
            }

            return builder.ToString();            
        }

        private string CreateUsername()
        {
            return _databaseName + "$" + _credential.UserName;
        }
    }
}