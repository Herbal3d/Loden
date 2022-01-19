// Copyright 2019 Robert Adams
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
using System.Reflection;
using System.Threading.Tasks;
using Mono.Addins;

using OpenSim.Framework;
using OpenSim.Region.CoreModules.World.LegacyMap;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.PhysicsModules.SharedBase; // needed to test if objects are physical

using OMV = OpenMetaverse;
using OMVS = OpenMetaverse.StructuredData;
using OMVA = OpenMetaverse.Assets;
using OMVR = OpenMetaverse.Rendering;

using org.herbal3d.cs.CommonUtil;

using log4net;
using Nini.Config;

namespace org.herbal3d.Loden {

    // Class passed around for global context for this region module instance
    public class LodenContext {
        public IConfig sysConfig;
        public LodenParams parms;
        public LodenStats stats;
        public IBLogger log;
        public string contextName;          // a unique identifier for this context -- used in filenames, ...

        public LodenContext(IConfig pSysConfig, LodenParams pParms, IBLogger pLog) {
            sysConfig = pSysConfig;
            parms = pParms;
            log = pLog;
            stats = new LodenStats(this);
            contextName = String.Empty;
        }
    }

    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "LodenModule")]
    public class LodenModule : INonSharedRegionModule {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly String _logHeader = "[LodenModule]";

        private LodenContext _lContext;
        private Scene _scene;
        private LodenRegion _regionProcessor;

        // IRegionModuleBase.Name
        public string Name { get { return "OSAuthModule"; } }

        // IRegionModuleBase.ReplaceableInterface
        // This module has nothing to do with replaceable interfaces.
        public Type ReplaceableInterface { get { return null; } }

        // IRegionModuleBase.Initialize
        public void Initialise(IConfigSource pConfig) {
            var sysConfig = pConfig.Configs["Loden"];

            _lContext = new LodenContext(sysConfig, null, new LoggerLog4Net(_log));
            _lContext.parms  = new LodenParams(_lContext);
            if (_lContext.parms.Enabled) {
                _lContext.log.Info("{0} Enabled", _logHeader);
            }
        }
        //
        // IRegionModuleBase.Close
        public void Close() {
            // Stop the region processor.
            if (_regionProcessor != null) {
                _regionProcessor.Shutdown();
                _regionProcessor = null;
            }
        }

        // IRegionModuleBase.AddRegion
        // Called once for the region we're managing.
        public void AddRegion(Scene pScene) {
            // Remember all the loaded scenes
            _scene = pScene;
        }

        // IRegionModuleBase.RemoveRegion
        public void RemoveRegion(Scene pScene) {
            if (_scene != null) {
                Close();
                _scene = null;
            }
        }

        // IRegionModuleBase.RegionLoaded
        // Called once for each region loaded after all other regions have been loaded.
        public void RegionLoaded(Scene scene) {
            if (_lContext.parms.Enabled) {
                // Start a processing  thread for the region we're managing
                _regionProcessor = new LodenRegion(_scene, _lContext);
                _regionProcessor.Start();
            }
            // That's nice.
        }
    }

}
