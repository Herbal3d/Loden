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
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;

using OMV = OpenMetaverse;
using OMVS = OpenMetaverse.StructuredData;
using OMVA = OpenMetaverse.Assets;
using OMVR = OpenMetaverse.Rendering;

using log4net;

namespace org.herbal3d.Loden {

    public class PrimToMesh : IDisposable {
        private readonly string _logHeader = "[Loden.PrimToMesh]";

        private readonly LodenContext _context;
        private OMVR.MeshmerizerR m_mesher;

        public PrimToMesh(LodenContext pContext) {
            m_mesher = new OMVR.MeshmerizerR();
            _context = pContext;
        }

        /// <summary>
        /// Create and return a faceted mesh.
        /// </summary>
        public async Task<GltfNode> CreateMeshResource(SceneObjectGroup sog, SceneObjectPart sop,
                    OMV.Primitive prim, IAssetFetcher assetFetcher, OMVR.DetailLevel lod) {

            GltfNode mesh = null;
            try {
                if (prim.Sculpt != null) {
                    if (prim.Sculpt.Type == OMV.SculptType.Mesh) {
                        // log.DebugFormat("{0}: CreateMeshResource: creating mesh", LogHeader);
                        _context.stats.numMeshAssets++;
                        mesh = await MeshFromPrimMeshData(sog, sop, prim, assetFetcher, lod);
                    }
                    else {
                        // m_log.DebugFormat("{0}: CreateMeshResource: creating sculpty", LogHeader);
                        _context.stats.numSculpties++;
                        mesh = await MeshFromPrimSculptData(sog, sop, prim, assetFetcher, lod);
                    }
                }
                else {
                    // m_log.DebugFormat("{0}: CreateMeshResource: creating primshape", LogHeader);
                    _context.stats.numSimplePrims++;
                    mesh = await MeshFromPrimShapeData(sog, sop, prim, lod);
                }
            }
            catch (LodenException le) {
            }
            catch (Exception e) {
            }
            return mesh;
        }

        private async Task<GltfNode> MeshFromPrimShapeData(SceneObjectGroup sog, SceneObjectPart sop,
                                OMV.Primitive prim, OMVR.DetailLevel lod) {
            OMVR.FacetedMesh fmesh = m_mesher.GenerateFacetedMesh(prim, lod);

            GltfNode mesh = new GltfNode();
            ExtendedPrim extPrim = new ExtendedPrim(sog, sop, prim, mesh);
            ExtendedPrimGroup extPrimGroup = new ExtendedPrimGroup(extPrim);

            return mesh;
        }

        private async Task<GltfNode> MeshFromPrimSculptData(SceneObjectGroup sog, SceneObjectPart sop,
                                OMV.Primitive prim, IAssetFetcher assetFetcher, OMVR.DetailLevel lod) {

            // Get the asset that the sculpty is built on
            EntityHandle texHandle = new EntityHandle(prim.Sculpt.SculptTexture);
            OMVA.AssetTexture bm = await assetFetcher.FetchTexture(texHandle);
            OMVR.FacetedMesh fMesh = m_mesher.GenerateFacetedSculptMesh(prim, bm.Image.ExportBitmap(), lod);

            GltfNode mesh = new GltfNode();
            ExtendedPrim extPrim = new ExtendedPrim(sog, sop, prim, fMesh);
            ExtendedPrimGroup extPrimGroup = new ExtendedPrimGroup(extPrim);

            return mesh;
        }

        private Task<GltfNode> MeshFromPrimMeshData(SceneObjectGroup sog, SceneObjectPart sop,
                                OMV.Primitive prim, IAssetFetcher assetFetcher, OMVR.DetailLevel lod) {

            // Get the asset that the mesh is built on
            EntityHandle meshHandle = new EntityHandle(prim.Sculpt.SculptTexture);
            try {
                byte[] meshBytes = await assetFetcher.FetchRawAsset(meshHandle);
            }
            catch (Exception e) {
                throw new LodenException("{0} MeshFromPrimMeshData: Rejected FetchTexture: {1}", _logHeader, e);
            }
            OMVA.AssetMesh meshAsset = new OMVA.AssetMesh(prim.ID, meshBytes);
            OMVR.FacetedMesh fMesh;
            GltfNode mesh = null;
            if (OMVR.FacetedMesh.TryDecodeFromAsset(prim, meshAsset, lod, out fMesh)) {
                ExtendedPrim extPrim = new ExtendedPrim(sog, sop, prim, fMesh);
                ExtendedPrimGroup eGroup = new ExtendedPrimGroup(extPrim);
            }
            else {
                throw new LodenException("{0}: MeshFromPrimMeshData: could not decode mesh information from asset. ID="
                            + prim.ID.ToString());
            }

            return mesh;
        }

        // Returns an ExtendedPrimGroup with a mesh for the passed heightmap.
        // Note that the returned EPG does not include any face information -- the caller must add a texture.
        public GltfNode MeshFromHeightMap( float[,] pHeightMap, int regionSizeX, int regionSizeY) {

            // OMVR.Face rawMesh = m_mesher.TerrainMesh(pHeightMap, 0, pHeightMap.GetLength(0)-1, 0, pHeightMap.GetLength(1)-1);
            _context.log.DebugFormat("{0} MeshFromHeightMap: heightmap=<{1},{2}>, regionSize=<{3},{4}>",
                    _logHeader, pHeightMap.GetLength(0), pHeightMap.GetLength(1), regionSizeX, regionSizeY);
            OMVR.Face rawMesh = LodenTerrain.TerrainMesh(pHeightMap, (float)regionSizeX, (float)regionSizeY, _context);
            OMVR.FacetedMesh facetMesh = new OMVR.FacetedMesh();
            facetMesh.Faces = new List<OMVR.Face>() { rawMesh };

            GltfNode mesh = null;
            ExtendedPrim ep = new ExtendedPrim(null, null, null, facetMesh);
            ep.faces = new List<FaceInfo>();

            ExtendedPrimGroup epg = new ExtendedPrimGroup(ep);

            return mesh;
        }


        public void Dispose() {
            m_mesher = null;
        }

        public void UpdateCoords(FaceInfo faceInfo, OMV.Primitive prim) {
            if (faceInfo.vertexs != null) {
                m_mesher.TransformTexCoords(faceInfo.vertexs, faceInfo.faceCenter, faceInfo.textureEntry,  prim.Scale);
            }
        }

        // Walk through all the vertices and scale the included meshes
        public static void ScaleMeshes(ExtendedPrimGroup ePG) {
            foreach (ExtendedPrim ep in ePG.Values) {
                OMV.Vector3 scale = ep.fromOS.primitive.Scale;
                if (scale.X != 1.0 || scale.Y != 1.0 || scale.Z != 1.0) {
                    OnAllVertex(ep, delegate (ref OMVR.Vertex vert) {
                        vert.Position *= scale;
                    });
                }
            }
        }

        // Loop over all the vertices in an ExtendedPrim and perform some operation on them
        public delegate void OperateOnVertex(ref OMVR.Vertex vert);
        public static void OnAllVertex(ExtendedPrim ep, OperateOnVertex vertOp) {
            foreach (FaceInfo aFace in ep.faces) {
                for (int jj = 0; jj < aFace.vertexs.Count; jj++) {
                    OMVR.Vertex aVert = aFace.vertexs[jj];
                    vertOp(ref aVert);
                    aFace.vertexs[jj] = aVert;
                }
            }
        }


    }
}
