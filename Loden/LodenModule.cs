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

using log4net;
using Nini.Config;

namespace org.herbal3d.Loden {

[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "LodenModule")]
    public class LodenModule : ISharedRegionModule {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly String LogHeader = "[LodenModule]";

        private IConfigSource _config;
        private Dictionary<OMV.UUID, Scene> _scenes = new Dictionary<OMV.UUID, Scene>();

        // IRegionModuleBase.Name
        public string Name { get { return "OSAuthModule"; } }

        public Type ReplaceableInterface => throw new NotImplementedException();

        // IRegionModuleBase.Initialize
        public void Initialise(IConfigSource pConfig) {
            _config = pConfig;
            if (_config.Configs["Loden"] != null)
            {
                IConfig con = _config.Configs["Loden"];
                if (!con.GetBoolean("Enabled", false))
                    return;

                // m_port = con.GetInt("Port", 34343);
                // m_webServerOrigin = con.GetString("WS_Origin", "http://localhost:34343");
                // m_webServerLocation = con.GetString("WS_Location", "ws://localhost:34343/");

                // initialization
                m_log.InfoFormat("{0}: Enabled and initialized", LogHeader);
            }
        }
        //
        // IRegionModuleBase.Close
        public void Close() {
        }

        // IRegionModuleBase.AddRegion
        // Called once for each region loaded.
        public void AddRegion(Scene scene) {
            if (!_scenes.ContainsKey(scene.RegionInfo.RegionID))
                _scenes.Add(scene.RegionInfo.RegionID, scene);
        }

        // IRegionModuleBase.RemoveRegion
        public void RemoveRegion(Scene scene) {
        }

        // IRegionModuleBase.RegionLoaded
        // Called once for each region loaded after all other regions have been loaded.
        public void RegionLoaded(Scene scene) {
        }

        // ISharedRegionModule.PostInitialise
        // Called once after all shared regions have been initialized
        public void PostInitialise() {
            throw new NotImplementedException();
        }
    }
}
