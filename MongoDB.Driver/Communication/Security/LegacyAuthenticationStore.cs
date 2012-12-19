using MongoDB.Driver.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver.Communication.Security
{
    internal class LegacyAuthenticationStore : IAuthenticationStore
    {
        private readonly Dictionary<string, Authentication> _authentications = new Dictionary<string, Authentication>();
        private readonly MongoConnection _connection;

        public LegacyAuthenticationStore(MongoConnection connection)
        {
            _authentications = new Dictionary<string, Authentication>();
            _connection = connection;
        }

        public void Authenticate(string databaseName, MongoCredentials credentials)
        {
            if (!CanAuthenticate(databaseName, credentials))
            {
                throw new InvalidOperationException("Database cannot be authenticated using this connection.  Ensure that CanAuthenticate is called before calling Authenticate.");
            }

            if (databaseName == null || credentials == null)
            {
                // nothing to do...
                return;
            }

            var nonceCommand = new CommandDocument("getnonce", 1);
            var commandResult = _connection.RunCommand(databaseName, QueryFlags.None, nonceCommand, false);
            if (!commandResult.Ok)
            {
                throw new MongoAuthenticationException(
                    "Error getting nonce for authentication.",
                    new MongoCommandException(commandResult));
            }

            var nonce = commandResult.Response["nonce"].AsString;
            var passwordDigest = MongoUtils.Hash(credentials.Username + ":mongo:" + credentials.Password);
            var digest = MongoUtils.Hash(nonce + credentials.Username + passwordDigest);
            var authenticateCommand = new CommandDocument
                {
                    { "authenticate", 1 },
                    { "user", credentials.Username },
                    { "nonce", nonce },
                    { "key", digest }
                };

            commandResult = _connection.RunCommand(databaseName, QueryFlags.None, authenticateCommand, false);
            if (!commandResult.Ok)
            {
                var message = string.Format("Invalid credentials for database '{0}'.", databaseName);
                throw new MongoAuthenticationException(
                    message,
                    new MongoCommandException(commandResult));
            }
        }

        // check whether the connection can be used with the given database (and credentials)
        // the following are the only valid authentication states for a connection:
        // 1. the connection is not authenticated against any database
        // 2. the connection has a single authentication against the admin database (with a particular set of credentials)
        // 3. the connection has one or more authentications against any databases other than admin
        //    (with the restriction that a particular database can only be authenticated against once and therefore with only one set of credentials)

        // assume that IsAuthenticated was called first and returned false
        public bool CanAuthenticate(string databaseName, MongoCredentials credentials)
        {
            if (databaseName == null)
            {
                return true;
            }

            if (_authentications.Count == 0)
            {
                // a connection with no existing authentications can authenticate anything
                return true;
            }
            else
            {
                // a connection with existing authentications can't be used without credentials
                if (credentials == null)
                {
                    return false;
                }

                // a connection with existing authentications can't be used with new admin credentials
                if (credentials.Admin)
                {
                    return false;
                }

                // a connection with an existing authentication to the admin database can't be used with any other credentials
                if (_authentications.ContainsKey("admin"))
                {
                    return false;
                }

                // a connection with an existing authentication to a database can't authenticate for the same database again
                if (_authentications.ContainsKey(databaseName))
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsAuthenticated(string databaseName, MongoCredentials credentials)
        {
            if (databaseName == null)
            {
                return true;
            }

            if (credentials == null)
            {
                return _authentications.Count == 0;
            }
            else
            {
                var authenticationDatabaseName = credentials.Admin ? "admin" : databaseName;
                Authentication authentication;
                if (_authentications.TryGetValue(authenticationDatabaseName, out authentication))
                {
                    return credentials == authentication.Credentials;
                }
                else
                {
                    return false;
                }
            }
        }

        public void Logout(string databaseName)
        {
            var logoutCommand = new CommandDocument("logout", 1);
            var commandResult = _connection.RunCommand(databaseName, QueryFlags.None, logoutCommand, false);
            if (!commandResult.Ok)
            {
                throw new MongoAuthenticationException(
                    "Error logging off.",
                    new MongoCommandException(commandResult));
            }

            _authentications.Remove(databaseName);
        }

        // nested classes
        // keeps track of what credentials were used with a given database
        // and when that database was last used on this connection
        private class Authentication
        {
            // private fields
            private MongoCredentials _credentials;
            private DateTime _lastUsed;

            // constructors
            public Authentication(MongoCredentials credentials)
            {
                _credentials = credentials;
                _lastUsed = DateTime.UtcNow;
            }

            public MongoCredentials Credentials
            {
                get { return _credentials; }
            }

            public DateTime LastUsed
            {
                get { return _lastUsed; }
                set { _lastUsed = value; }
            }
        }
    }
}
