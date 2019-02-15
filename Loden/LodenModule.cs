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

using org.herbal3d.cs.CommonEntitiesUtil;

using log4net;
using Nini.Config;

namespace org.herbal3d.Loden {

// Class passed around for global context for this region module instance
public class LodenContext {
    public IConfig sysConfig;
    public LodenParams parms;
    public LodenStats stats;
    public BLogger log;
    public string contextName;  // a unique identifier for this context -- used in filenames, ...

    public LodenContext(IConfig pSysConfig, LodenParams pParms, ILog pLog) {
        sysConfig = pSysConfig;
        parms = pParms;
        log = new LoggerLog4Net(pLog);
        stats = new LodenStats(this);
        contextName = String.Empty;
    }
}

[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "LodenModule")]
    public class LodenModule : INonSharedRegionModule {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly String _logHeader = "[LodenModule]";

        private LodenContext _context;
        private Scene _scene;
        private List<LodenRegion> _regionProcessors = new List<LodenRegion>();

        // IRegionModuleBase.Name
        public string Name { get { return "OSAuthModule"; } }

        // IRegionModuleBase.ReplaceableInterface
        // This module has nothing to do with replaceable interfaces.
        public Type ReplaceableInterface { get { return null; } }

        // IRegionModuleBase.Initialize
        public void Initialise(IConfigSource pConfig) {
            var sysConfig = pConfig.Configs["Loden"];
            _context = new LodenContext(sysConfig, null, _log);
            _context.parms  = new LodenParams(_context);
            if (sysConfig != null) {
                _context.parms.SetParameterConfigurationValues(sysConfig);
            }
            if (_context.parms.Enabled) {
                _log.InfoFormat("{0} Enabled", _logHeader);
            }
        }
        //
        // IRegionModuleBase.Close
        public void Close() {
            // Stop all the region processors.
            List<LodenRegion> processors = new List<LodenRegion>();
            lock (_regionProcessors) {
                processors = new List<LodenRegion>(_regionProcessors);
                _regionProcessors.Clear();
            }
            processors.ForEach(processor => {
                processor.Shutdown();
            });
            processors.Clear();
        }

        // IRegionModuleBase.AddRegion
        // Called once for the region we're managing.
        public void AddRegion(Scene pScene) {
            // Remember all the loaded scenes
            _scene = pScene;
        }

        // IRegionModuleBase.RemoveRegion
        public void RemoveRegion(Scene pScene) {
            _scene = null;
        }

        // IRegionModuleBase.RegionLoaded
        // Called once for each region loaded after all other regions have been loaded.
        public void RegionLoaded(Scene scene) {
            // That's nice.
        }

        // ISharedRegionModule.PostInitialise
        // Called once after all shared regions have been initialized
        public void PostInitialise() {
            if (_context.parms.Enabled) {
                // Start a processing  thread for the region we're managing
                Task.Run(async () => {
                    var processor = new LodenRegion(_scene, _context);
                    lock (_regionProcessors) {
                        _regionProcessors.Add(processor);
                    }
                    await processor.Start();
                });
            }
        }
    }
}
