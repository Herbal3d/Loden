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
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;

using org.herbal3d.cs.CommonEntities;
using org.herbal3d.cs.CommonEntitiesUtil;

using OMV = OpenMetaverse;
using OMVA = OpenMetaverse.Assets;
using OpenMetaverse.Imaging;

namespace org.herbal3d.Loden {

    public class LHandle {
        public BHash Hash;
        public string Filename;

        public LHandle(BHash pHash, string pFilename) {
            Hash = pHash;
            Filename = pFilename;
        }
        public override string ToString() {
            string ret = String.Empty;
            if (String.IsNullOrEmpty(Filename)) {
                ret = Hash.ToString();
            }
            else {
                ret = Filename;
            }
            return ret;
        }
    }

    // An async interface to the asset fetcher
    public class LodenAssets : IDisposable {
        protected readonly LodenContext _context;
        protected Scene _scene;

        public LodenAssets(Scene scene, LodenContext pContext) {
            _scene = scene;
            _context = pContext;
        }

        public void Dispose() {
        }

        // Given a hash for an SOG, return a handle for the SOG entry.
        // If there is no SOG info in the database, return 'null'.
        public async Task<LHandle> GetHandle(BHash pHash) {
            LHandle ret = null;
            string filename = PersistRules.GetFilename(PersistRules.AssetType.Scene,
                                _scene.RegionInfo.RegionName, pHash.ToString(), _context.parms);
            filename = Path.GetFileNameWithoutExtension(filename);
            string dir = PersistRules.StorageDirectory(pHash.ToString(), _context.parms);
            // Heavy handed async stuff but the checks for existance could take a while
            //     if the storage system is remote.
            if (await Task.Run(() => {
                return Directory.Exists(dir) &&
                             ( File.Exists(Path.Combine(dir, pHash + ".gltf"))
                             || File.Exists(Path.Combine(dir, pHash + ".glb")) );
                        }) ) {
                ret = new LHandle(pHash, dir);
            }
            return ret;
        }
    }
}
