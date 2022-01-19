// Copyright 2022 Robert Adams
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//     Unless required by applicable law or agreed to in writing, software
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using org.herbal3d.cs.CommonUtil;

using log4net;

namespace org.herbal3d.Loden {

    // Wrapper for log4net that looks like a IBLogger
    class LoggerLog4Net : IBLogger {

        private readonly ILog _log;

        public LoggerLog4Net(ILog pLog) {
            _log = pLog;
        }

        void IBLogger.SetLogLevel(LogLevels pLevel) {
            // throw new NotImplementedException();
        }

        void IBLogger.Debug(string pMsg, params object[] pArgs) {
            _log.DebugFormat(pMsg, pArgs);
        }

        void IBLogger.Error(string pMsg, params object[] pArgs) {
            _log.ErrorFormat(pMsg, pArgs);
        }

        void IBLogger.Info(string pMsg, params object[] pArgs) {
            _log.InfoFormat(pMsg, pArgs);
        }

        void IBLogger.Trace(string pMsg, params object[] pArgs) {
            _log.DebugFormat(pMsg, pArgs);
        }

        void IBLogger.Warn(string pMsg, params object[] pArgs) {
            _log.WarnFormat(pMsg, pArgs);
        }
    }
}
