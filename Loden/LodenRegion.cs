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

using org.herbal3d.cs.CommonEntities;
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
            // TODO:

            BHash regionHash = CreateRegionHash(_scene);
            _context.log.DebugFormat("{0} SOGs in region: {1}", _logHeader, _scene.GetSceneObjectGroups().Count);
            _context.log.DebugFormat("{0} Computed region hash: {1}", _logHeader, regionHash.ToString());

            // Cleaned up region identifier
            string regionIdentifier = _scene.RegionInfo.RegionID.ToString().Replace("-", "");

            using (AssetManager assetManager = new AssetManager(_scene.AssetService, _context.log, _context.parms)) {
                string specFilename = regionIdentifier + ".json";
                string specURI = CreateFileURI(regionIdentifier + ".json", _context.parms);

                // See if region specification file has been built
                string regionSpec = await assetManager.AssetStorage.FetchText(specFilename);
                if (String.IsNullOrEmpty(regionSpec)) {
                    // The region has not been built.
                    _context.log.DebugFormat("{0}: region spec file does not exist. Rebuilding", _logHeader);

                    // Convert the OpenSimulator scene to BScene object for manipulation
                    BScene bScene = await ConvertSceneToBScene(assetManager);

                    // Write out the 'top level' (highest quality) version of the region
                    LHandle topLevelHandle = await WriteOutLevel(regionHash, bScene, assetManager);
                    string topLevelURI = CreateFileURI(topLevelHandle.Filename, _context.parms);

                    // Create the region specification which defines the top of the region LOD tree
                    LHandle regionSpecFile = await WriteRegionSpec(assetManager, specFilename, topLevelURI);
                }
                else {
                    _context.log.DebugFormat("{0}: region spec file exists.", _logHeader);
                }
            }

            // Partition region and verify all partitions have been created and not different.
            // TODO:

            // Walk up the LOD chain and verify existance or build each LOD level
            // TODO:

            // Wait for changes and do rebuilds of necessary
            // TODO:
            
        }

        // Convert the region into the optimizable and convertable BScene.
        private async Task<BScene> ConvertSceneToBScene(AssetManager pAssetManager) {
            BScene bScene = null;
            try {
                BConverterOS converter = new BConverterOS(_context.log, _context.parms);
                bScene = await converter.ConvertRegionToBScene(_scene, pAssetManager);
            }
            catch (Exception e) {
                _context.log.ErrorFormat("{0} Exeception converting region to BScene: {1}", _logHeader, e);
            }
            return bScene;
        }

        // Given a BScene, write out a GLTF version and return a handle to the version.
        private async Task<LHandle> WriteOutLevel(BHash pLevelHash, BScene pBScene, AssetManager pAssetManager) {
            Gltf gltf = null;
            try {
                gltf = new Gltf(_scene.Name, _context.log, _context.parms);
                gltf.LoadScene(pBScene, pAssetManager);
            }
            catch (Exception e) {
                string emsg = String.Format("{0} Exeception loading scene into Gltf: {1}", _logHeader, e);
                _context.log.ErrorFormat(emsg);
                throw new Exception(emsg);
            }

            string topLevelFilename = pLevelHash.ToString() + ".gltf";
            _context.log.DebugFormat("{0}: writing top level region GLTF to {1}", _logHeader, topLevelFilename);
            try {
                using (var outm = new MemoryStream()) {
                    using (StreamWriter outt = new StreamWriter(outm)) {
                        gltf.ToJSON(outt);
                    }
                    await pAssetManager.AssetStorage.Store(topLevelFilename, outm.ToArray());
                }
                gltf.WriteBinaryFiles(pAssetManager.AssetStorage);
                gltf.WriteImages(pAssetManager.AssetStorage);
            }
            catch (Exception e) {
                string emsg = String.Format("{0} Exeception writing top level GLTF files: {1}", _logHeader, e);
                _context.log.ErrorFormat(emsg);
                throw new Exception(emsg);
            }
            return new LHandle(pLevelHash, topLevelFilename);
        }

        // Write the region spec file.
        private async Task<LHandle> WriteRegionSpec(AssetManager pAssetManager, string pFilename, string pRegionSpecURI) {
            // Create a simple tileset defining the region
            Tiles.TileSet regSpec = new Tiles.TileSet {
                root = new Tiles.Tile() {
                    content = new Tiles.TileContent(pRegionSpecURI),
                    boundingVolume = new Tiles.TileBoundingVolume() {
                        box = new Tiles.TileBox()
                    },
                    geometricError = 0.5f
                },
            };

            using (var outm = new MemoryStream()) {
                using (var outt = new StreamWriter(outm)) {
                    regSpec.ToJSON(outt);
                }
                await pAssetManager.AssetStorage.Store(pFilename, outm.ToArray());
            }
            return new LHandle(new BHashULong(), pFilename);
        }

        // Given a filename, return the URI that would reference that file in the asset system
        private string CreateFileURI(string pFilename, IParameters pParams) {
            return PersistRules.ReferenceURL(pParams.P<string>("URIBase"), pFilename);
        }

        // Create a region hash made of the hashes of all the SOGs in the region
        private BHash CreateRegionHash(Scene pScene) {
            BHasher regionHasher = new BHasherSHA256();
            regionHasher.Add(_scene.RegionInfo.RegionID.GetBytes(), 0, 16);
            foreach (SceneObjectGroup sog in pScene.GetSceneObjectGroups()) {
                BHash sogHash = CreateSOGHash(sog);
                // Remember the SOG hash so we won't need to recreate it later
                byte[] sogHashBytes = sogHash.ToBytes();
                regionHasher.Add(sogHashBytes, 0, sogHashBytes.Length);
            }
            return regionHasher.Finish();
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
