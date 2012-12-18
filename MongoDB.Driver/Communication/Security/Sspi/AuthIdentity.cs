using System.Runtime.InteropServices;

namespace MongoDB.Driver.Security.Sspi
{
    /// <summary>
    /// SEC_WINNT_AUTH_IDENTITY
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct AuthIdentity
    {
        // public fields
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Username;
        public int UsernameLength;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Domain;
        public int DomainLength;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Password;
        public int PasswordLength;
        public AuthIdentityFlag Flags;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthIdentity" /> struct.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="domain">The domain.</param>
        public AuthIdentity(string username, string password)
        {
            Username = username;
            UsernameLength = 0;
            if (username != null)
            {
                UsernameLength = username.Length;
            }
            Password = password;
            PasswordLength = 0;
            if (password != null)
            {
                PasswordLength = password.Length;
            }
            Domain = null;
            DomainLength = 0;

            Flags = AuthIdentityFlag.Unicode;
        }
    }
}