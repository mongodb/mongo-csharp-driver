/* Copyright 2010-2014 MongoDB Inc.
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

namespace MongoDB.Driver.Communication.Security.Mechanisms.Gsasl
{
    /// <summary>
    /// Valid properties used in a GsaslSession.
    /// </summary>
    internal enum GsaslProperty
    {
        GSASL_AUTHID = 1,
        GSASL_AUTHZID = 2,
        GSASL_PASSWORD = 3,
        GSASL_ANONYMOUS_TOKEN = 4,
        GSASL_SERVICE = 5,
        GSASL_HOSTNAME = 6,
        GSASL_GSSAPI_DISPLAY_NAME = 7,
        GSASL_PASSCODE = 8,
        GSASL_SUGGESTED_PIN = 9,
        GSASL_PIN = 10,
        GSASL_REALM = 11,
        GSASL_DIGEST_MD5_HASHED_PASSWORD = 12,
        GSASL_QOPS = 13,
        GSASL_QOP = 14,
        GSASL_SCRAM_ITER = 15,
        GSASL_SCRAM_SALT = 16,
        GSASL_SCRAM_SALTED_PASSWORD = 17,
        GSASL_CB_TLS_UNIQUE = 18,
        GSASL_SAML20_IDP_IDENTIFIER = 19,
        GSASL_SAML20_REDIRECT_URL = 20,
        GSASL_OPENID20_REDIRECT_URL = 21,
        GSASL_OPENID20_OUTCOME_DATA = 22,
        /* Client callbacks. */
        GSASL_SAML20_AUTHENTICATE_IN_BROWSER = 250,
        GSASL_OPENID20_AUTHENTICATE_IN_BROWSER = 251,
        /* Server validation callback properties. */
        GSASL_VALIDATE_SIMPLE = 500,
        GSASL_VALIDATE_EXTERNAL = 501,
        GSASL_VALIDATE_ANONYMOUS = 502,
        GSASL_VALIDATE_GSSAPI = 503,
        GSASL_VALIDATE_SECURID = 504,
        GSASL_VALIDATE_SAML20 = 505,
        GSASL_VALIDATE_OPENID20 = 506
    }
}
