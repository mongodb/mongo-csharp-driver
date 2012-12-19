using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace MongoDB.Driver.Security.Sspi
{
    /// <summary>
    /// SEC_WINNT_AUTH_IDENTITY
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class AuthIdentity : IDisposable
    {
        // public fields
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Username;
        public int UsernameLength;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Domain;
        public int DomainLength;
        public IntPtr Password;
        public int PasswordLength;
        public AuthIdentityFlag Flags;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthIdentity" /> struct.
        /// </summary>
        /// <param name="identity">The identity.</param>
        public AuthIdentity(MongoClientIdentity identity)
        {
            Username = null;
            UsernameLength = 0;
            if (!string.IsNullOrEmpty(identity.Username))
            {
                Username = identity.Username;
                UsernameLength = Username.Length;
            }

            Password = IntPtr.Zero;
            PasswordLength = 0;
            
            if (identity.SecurePassword != null && identity.SecurePassword.Length > 0)
            {
                Password = Marshal.SecureStringToGlobalAllocUnicode(identity.SecurePassword);
                PasswordLength = identity.SecurePassword.Length;
            }

            Domain = null;
            DomainLength = 0;

            Flags = AuthIdentityFlag.Unicode;
        }

        ~AuthIdentity()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            if (Password != IntPtr.Zero)
            {
                Marshal.ZeroFreeGlobalAllocUnicode(Password);
                Password = IntPtr.Zero;
            }
        }
    }
}