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
using org.herbal3d.Tiles;

namespace org.herbal3d.Loden {
    public class LodenRegion {
        private static readonly String _logHeader = "[LodenRegion]";

        public string RegionTopLevelSpecURL;

        private readonly Scene _scene;
        public readonly LodenContext LContext;
        private LodenAssets _assetTools;

        // Given a scene, do the LOD ("level of detail") conversion
        public LodenRegion(Scene pScene, LodenContext pLContext) {
            _scene = pScene;
            LContext = pLContext;

            // Register this lod controller for the region so other modules can access
            _scene.RegisterModuleInterface<LodenRegion>(this);
        }

        public void Start() {
            // Wait for the region to have all its content before scanning
            _scene.EventManager.OnPrimsLoaded += Event_OnPrimsLoaded;
        }

        public void Shutdown() {
            _scene.EventManager.OnPrimsLoaded -= Event_OnPrimsLoaded;
        }

        // All prims have been loaded into the region.
        // Verify they have been converted into the LOD'ed versions.
        private void Event_OnPrimsLoaded(Scene pScene) {
            // Loading is going to take a while. Start up a Task.
            Task.Run(async () => {
                await ConvertRegionAssets();
            });
        }

        private async Task ConvertRegionAssets() {
            _assetTools = new LodenAssets(_scene, LContext);

            // Subscribe to changes in the region so we know when to start rebuilding
            // TODO:

            BHash regionHash = LodenRegion.CreateRegionHash(_scene);
            LContext.log.DebugFormat("{0} SOGs in region: {1}", _logHeader, _scene.GetSceneObjectGroups().Count);
            LContext.log.DebugFormat("{0} Computed region hash: {1}", _logHeader, regionHash.ToString());

            // Cleaned up region identifier
            string regionIdentifier = _scene.RegionInfo.RegionID.ToString().Replace("-", "");

            using (AssetManager assetManager = new AssetManager(_scene.AssetService, LContext.log, LContext.parms)) {
                string specFilename = regionIdentifier + ".json";
                RegionTopLevelSpecURL = CreateFileURI(regionIdentifier + ".json", LContext.parms);

                // See if region specification file has been built
                bool buildRegion = true;
                try {
                    string regionSpec = await assetManager.AssetStorage.FetchText(specFilename);
                    if (!String.IsNullOrEmpty(regionSpec)) {
                        // Read in the spec file
                        TileSet regionTiles = TileSet.FromString(regionSpec);
                        if (regionTiles.root.content.extras.ContainsKey("contentHash")) {
                            // If the content hash matches, the region doesn't need rebuilding
                            if ((string)regionTiles.root.content.extras["contentHash"] == regionHash.ToString()) {
                                LContext.log.DebugFormat("{0} Content hash matches. Not rebuilding", _logHeader);
                                buildRegion = false;
                            }
                            else {
                                LContext.log.DebugFormat("{0} Content hash does not match. Rebuilding", _logHeader);
                            }
                        }
                    }
                }
                catch (Exception e) {
                    LContext.log.ErrorFormat("{0} Exception reading region spec file: {1}", _logHeader, e);
                    buildRegion = true;
                }
                if (buildRegion) {
                    // The region has not been built.
                    LContext.log.DebugFormat("{0}: region does not match. Rebuilding", _logHeader);

                    // Convert the OpenSimulator scene to BScene object for manipulation
                    BScene bScene = await ConvertSceneToBScene(assetManager);

                    // Write out the 'top level' (highest quality) version of the region
                    LHandle topLevelHandle = await WriteOutLevel(regionHash, bScene, assetManager);

                    // Create the region specification which defines the top of the region LOD tree
                    LHandle regionSpecFile = await WriteRegionSpec(assetManager, regionHash, specFilename, topLevelHandle.Filename);
                }
                else {
                    LContext.log.DebugFormat("{0}: region spec file exists.", _logHeader);
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
                BConverterOS converter = new BConverterOS(LContext.log, LContext.parms);
                bScene = await converter.ConvertRegionToBScene(_scene, pAssetManager);
            }
            catch (Exception e) {
                LContext.log.ErrorFormat("{0} Exeception converting region to BScene: {1}", _logHeader, e);
            }
            return bScene;
        }

        // Given a BScene, write out a GLTF version and return a handle to the version.
        private async Task<LHandle> WriteOutLevel(BHash pLevelHash, BScene pBScene, AssetManager pAssetManager) {
            Gltf gltf;
            try {
                gltf = new Gltf(_scene.Name, LContext.log, LContext.parms);
                gltf.LoadScene(pBScene);
            }
            catch (Exception e) {
                string emsg = String.Format("{0} Exeception loading scene into Gltf: {1}", _logHeader, e);
                LContext.log.ErrorFormat(emsg);
                throw new Exception(emsg);
            }

            string topLevelFilename = pLevelHash.ToString() + ".gltf";
            LContext.log.DebugFormat("{0}: writing top level region GLTF to {1}", _logHeader, topLevelFilename);
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
                LContext.log.ErrorFormat(emsg);
                throw new Exception(emsg);
            }
            return new LHandle(pLevelHash, topLevelFilename);
        }

        // Write the region spec file.
        private async Task<LHandle> WriteRegionSpec(AssetManager pAssetManager, BHash pRegionHash,
                                    string pFilename, string pRegionSpecURI) {
            // Create a simple tileset defining the region
            Tiles.TileSet regSpec = new Tiles.TileSet {
                root = new Tiles.Tile() {
                    content = new Tiles.TileContent() {
                        uri = pRegionSpecURI,
                        extras = new Tiles.TileExtensions() {
                            { "contentHash", pRegionHash.ToString() }
                        }
                    },
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
        public static BHash CreateRegionHash(Scene pScene) {
            BHasher regionHasher = new BHasherSHA256();
            regionHasher.Add(pScene.RegionInfo.RegionID.GetBytes(), 0, 16);

            foreach (SceneObjectGroup sog in pScene.GetSceneObjectGroups().OrderBy(x => x.UUID)) {
                regionHasher.Add(LodenRegion.CreateSOGHash(sog));
            }
            
            return regionHasher.Finish();
        }

        // Create a uniquifying hash for this SOG instance.
        // The hash includes the UUID, a hash of the prim paramters, and the position in the scene.
        public static BHash CreateSOGHash(SceneObjectGroup pSog) {
            BHasher sogHasher = new BHasherSHA256();
            sogHasher.Add(pSog.UUID.GetBytes(), 0, 16);
            foreach (SceneObjectPart sop in pSog.Parts.OrderBy(x => x.UUID)) {
                sogHasher.Add(sop.UUID.GetBytes(), 0, 16);
                sogHasher.Add(sop.Shape.GetMeshKey(sop.Scale, (float)OMVR.DetailLevel.Highest));
                LodenRegion.AddPositionToHash(sogHasher, sop.AbsolutePosition, sop.RotationOffset);
            };
            return sogHasher.Finish();
        }

        public static void AddPositionToHash(BHasher hasher, OMV.Vector3 pos, OMV.Quaternion rot) {
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
