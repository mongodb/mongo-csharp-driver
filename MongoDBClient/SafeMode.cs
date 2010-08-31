/* Copyright 2010 10gen Inc.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.MongoDBClient {
    public class SafeMode {
        #region private static fields
        private static SafeMode safeMode1 = new SafeMode(1);
        private static SafeMode safeMode2 = new SafeMode(2);
        private static SafeMode safeMode3 = new SafeMode(3);
        private static SafeMode safeModeFalse = new SafeMode(false);
        private static SafeMode safeModeTrue = new SafeMode(true);
        #endregion

        #region private fields
        private bool enabled;
        private int replications;
        private TimeSpan timeout;
        #endregion

        #region constructors
        public SafeMode(
            bool enabled
        ) {
            this.enabled = enabled;
        }

        public SafeMode(
            int replications
        ) {
            this.enabled = true;
            this.replications = replications;
        }

        public SafeMode(
            int replications,
            TimeSpan timeout
        ) {
            this.enabled = true;
            this.replications = replications;
            this.timeout = timeout;
        }
        #endregion

        #region public static properties
        public static SafeMode False {
            get { return safeModeFalse; }
        }

        public static SafeMode True {
            get { return safeModeTrue; }
        }
        #endregion

        #region public properties
        public bool Enabled {
            get { return enabled; }
        }

        public int Replications {
            get { return replications; }
        }

        public TimeSpan Timeout {
            get { return timeout; }
        }
        #endregion

        #region public static methods
        public static SafeMode WaitForReplications(
            int replications
        ) {
            switch (replications) {
                case 1: return safeMode1;
                case 2: return safeMode2;
                case 3: return safeMode3;
                default: return new SafeMode(replications);

            }
        }

        public static SafeMode WaitForReplications(
            int replications,
            TimeSpan timeout
        ) {
            if (timeout == TimeSpan.Zero) {
                switch (replications) {
                    case 1: return safeMode1;
                    case 2: return safeMode2;
                    case 3: return safeMode3;
                    default: return new SafeMode(replications);

                }
            } else {
                return new SafeMode(replications, timeout);
            }
        }
        #endregion
    }
}
