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

using org.herbal3d.cs.os.CommonEntities;
using org.herbal3d.cs.CommonEntitiesUtil;

using OMV = OpenMetaverse;
using OMVA = OpenMetaverse.Assets;
using OpenMetaverse.Imaging;

namespace org.herbal3d.Loden {

    public class LHandle {
        public BHash Hash;
        public string Dir;

        public LHandle(BHash pHash, string pDir) {
            Hash = pHash;
            Dir = pDir;
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

        // Given a hash for an SOG, return a BInstance for the SOG.
        // If there is no SOG info in the database, return 'null'.
        public async Task<LHandle> GetHandle(BHash pHash) {
            LHandle ret = null;
            string dir = BaseHashToDirectory(pHash.ToString());
            // heavy handed async stuff but the checks for existance could take a while
            if (await Task.Run(() => {
                return Directory.Exists(dir) &&
                             ( File.Exists(Path.Combine(dir, pHash + ".gltf"))
                             || File.Exists(Path.Combine(dir, pHash + ".glb")) );
                        }) ) {
                ret = new LHandle(pHash, dir);
            }
            return ret;
        }

        private string BaseHashToDirectory(string pHash) {
            string ret = pHash;
            if (pHash.Length >= 10) {
                ret = Path.Combine(_context.parms.LodenAssetDir,
                        Path.Combine(pHash.Substring(0, 2),
                            Path.Combine(pHash.Substring(2, 2),
                                Path.Combine(pHash.Substring(4, 2),
                                    Path.Combine(pHash.Substring(6, 4),
                                        pHash
                        )))));
            }
            return ret;
        }

    }


    /*
    // Fetch an asset from  the OpenSimulator asset system
    public class OSAssetFetcher : AssetFetcher {
        private readonly string _logHeader = "[OSAssetFetcher]";

        public OSAssetFetcher(Scene pScene, LodenContext pContext) : base (pScene, pContext) {
        }

        public override async Task<byte[]> FetchRawAssetAsync(OMV.UUID handle) {
            AssetBase asset = await AssetServiceGetAsync(_scene, handle);
            if (asset == null || asset.Data == null || asset.Data.Length == 0) {
                throw new LodenException("{0} FetchRawAsset: could not fetch asset {1}",
                            _logHeader, handle);
            }
            return asset.Data;
        }

        /// <summary>
        /// Fetch a texture and return an OMVA.AssetTexture. The only information initialized
        /// in the AssetTexture is the UUID and the binary data.s
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public override async Task<OMVA.AssetTexture> FetchTextureAsync(OMV.UUID handle) {

            AssetBase asset = await AssetServiceGetAsync(_scene, handle);
            OMVA.AssetTexture tex = null;
            LodenException failure = null;
            if (asset != null && asset.IsBinaryAsset && asset.Type == (sbyte)OMV.AssetType.Texture) {
                tex = new OMVA.AssetTexture(handle, asset.Data);
                try {
                    if (!tex.Decode()) {
                        failure = new LodenException("{0}: FetchTexture: could not decode JPEG2000 texture. ID={1}",
                                                _logHeader, handle);
                    }
                }
                catch (Exception e) {
                    failure = new LodenException("{0}: FetchTexture: exception decoding JPEG2000 texture. ID={1}, e: {2}",
                                _logHeader, handle, e);
                }
            }
            else {
                failure = new LodenException("{0}: FetchTexture: asset was not of type texture. ID={1}",
                                _logHeader, handle);
            }
            if (failure != null) {
                throw failure;
            }

            return tex;
        }

        /// <summary>
        /// Fetch a texture and return an OMVA.AssetTexture. The only information initialized
        /// in the AssetTexture is the UUID and the binary data.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public override async Task<Image> FetchTextureAsImageAsync(OMV.UUID handle) {

            Image imageDecoded = null;

            AssetBase asset = await AssetServiceGetAsync(_scene, handle);
            LodenException failure = null;
            if (asset != null) {
                if (asset.IsBinaryAsset && asset.Type == (sbyte)OMV.AssetType.Texture) {
                    try {
                        if (_context.parms.UseOpenSimImageDecoder) {
                            IJ2KDecoder imgDecoder = _scene.RequestModuleInterface<IJ2KDecoder>();
                            imageDecoded = imgDecoder.DecodeToImage(asset.Data);
                        }
                        else {
                            if (OpenJPEG.DecodeToImage(asset.Data, out ManagedImage mimage, out imageDecoded)) {
                                // clean up unused object. Decoded image in 'imageDecoded'.
                                mimage = null;
                            }
                            else {
                                imageDecoded = null;
                            }
                        }
                    }
                    catch (Exception e) {
                        failure = new LodenException("{0}: FetchTextureAsImage: exception decoding JPEG2000 texture. ID={1}: {2}",
                                                _logHeader, handle, e);
                    }
                }
                else {
                    failure = new LodenException("{0}: FetchTextureAsImage: asset was not of type texture. ID={1}",
                                            _logHeader, handle);
                }
            }
            else {
                failure = new LodenException("{0}: FetchTextureAsImage: could not fetch texture asset. ID={1}",
                                        _logHeader, handle);
            }
            if (failure != null) {
                throw failure;
            }

            return imageDecoded;
        }

        // An async/await version of async call to OpenSimulator AssetService.
        public async Task<AssetBase> AssetServiceGetAsync(Scene pScene, OMV.UUID pHandle) {
            var tcs = new TaskCompletionSource<AssetBase>();
            pScene.AssetService.Get(pHandle.ToString(), this, (rid, rsender, rasset) => {
                tcs.SetResult(rasset);
            });

            return await tcs.Task;
        }

        public override void Dispose() {
            _scene = null;
        }
    }
    */
}
