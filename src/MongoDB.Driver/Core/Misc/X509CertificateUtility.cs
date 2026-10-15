/* Copyright 2010-present MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#if NET472
using System.Runtime.CompilerServices;
#endif
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MongoDB.Driver.Core.Misc;

internal static class X509CertificateUtility
{
    // Derived from the certificate hash (itself a digest of the raw data) so that any two
    // certificates considered equal by CertificatesAreEqual produce the same value.
    public static int GetCertificateHashCode(X509Certificate certificate)
    {
        if (certificate == null)
        {
            return 0;
        }

        var hash = 17;
        foreach (var b in GetCertHash(certificate))
        {
            hash = 37 * hash + b;
        }
        return hash;
    }

    public static bool AreCertificatesEqual(X509Certificate lhs, X509Certificate rhs)
    {
        if (ReferenceEquals(lhs, rhs))
        {
            return true;
        }

        if (lhs == null || rhs == null)
        {
            return false;
        }

        // X509Certificate.Equals compares only the issuer name and serial number, which two
        // distinct certificates can share (a CA can reissue a certificate reusing a serial, and
        // both values are attacker-chosen for a self-signed certificate).
        // Using GetCertHash(HashAlgorithmName.SHA256) is safe because it validates a collision-resistant hash
        // of the entire certificate structure.
        // The certificate itself is public, so whether each side holds a private key is compared as well:
        // otherwise a certificate without a private key would compare equal to one that has it.
        // The hash is public data, so returning early when it differs leaks nothing.
        return FixedTimeEquals(GetCertHash(lhs), GetCertHash(rhs)) && ArePrivateKeysEqual(lhs, rhs);
    }

    private static bool ArePrivateKeysEqual(X509Certificate lhs, X509Certificate rhs)
    {
        if (ReferenceEquals(lhs, rhs))
        {
            return true;
        }

        var lhsCertificate = lhs as X509Certificate2;
        var rhsCertificate = rhs as X509Certificate2;

        // Only the presence of a private key is compared, not the key itself. The caller has already established that the raw
        // certificate data (and therefore the public key) is identical, and the platform only associates a private key
        // with a certificate whose public key it matches (e.g. X509Certificate2.CopyWithPrivateKey rejects any other key).
        // So when both certificates have a private key it must be the key matching that shared public key.
        return lhsCertificate?.HasPrivateKey == rhsCertificate?.HasPrivateKey;
    }

#if NET472
    // CryptographicOperations is not available on net472, so use an equivalent local implementation.
    // NoOptimization/NoInlining keep the JIT from introducing an early exit.
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private static bool FixedTimeEquals(byte[] lhs, byte[] rhs)
    {
        if (lhs.Length != rhs.Length)
        {
            return false;
        }

        var accumulator = 0;
        for (var i = 0; i < lhs.Length; i++)
        {
            accumulator |= lhs[i] ^ rhs[i];
        }

        return accumulator == 0;
    }

    private static byte[] GetCertHash(X509Certificate cert)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(cert.GetRawCertData());
    }
#else
    private static bool FixedTimeEquals(byte[] lhs, byte[] rhs) =>
        CryptographicOperations.FixedTimeEquals(lhs, rhs);

    private static byte[] GetCertHash(X509Certificate cert)
        => cert.GetCertHash(HashAlgorithmName.SHA256);
#endif
}
