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
//
// Some code covered by: Copyright (c) Contributors, http://opensimulator.org/
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;

using org.herbal3d.cs.CommonUtil;

using Nini.Config;

namespace org.herbal3d.Loden {
    public class ConfigParam : Attribute {
        public ConfigParam(string name, Type valueType, string desc = null) {
            this.name = name;
            this.valueType = valueType;
            this.desc = desc;
        }
        public string name;
        public Type valueType;
        public string desc;
    }

    public class LodenParams {
        private static readonly string _logHeader = "[LODEN PARAMS]";
        private readonly LodenContext _context;

        public LodenParams(LodenContext pContext) {
            _context = pContext;
            // If we were passed INI configuration, overlay the default values
            if (_context.sysConfig != null) {
                SetParameterConfigurationValues(_context.sysConfig);
            }
        }

        // =====================================================================================
        // =====================================================================================
        // List of all of the externally visible parameters.
        [ConfigParam(name: "Enabled", valueType: typeof(bool), desc: "If false, module is not enabled to operate")]
        public bool Enabled = false;

        [ConfigParam(name: "OutputDir", valueType: typeof(string), desc: "Base directory for Loden asset storage")]
        public string OutputDir = "./LodenAssets";

        [ConfigParam(name: "URIBase", valueType: typeof(string), desc: "the string added to be beginning of asset name to create URI")]
        public string URIBase = "./";

        [ConfigParam(name: "UseReadableFilenames", valueType: typeof(bool), desc: "Whether filenames should be human readable or UUIDs")]
        public bool UseReadableFilenames = false;

        [ConfigParam(name: "UseDeepFilenames", valueType: typeof(bool), desc: "Whether filenames be organized into a deep directory structure")]
        public bool UseDeepFilenames = true;

        [ConfigParam(name: "WriteBinaryGltf", valueType: typeof(bool), desc: "Whether to write .gltf or .glb file")]
        public bool WriteBinaryGltf = false;

        [ConfigParam(name: "GltfCopyright", valueType: typeof(string), desc: "Copyright notice embedded into generated GLTF files")]
        public string GltfCopyright = "Copyright 2022. All rights reserved";

        [ConfigParam(name: "ConvoarID", valueType: typeof(string), desc: "GUID used for CreatorID, ... (new terrain and images)")]
        public string ConvoarID = "e1f5686f-05a8-44f7-aae4-551733b07551";

        [ConfigParam(name: "VerticesMaxForBuffer", valueType: typeof(int), desc: "Number of vertices to cause splitting of buffer files")]
        public int VerticesMaxForBuffer = 50000;
        [ConfigParam(name: "DoubleSided", valueType: typeof(bool), desc: "whether double sided mesh rendering specified in GLTF")]
        public bool DoubleSided = false;

        [ConfigParam(name: "AddTerrainMesh", valueType: typeof(bool), desc: "whether to create and add a terrain mesh")]
        public bool AddTerrainMesh = true;
        [ConfigParam(name: "CreateTerrainSplat", valueType: typeof(bool), desc: "whether to generate a terrain mesh splat texture")]
        public bool CreateTerrainSplat = true;
        [ConfigParam(name: "HalfRezTerrain", valueType: typeof(bool), desc: "Whether to reduce the terrain resolution by 2")]
        public bool HalfRezTerrain = false;

        [ConfigParam(name: "TextureMaxSize", valueType: typeof(int), desc: "The maximum pixel dimension for images if exporting")]
        public int TextureMaxSize = 256;
        [ConfigParam(name: "PreferredTextureFormat", valueType: typeof(string), desc: "One of: PNG, JPG, GIF, BMP")]
        public string PreferredTextureFormat = "PNG";
        [ConfigParam(name: "PreferredTextureFormatIfNoTransparency", valueType: typeof(string), desc: "One of: PNG, JPG, GIF, BMP")]
        public string PreferredTextureFormatIfNoTransparency = "JPG";

        [ConfigParam(name: "AddUniqueCodes", valueType: typeof(bool), desc: "Add an extras.unique value to some GLTF objects as a unique hash")]
        public bool AddUniqueCodes = true;
        [ConfigParam(name: "DisplayTimeScaling", valueType: typeof(bool), desc: "If to delay mesh scaling to display/GPU time")]
        public bool DisplayTimeScaling = false;

        [ConfigParam(name: "LogBuilding", valueType: typeof(bool), desc: "log detail BScene/BInstance object building")]
        public bool LogBuilding = true;
        [ConfigParam(name: "LogGltfBuilding", valueType: typeof(bool), desc: "output detailed gltf construction details")]
        public bool LogGltfBuilding = false;

        /// <summary>
        /// Given a set of user parameters, loop through all the defined parameters
        ///     and change the default value to the user's value if specified.
        /// </summary>
        /// <param name="cfg"></param>
        public void SetParameterConfigurationValues(IConfig cfg) {
            // For every method in this class
            foreach (FieldInfo fi in this.GetType().GetFields()) {
                // For every attribute in the field
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi)) {
                    // If the attribute is a 'ConfigParam'
                    ConfigParam cp = attr as ConfigParam;
                    if (cp != null) {
                        // If the user specified a new value, use that value
                        if (cfg.Contains(cp.name)) {
                            string configValue = cfg.GetString(cp.name);
                            // User values are always type 'string' so convert to value type
                            fi.SetValue(this, ParamBlock.ConvertToObj(cp.valueType, configValue));
                        }
                    }
                }
            }
        }
    }
}
