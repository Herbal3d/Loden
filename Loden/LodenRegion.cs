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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Addins;
using System.IO;

using OpenSim.Framework;
using OpenSim.Region.CoreModules.World.LegacyMap;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.PhysicsModules.SharedBase; // needed to test if objects are physical

using OMV = OpenMetaverse;
using OMVS = OpenMetaverse.StructuredData;
using OMVA = OpenMetaverse.Assets;
using OMVR = OpenMetaverse.Rendering;

using org.herbal3d.cs.os.CommonEntities;
using org.herbal3d.cs.CommonEntitiesUtil;

using log4net;

namespace org.herbal3d.Loden {
    public class LodenRegion {
        private static readonly String _logHeader = "[LodenRegion]";

        private readonly Scene _scene;
        private readonly LodenContext _context;
        private LodenAssets _assetTools;
        private bool _running;  // 'true' if should be doing stuff

        // Given a scene, do the LOD ("level of detail") conversion
        public LodenRegion(Scene pScene, LodenContext pContext) {
            _scene = pScene;
            _context = pContext;
            _running = false;
        }


        public void Start() {
            _running = true;
            // Wait for the region to have all its content before scanning
            _scene.EventManager.OnPrimsLoaded += Event_OnPrimsLoaded;
        }

        public void Shutdown() {
            _running = false;
            _scene.EventManager.OnPrimsLoaded -= Event_OnPrimsLoaded;
        }

        // All prims have been loaded into the region.
        // Verify they have been converted into the LOD'ed versions.
        private void Event_OnPrimsLoaded(Scene pScene) {
            // Loading is going to take a while. Start up a Task.
            Task.Run(async () => {
                await ConvertRegionAssets(pScene);
            });
        }

        private async Task ConvertRegionAssets(Scene pScene) {
            _assetTools = new LodenAssets(_scene, _context);

            // Subscribe to changes in the region so we know when to start rebuilding

            _context.log.DebugFormat("{0} Enter", _logHeader);
            // Create a region hash and build content tables
            Dictionary<OMV.UUID, BHash> sogHashes = new Dictionary<OMV.UUID, BHash>();
            BHasher regionHasher = new BHasherSHA256();
            regionHasher.Add(_scene.RegionInfo.RegionID.GetBytes(), 0, 16);
            foreach (SceneObjectGroup sog in _scene.GetSceneObjectGroups()) {
                BHash sogHash = CreateSOGHash(sog);
                sogHashes.Add(sog.UUID, sogHash);
                regionHasher.Add(sogHash.ToBytes(), 0, sogHash.ToBytes().Length);
            }
            BHash regionHash = regionHasher.Finish();
            _context.log.DebugFormat("{0} SOGs in region: {1}", _logHeader, _scene.GetSceneObjectGroups().Count);
            _context.log.DebugFormat("{0} Computed region hash: {1}", _logHeader, regionHash.ToString());

            // Cleaned up region identifier
            string regionIdentifier = _scene.RegionInfo.RegionID.ToString().Replace("-", "");

            // See if region specification file has been built
            string absSpecFilename = CreateAbsFilePath(regionIdentifier + ".json", _context.parms);
            _context.log.DebugFormat("{0}: region spec filename={1}", _logHeader, absSpecFilename);
            if (!File.Exists(absSpecFilename)) {
                // The region has not been built.
                _context.log.DebugFormat("{0}: region spec file does not exist. Rebuilding", _logHeader);
                BScene bScene = null;
                using (AssetManager assetManager = new OSAssetFetcher(_scene.AssetService, _context.log, _context.parms)) {
                    try {
                        BConverterOS converter = new BConverterOS(_context.log, _context.parms);
                        bScene = await converter.ConvertRegionToBScene(_scene, assetManager);
                    }
                    catch (Exception e) {
                        _context.log.ErrorFormat("{0} Exeception converting region to BScene: {1}", _logHeader, e);
                    }
                    Gltf gltf = null;
                    try {
                        gltf = new Gltf(_scene.Name, _context.log, _context.parms);
                        gltf.LoadScene(bScene, assetManager);
                    }
                    catch (Exception e) {
                        _context.log.ErrorFormat("{0} Exeception loading scene into Gltf: {1}", _logHeader, e);
                    }
                    if (gltf != null) {
                        string absTopLevelFilename = CreateAbsFilePath(regionIdentifier + ".gltf", _context.parms);
                        _context.log.DebugFormat("{0}: writing top level region GLTF to {1}", _logHeader, absTopLevelFilename);
                        try {
                            using (StreamWriter outt = File.CreateText(absTopLevelFilename)) {
                                gltf.ToJSON(outt);
                            }
                            gltf.WriteBinaryFiles();
                            gltf.WriteImages();
                        }
                        catch (Exception e) {
                            _context.log.ErrorFormat("{0} Exeception writing top level GLTF files: {1}", _logHeader, e);
                        }
                    }
                }
            }

            // Is there terrain for the region?

            // Suck in the metadata for all the things in the region.

            // Partition region and verify all partitions have been created and not different.

            // Walk up the LOD chain and verify existance or build each LOD level

            // Wait for changes and do rebuilds of necessary
            
        }

        // Given a target filename, return the absolute path to a created storage filename.
        private string CreateAbsFilePath(string pFilename, IParameters pParams) {
            string strippedStorageName = Path.GetFileNameWithoutExtension(pFilename);
            string storageDir = PersistRules.StorageDirectory(strippedStorageName, pParams);
            string absCreatedDir = PersistRules.CreateDirectory(storageDir, pParams);
            return Path.Combine(absCreatedDir, pFilename);
        }

        // Create a uniquifying hash for this SOG
        private BHash CreateSOGHash(SceneObjectGroup pSog) {
            BHasher sogHasher = new BHasherSHA256();
            sogHasher.Add(pSog.UUID.GetBytes(), 0, 16);
            foreach (SceneObjectPart sop in pSog.Parts) {
                sogHasher.Add(sop.UUID.GetBytes(), 0, 16);
                AddPositionToHash(sogHasher, sop.AbsolutePosition, sop.RotationOffset);
            };
            return sogHasher.Finish();
        }

        private void AddPositionToHash(BHasher hasher, OMV.Vector3 pos, OMV.Quaternion rot) {
            hasher.Add(pos.X);
            hasher.Add(pos.Y);
            hasher.Add(pos.Z);
            hasher.Add(rot.X);
            hasher.Add(rot.Y);
            hasher.Add(rot.Z);
            hasher.Add(rot.W);
            
        }
    }
}
