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
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Definition of classes for Cesium 3d-Tiles.
// https://github.com/AnalyticalGraphicsInc/3d-tiles/tree/master/specification
// Notated for correct JSON serialization by Json.NET
namespace org.herbal3d.Tiles {
    public class TileSet {
        public TileAsset asset;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileProperties properties;
        public float geometricError;
        public Tile root;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] extensionsUsed;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] extensionsRequired;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extensions;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extras;

        public TileSet() {
            asset = new TileAsset();
            geometricError = 1.0f;
            root = new Tile();
        }

        public void ToJSON(StreamWriter outt) {
            JsonSerializer serializer = new JsonSerializer {
                Formatting = Formatting.Indented
            };
            serializer.Serialize(outt, this);
            outt.Flush();
        }
    }

    public class TileAsset {
        public string version = "1.0";
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string tilesetVersion;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extensions;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extras;

        public TileAsset() {
        }
    }

    public class Tile {
        public TileBoundingVolume boundingVolume = new TileBoundingVolume();
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileBoundingVolume viewerRequestVolume;
        public float geometricError = 1.0f;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string refine;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileTransform transform;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileContent content;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileContent[] children;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extensions;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extras;

        public Tile() {
        }
    }

    public class TileContent {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileBoundingVolume boundingVolume;
        public string uri;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extensions;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extras;

        public TileContent(string pURI) {
            uri = pURI;
        }
    }

    public class TileBoundingVolume {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileBox box;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileRegion region;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileSphere sphere;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extensions;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extras;
    }

    public abstract class JustAnArray {
        abstract public int ArraySize();
        abstract public double[] GetArray();
        abstract public string Name();
    }
    // double[16] specifying node transform
    // [1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]
    [JsonConverter(typeof(JustAnArrayIO))]
    public class TileTransform : JustAnArray {
        public double[] transform;

        public override int ArraySize() { return 16; }
        public override double[] GetArray() { return transform; }
        public override string Name() { return "transform"; }

        public TileTransform() {
            transform = new double[16] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        }
        public TileTransform(double[] pValues) {
            transform = pValues;
        }
    }

    // double[12] defining oriented bounding box.
    // The first three elements define the x, y, and z values for the center of the box.
    //    The next three elements(with indices 3, 4, and 5) define the x axis direction
    //    and half-length.The next three elements(indices 6, 7, and 8) define the y axis
    //    direction and half-length.The last three elements(indices 9, 10, and 11)
    //    define the z axis direction and half-length.
    [JsonConverter(typeof(JustAnArrayIO))]
    public class TileBox : JustAnArray {
        public double[] box;

        public override int ArraySize() { return 12; }
        public override double[] GetArray() { return box; }
        public override string Name() { return "box"; }

        public TileBox() {
            box = new double[12];
        }
        public TileBox(double[] pValues) {
            box = pValues;
        }
    }
    // double[6] defining a region
    // An array of six numbers that define a bounding geographic region in EPSG:4979
    //    coordinates with the order[west, south, east, north, minimum height, maximum height].
    //    Longitudes and latitudes are in radians, and heights are in meters above
    //    (or below) the WGS84 ellipsoid.
    [JsonConverter(typeof(JustAnArrayIO))]
    public class TileRegion : JustAnArray {
        public double[] region;
        public override int ArraySize() { return 6; }
        public override double[] GetArray() { return region; }
        public override string Name() { return "region"; }
        public TileRegion() {
            region = new double[6];
        }
        public TileRegion(double[] pValues) {
            region = pValues;
        }
    }
    // double[6] defining a sphere
    // The first three elements define the x, y, and z values for the center of the sphere.
    //  The last element (with index 3) defines the radius in meters.
    [JsonConverter(typeof(JustAnArrayIO))]
     public class TileSphere : JustAnArray {
        public double[] sphere;
        public override int ArraySize() { return 4; }
        public override double[] GetArray() { return sphere; }
        public override string Name() { return "sphere"; }
        public TileSphere() {
            sphere = new double[4];
        }
        public TileSphere(double[] pValues) {
            sphere = pValues;
        }
    }

    public class TileExtensions : Dictionary<string, object> {
    }

    public class TileProperty {
        public double minimum;
        public double maximum;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extensions;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TileExtensions extras;
    }
    public class TileProperties : Dictionary<string, TileProperty> {
    }

    // JSON output and input routines for classes that are really just names
    //    and an array of values;
    class JustAnArrayIO : JsonConverter {
        public override bool CanRead => false;

        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(JustAnArray);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value is JustAnArray obj) {
                JArray values = new JArray(obj.GetArray());
                values.WriteTo(writer);
            }
        }
    }
}
